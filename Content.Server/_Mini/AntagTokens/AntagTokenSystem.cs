// SPDX-FileCopyrightText: 2026 Casha
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Content.Server._Mini.AntagTokens.Components;
using Content.Server.Administration.Managers;
using Content.Server.Antag;
using Content.Server.Antag.Components;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Server.Roles;
using Content.Server.Roles.Jobs;
using Content.Shared._Mini.AntagTokens;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Server._Mini.AntagTokens;

public sealed class AntagTokenSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IServerDbManager _db = default!;
    [Dependency] private readonly UserDbDataManager _userDb = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
    [Dependency] private readonly JobSystem _jobs = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly RoleSystem _role = default!;

    private readonly Dictionary<NetUserId, PlayerTokenState> _states = new();
    private readonly Dictionary<EntityUid, ReservedGhostRuleState> _reservedGhostRules = new();
    private readonly Dictionary<NetUserId, int?> _sponsorLevelOverrides = new();
    private readonly Dictionary<NetUserId, OnlineRewardState> _onlineRewards = new();

    private int GetDonorBonusByLevel(int sponsorLevel)
    {
        return sponsorLevel switch
        {
            1 => 10,
            2 => 20,
            3 => 30,
            4 => 40,
            5 => 50,
            _ => 0
        };
    }
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AntagTokenOpenRequestEvent>(OnOpenRequest);
        SubscribeNetworkEvent<AntagTokenPurchaseRequestEvent>(OnPurchaseRequest);
        SubscribeNetworkEvent<AntagTokenClearRequestEvent>(OnClearRequest);

        SubscribeLocalEvent<AntagSelectionComponent, AntagSelectionExcludeSessionEvent>(OnExcludeReservedSession);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnJoinedLobby);
        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnRoundstartJobsAssigned, after: new[] { typeof(AntagSelectionSystem) });
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
        SubscribeLocalEvent<GameRuleComponent, GameRuleEndedEvent>(OnReservedGhostRuleEnded);
        SubscribeLocalEvent<GhostRoleAntagSpawnerComponent, ComponentStartup>(OnAntagSpawnerStartup);
        SubscribeLocalEvent<ReservedGhostRoleComponent, TakeGhostRoleEvent>(OnReservedGhostTakeRole, before: new[] { typeof(GhostRoleSystem) });
        SubscribeLocalEvent<GhostRoleAntagSpawnerComponent, GhostRoleSpawnerUsedEvent>(OnReservedGhostSpawnerUsed);

        _userDb.AddOnLoadPlayer(LoadPlayerData);
        _userDb.AddOnPlayerDisconnect(OnPlayerDisconnect);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        SaveAll();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = DateTime.UtcNow;
        foreach (var session in _playerManager.Sessions)
        {
            if (!_onlineRewards.TryGetValue(session.UserId, out var rewardState))
                continue;

            foreach (var (threshold, rewardAmount) in AntagTokenCatalog.OnlineRewardMilestones)
            {
                if (rewardState.GrantedThresholds.Contains(threshold))
                    continue;

                if (now - rewardState.ConnectedAtUtc < threshold)
                    continue;

                rewardState.GrantedThresholds.Add(threshold);
                AddBalance(session.UserId, rewardAmount, out var granted, out _);

                if (granted > 0)
                    ShowPopup(session, $"Награда за онлайн: +{granted} мон.");
            }
        }
    }

    public bool AddBalance(NetUserId userId, int amount, out int grantedAmount, out string? note)
    {
        grantedAmount = 0;
        note = null;

        if (amount <= 0)
            return false;

        var state = EnsureStateExists(userId);
        if (state == null)
            return false;

        NormalizeMonthlyState(state, DateTime.UtcNow);

        var cap = GetMonthlyCap(userId);
        var available = cap.HasValue ? Math.Max(0, cap.Value - state.MonthlyEarned) : amount;
        grantedAmount = Math.Min(amount, available);

        if (grantedAmount > 0)
        {
            state.Balance += grantedAmount;
            if (cap.HasValue)
                state.MonthlyEarned += grantedAmount;
        }

        if (grantedAmount < amount)
            note = "Достигнут месячный лимит монет для вашего уровня поддержки.";

        PersistState(userId, state);
        SendState(userId);
        return grantedAmount > 0 || note != null;
    }

    public bool TrySpendBalance(NetUserId userId, int amount, out string? error)
    {
        error = null;

        if (amount <= 0)
            return true;

        var state = EnsureStateExists(userId);
        if (state == null)
        {
            error = "Профиль валюты ещё не загружен.";
            return false;
        }

        if (state.Balance < amount)
        {
            error = $"Недостаточно монет. Нужно: {amount}.";
            return false;
        }

        state.Balance -= amount;
        PersistState(userId, state);
        SendState(userId);
        return true;
    }

    public int GetBalance(NetUserId userId)
    {
        return EnsureStateExists(userId)?.Balance ?? 0;
    }

    public bool SetBalance(NetUserId userId, int amount)
    {
        if (amount < 0)
            return false;

        var state = EnsureStateExists(userId);
        if (state == null)
            return false;

        state.Balance = amount;
        PersistState(userId, state);
        SendState(userId);
        return true;
    }

    public bool SetMonthlyEarned(NetUserId userId, int amount)
    {
        if (amount < 0)
            return false;

        var state = EnsureStateExists(userId);
        if (state == null)
            return false;

        NormalizeMonthlyState(state, DateTime.UtcNow);
        state.MonthlyEarned = amount;
        PersistState(userId, state);
        SendState(userId);
        return true;
    }

    public bool AddRoleCredit(NetUserId userId, string roleId, int amount, out int newAmount)
    {
        newAmount = 0;

        if (amount <= 0 || !AntagTokenCatalog.TryGetRole(roleId, out _))
            return false;

        var state = EnsureStateExists(userId);
        if (state == null)
            return false;

        state.RoleCredits.TryGetValue(roleId, out var current);
        newAmount = current + amount;
        state.RoleCredits[roleId] = newAmount;
        PersistState(userId, state);
        SendState(userId);
        return true;
    }

    public bool TryOpenForSession(ICommonSession session)
    {
        if (EnsureStateExists(session.UserId) == null)
            return false;

        SendState(session.UserId);
        return true;
    }

    public bool TryGetDebugState(NetUserId userId, [NotNullWhen(true)] out PlayerTokenState? state)
    {
        state = EnsureStateExists(userId);
        if (state == null)
            return false;

        NormalizeMonthlyState(state, DateTime.UtcNow);
        return true;
    }

    public bool TryPurchaseForSession(ICommonSession session, string roleId, out string? error)
    {
        error = null;

        if (!AntagTokenCatalog.TryGetRole(roleId, out var role))
        {
            error = "Такой роли нет в магазине.";
            return false;
        }

        var state = EnsureStateExists(session.UserId);
        if (state == null)
        {
            error = "Профиль валюты ещё не загружен.";
            return false;
        }

        if (!TryGetRoleAvailability(role, session.UserId, state.PendingDepositRoleId == role.Id, out var statusLocKey))
        {
            error = statusLocKey == null ? "Роль сейчас недоступна." : Loc.GetString(statusLocKey);
            return false;
        }

        var useRoleCredit = state.RoleCredits.GetValueOrDefault(role.Id) > 0;
        if (!useRoleCredit && state.Balance < role.Cost)
        {
            error = "Недостаточно монет.";
            return false;
        }

        if (role.Mode == AntagPurchaseMode.LobbyDeposit)
        {
            if (state.PendingDepositRoleId == role.Id)
            {
                error = "Эта роль уже выбрана и ожидает раунда.";
                return false;
            }

            if (state.PendingDepositRoleId != null)
            {
                error = "Сначала снимите текущий депозит роли.";
                return false;
            }

            if (IsRoleSaturated(role.Id, session.UserId))
            {
                error = "Лимит заявок на эту роль уже занят.";
                return false;
            }

            SpendForRole(state, role, useRoleCredit);
            state.PendingDepositRoleId = role.Id;
            state.PendingDepositUsedRoleCredit = useRoleCredit;
            PersistState(session.UserId, state);
            SendState(session.UserId);
            return true;
        }

        if (role.GameRuleId == null || !_gameTicker.StartGameRule(role.GameRuleId, out var ruleEntity))
        {
            error = "Не удалось запустить событие для этой роли.";
            return false;
        }

        SpendForRole(state, role, useRoleCredit);
        PersistState(session.UserId, state);
        _reservedGhostRules[ruleEntity] = new ReservedGhostRuleState(session.UserId, role.Id, useRoleCredit);
        MarkReservedGhostSpawners(ruleEntity, session.UserId);
        SendState(session.UserId);
        return true;
    }

    public bool ClearDeposit(NetUserId userId, out string? error)
    {
        error = null;
        var state = EnsureStateExists(userId);
        if (state == null)
        {
            error = "Профиль валюты ещё не загружен.";
            return false;
        }

        if (state.PendingDepositRoleId == null)
        {
            error = "Сейчас нет активного депозита.";
            return false;
        }

        RefundPendingDeposit(userId, state);
        PersistState(userId, state);
        SendState(userId);
        return true;
    }

    public void SetSponsorLevelOverride(NetUserId userId, int? sponsorLevel)
    {
        if (sponsorLevel is <= 0)
            _sponsorLevelOverrides.Remove(userId);
        else
            _sponsorLevelOverrides[userId] = sponsorLevel;

        SendState(userId);
    }

    public int GetEffectiveSponsorLevel(NetUserId userId)
    {
        if (_sponsorLevelOverrides.TryGetValue(userId, out var overrideLevel) &&
            overrideLevel is > 0)
        {
            return overrideLevel.Value;
        }

        return EntitySystem.Get<SponsorSystem>().Sponsors
            .FirstOrDefault(s => s.Uid == userId.UserId.ToString()).Level;
    }

    private async Task LoadPlayerData(ICommonSession player, CancellationToken cancel)
    {
        var tokenEntries = await _db.GetPlayerAntagTokens(player.UserId.UserId, cancel);
        var selection = await _db.GetPlayerAntagTokenSelection(player.UserId.UserId, cancel);

        var state = new PlayerTokenState();
        foreach (var token in tokenEntries)
        {
            switch (token.TokenId)
            {
                case AntagTokenCatalog.BalanceEntryId:
                    state.Balance = Math.Max(0, token.Amount);
                    break;
                case AntagTokenCatalog.MonthlyEarnedEntryId:
                    state.MonthlyEarned = Math.Max(0, token.Amount);
                    break;
                case AntagTokenCatalog.MonthlyYearEntryId:
                    state.MonthlyYear = token.Amount;
                    break;
                case AntagTokenCatalog.MonthlyMonthEntryId:
                    state.MonthlyMonth = token.Amount;
                    break;
                case AntagTokenCatalog.DepositUsedRoleCreditEntryId:
                    state.PendingDepositUsedRoleCredit = token.Amount > 0;
                    break;
                default:
                    if (token.TokenId.StartsWith("role-credit:", StringComparison.Ordinal) &&
                        token.Amount > 0)
                    {
                        var roleId = token.TokenId["role-credit:".Length..];
                        state.RoleCredits[roleId] = token.Amount;
                    }
                    break;
            }
        }

        if (selection?.TokenId == AntagTokenCatalog.DepositSelectionTokenId &&
            selection.AntagId is { Length: > 0 } selectedRoleId &&
            AntagTokenCatalog.TryGetRole(selectedRoleId, out var role) &&
            role.Mode == AntagPurchaseMode.LobbyDeposit)
        {
            state.PendingDepositRoleId = selectedRoleId;
        }

        NormalizeMonthlyState(state, DateTime.UtcNow);
        _states[player.UserId] = state;
        _onlineRewards[player.UserId] = new OnlineRewardState(DateTime.UtcNow);
    }

    private void OnPlayerDisconnect(ICommonSession player)
    {
        if (_states.TryGetValue(player.UserId, out var state))
            PersistState(player.UserId, state);

        _states.Remove(player.UserId);
        _onlineRewards.Remove(player.UserId);
    }

    private void OnJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        _onlineRewards.TryAdd(ev.PlayerSession.UserId, new OnlineRewardState(DateTime.UtcNow));

        var sponsorLevel = GetEffectiveSponsorLevel(ev.PlayerSession.UserId);
        if (sponsorLevel > 0)
        {
            var userId = ev.PlayerSession.UserId;
            var now = DateTime.UtcNow;

            // Получаем дату последнего начисления (если не было — null)
            if (!_lastDonorBonusClaim.TryGetValue(userId, out var lastClaim) ||
                (now - lastClaim).TotalDays >= 30)
            {
                var bonusAmount = GetDonorBonusByLevel(sponsorLevel);
                if (bonusAmount > 0)
                {
                    AddBalance(userId, bonusAmount, out var granted, out _);
                    ShowPopup(ev.PlayerSession, $"Донатерский бонус: +{granted} монет (уровень {sponsorLevel})!");

                    // Обновляем время последней выдачи
                    _lastDonorBonusClaim[userId] = now;
                }
            }
        }

        SendState(ev.PlayerSession.UserId);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent _)
    {
        _reservedGhostRules.Clear();
        SaveAll();
    }

    private void OnRoundstartJobsAssigned(RulePlayerJobsAssignedEvent ev)
    {
        foreach (var session in ev.Players)
        {
            if (!TryGetPendingLobbyRole(session.UserId, out var role))
                continue;

            var state = EnsureStateExists(session.UserId);
            if (state == null)
                continue;

            if (!TryGetRoleAvailability(role, session.UserId, purchased: true, out var statusLocKey))
            {
                RefundPendingDeposit(session.UserId, state);
                PersistState(session.UserId, state);
                SendState(session.UserId);
                ShowPopup(session, statusLocKey == null ? "Депозит роли отменён, средства возвращены." : $"{Loc.GetString(statusLocKey)} Средства возвращены.");
                continue;
            }

            if (IsReservedRoleBlockedByCurrentJob(session))
            {
                ShowPopup(session, "Текущая должность из Command/Security блокирует токен-роль. Резерв сохранён до подходящего раунда.");
                SendState(session.UserId);
                continue;
            }

            if (!TryAssignReservedRoundstartRole(session, role, out var assignError))
            {
                RefundPendingDeposit(session.UserId, state);
                PersistState(session.UserId, state);
                SendState(session.UserId);
                ShowPopup(session, assignError ?? "Не удалось выдать зарезервированную роль. Средства возвращены.");
                continue;
            }

            state.PendingDepositRoleId = null;
            state.PendingDepositUsedRoleCredit = false;
            PersistState(session.UserId, state);
            SendState(session.UserId);
            ShowPopup(session, $"Зарезервированная роль \"{GetRoleName(role)}\" выдана.");
        }
    }

    private void OnAntagSpawnerStartup(Entity<GhostRoleAntagSpawnerComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.Rule is not { } rule ||
            !_reservedGhostRules.TryGetValue(rule, out var reservedState))
        {
            return;
        }

        var reserved = EnsureComp<ReservedGhostRoleComponent>(ent);
        reserved.ReservedUserId = reservedState.UserId;
    }

    private void OnReservedGhostRuleEnded(Entity<GameRuleComponent> ent, ref GameRuleEndedEvent args)
    {
        if (!_reservedGhostRules.Remove(ent, out var reservedState))
            return;

        if (!AntagTokenCatalog.TryGetRole(reservedState.RoleId, out var role))
            return;

        var state = EnsureStateExists(reservedState.UserId);
        if (state == null)
            return;

        // RefundRolePurchase(state, role, reservedState.UsedRoleCredit);
        PersistState(reservedState.UserId, state);
        SendState(reservedState.UserId);

        if (_playerManager.TryGetSessionById(reservedState.UserId, out var session))
            ShowPopup(session, $"Событие для роли \"{GetRoleName(role)}\" не состоялось. Средства возвращены.");
    }

    private void OnOpenRequest(AntagTokenOpenRequestEvent _, EntitySessionEventArgs args)
    {
        SendState(args.SenderSession.UserId);
    }

    private void OnPurchaseRequest(AntagTokenPurchaseRequestEvent ev, EntitySessionEventArgs args)
    {
        if (!TryPurchaseForSession(args.SenderSession, ev.RoleId, out var error))
        {
            ShowPopup(args.SenderSession, error ?? "Покупка недоступна.");
            SendState(args.SenderSession.UserId);
            return;
        }

        if (!AntagTokenCatalog.TryGetRole(ev.RoleId, out var role))
            return;

        var message = role.Mode == AntagPurchaseMode.GhostRule
            ? "Событие запущено. Только вы сможете занять эту гост-роль."
            : "Роль поставлена в очередь на следующий подходящий раунд.";

        ShowPopup(args.SenderSession, message);
        SendState(args.SenderSession.UserId);
    }

    private void OnClearRequest(AntagTokenClearRequestEvent _, EntitySessionEventArgs args)
    {
        if (!ClearDeposit(args.SenderSession.UserId, out var error))
        {
            ShowPopup(args.SenderSession, error ?? "Не удалось снять депозит.");
            return;
        }

        ShowPopup(args.SenderSession, "Депозит роли снят, средства возвращены.");
        SendState(args.SenderSession.UserId);
    }

    private void OnExcludeReservedSession(Entity<AntagSelectionComponent> _, ref AntagSelectionExcludeSessionEvent args)
    {
        args.Excluded = HasPendingLobbyDeposit(args.Session.UserId);
    }

    private void OnReservedGhostTakeRole(Entity<ReservedGhostRoleComponent> ent, ref TakeGhostRoleEvent args)
    {
        if (args.Player.UserId == ent.Comp.ReservedUserId)
            return;

        ShowPopup(args.Player, "Эта гост-роль зарезервирована другим игроком.");
        args.TookRole = true;
    }

    private void OnReservedGhostSpawnerUsed(Entity<GhostRoleAntagSpawnerComponent> ent, ref GhostRoleSpawnerUsedEvent args)
    {
        if (ent.Comp.Rule is not { } rule)
            return;

        _reservedGhostRules.Remove(rule);
        RemCompDeferred<ReservedGhostRoleComponent>(ent);
    }

    private void MarkReservedGhostSpawners(EntityUid ruleEntity, NetUserId reservedUserId)
    {
        var query = EntityQueryEnumerator<GhostRoleAntagSpawnerComponent>();
        while (query.MoveNext(out var uid, out var spawner))
        {
            if (spawner.Rule != ruleEntity)
                continue;

            var reserved = EnsureComp<ReservedGhostRoleComponent>(uid);
            reserved.ReservedUserId = reservedUserId;
        }
    }

    private bool TryGetPendingLobbyRole(NetUserId userId, [NotNullWhen(true)] out AntagRoleDefinition? role)
    {
        role = null;

        if (!_states.TryGetValue(userId, out var state) ||
            state.PendingDepositRoleId == null ||
            !AntagTokenCatalog.TryGetRole(state.PendingDepositRoleId, out var selectedRole) ||
            selectedRole.Mode != AntagPurchaseMode.LobbyDeposit ||
            selectedRole.AntagId == null ||
            selectedRole.GameRuleId == null)
        {
            return false;
        }

        role = selectedRole;
        return true;
    }

    private static bool MatchesDefinition(string antagId, AntagSelectionDefinition definition)
    {
        return definition.PrefRoles.Contains(antagId) || definition.FallbackRoles.Contains(antagId);
    }

    private bool HasPendingLobbyDeposit(NetUserId userId)
    {
        return TryGetPendingLobbyRole(userId, out _);
    }

    private bool TryAssignReservedRoundstartRole(ICommonSession session, AntagRoleDefinition role, out string? error)
    {
        error = null;

        if (session.Status is SessionStatus.Disconnected or SessionStatus.Zombie)
        {
            error = "Игрок сейчас не в валидной сессии для выдачи токен-роли.";
            return false;
        }

        if (session.AttachedEntity is not { Valid: true })
        {
            error = "У игрока ещё нет валидной сущности для выдачи токен-роли.";
            return false;
        }

        if (_mind.TryGetMind(session, out var mindId, out _) && _role.MindIsAntagonist(mindId))
        {
            error = "Игрок уже получил антагонистическую роль другим путём.";
            return false;
        }

        var ruleEntity = _gameTicker.AddGameRule(role.GameRuleId!);
        if (!TryComp<AntagSelectionComponent>(ruleEntity, out var selection))
        {
            error = "У токен-правила нет AntagSelectionComponent.";
            return false;
        }

        if (!TryFindMatchingDefinition(selection, role.AntagId!, out var definition))
        {
            error = "В токен-правиле не найден подходящий antag definition.";
            return false;
        }

        var chosenDefinition = definition ?? throw new InvalidOperationException("Matching antag definition was null after successful lookup.");
        _antagSelection.MakeAntag((ruleEntity, selection), session, chosenDefinition);
        return true;
    }

    private static bool TryFindMatchingDefinition(
        AntagSelectionComponent selection,
        string antagId,
        [NotNullWhen(true)] out AntagSelectionDefinition? definition)
    {
        foreach (var def in selection.Definitions)
        {
            if (!MatchesDefinition(antagId, def))
                continue;

            definition = def;
            return true;
        }

        definition = null;
        return false;
    }

    private bool IsReservedRoleBlockedByCurrentJob(ICommonSession session)
    {
        if (!_mind.TryGetMind(session, out var mindId, out _) ||
            !_jobs.MindTryGetJobId(mindId, out var jobId) ||
            jobId == null)
        {
            return false;
        }

        if (!_jobs.TryGetAllDepartments(jobId.Value, out var departments))
            return false;

        return departments.Any(d => d.ID is "Command" or "Security");
    }

    private bool TryGetRoleAvailability(AntagRoleDefinition role, NetUserId userId, bool purchased, out string? statusLocKey)
    {
        statusLocKey = null;

        if (role.Mode == AntagPurchaseMode.Unavailable)
        {
            statusLocKey = role.UnavailableReasonLocKey ?? "antag-store-status-unavailable";
            return false;
        }

        var playerCount = _playerManager.PlayerCount;
        if (role.MinimumPlayers > 0 && playerCount < role.MinimumPlayers)
        {
            statusLocKey = "antag-store-status-min-players";
            return false;
        }

        if (role.RequiresInRound && _gameTicker.RunLevel != GameRunLevel.InRound)
        {
            statusLocKey = "antag-store-status-round-only";
            return false;
        }

        if (role.RequiresPreRoundLobby && _gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            statusLocKey = "antag-store-status-lobby-only";
            return false;
        }

        if (role.Mode == AntagPurchaseMode.LobbyDeposit && !purchased && IsRoleSaturated(role.Id, userId))
        {
            statusLocKey = "antag-store-status-saturated";
            return false;
        }

        return true;
    }

    private void SendState(NetUserId userId)
    {
        if (!_playerManager.TryGetSessionById(userId, out var session) ||
            !_states.TryGetValue(userId, out var state))
        {
            return;
        }

        NormalizeMonthlyState(state, DateTime.UtcNow);

        var roles = new List<AntagTokenRoleEntry>(AntagTokenCatalog.Roles.Count);
        foreach (var role in AntagTokenCatalog.Roles.Values)
        {
            var purchased = state.PendingDepositRoleId == role.Id;
            var freeUnlocks = state.RoleCredits.GetValueOrDefault(role.Id);
            var canAfford = freeUnlocks > 0 || state.Balance >= role.Cost;
            var available = TryGetRoleAvailability(role, userId, purchased, out var statusLocKey);
            var saturated = role.Mode == AntagPurchaseMode.LobbyDeposit && !purchased && IsRoleSaturated(role.Id, userId);

            if (purchased)
                statusLocKey = "antag-store-status-deposited";
            else if (state.PendingDepositRoleId != null && role.Mode == AntagPurchaseMode.LobbyDeposit)
                statusLocKey ??= "antag-store-status-has-other-deposit";
            else if (!canAfford)
                statusLocKey ??= "antag-store-status-not-enough";

            roles.Add(new AntagTokenRoleEntry(
                role.Id,
                role.Cost,
                role.Mode,
                purchased,
                freeUnlocks,
                canAfford,
                saturated,
                available,
                role.TagLocKey,
                statusLocKey));
        }

        var payload = new AntagTokenState(
            state.Balance,
            state.MonthlyEarned,
            GetMonthlyCap(userId),
            state.PendingDepositRoleId,
            roles);

        RaiseNetworkEvent(new AntagTokenStateEvent(payload), session);
    }

    private void PersistState(NetUserId userId, PlayerTokenState state)
    {
        _ = _db.SetPlayerAntagTokenAmount(userId.UserId, AntagTokenCatalog.BalanceEntryId, state.Balance);
        _ = _db.SetPlayerAntagTokenAmount(userId.UserId, AntagTokenCatalog.MonthlyEarnedEntryId, state.MonthlyEarned);
        _ = _db.SetPlayerAntagTokenAmount(userId.UserId, AntagTokenCatalog.MonthlyYearEntryId, state.MonthlyYear);
        _ = _db.SetPlayerAntagTokenAmount(userId.UserId, AntagTokenCatalog.MonthlyMonthEntryId, state.MonthlyMonth);
        _ = _db.SetPlayerAntagTokenAmount(userId.UserId, AntagTokenCatalog.DepositUsedRoleCreditEntryId, state.PendingDepositUsedRoleCredit ? 1 : 0);

        foreach (var role in AntagTokenCatalog.Roles.Keys)
        {
            _ = _db.SetPlayerAntagTokenAmount(userId.UserId, AntagTokenCatalog.GetRoleCreditEntryId(role), state.RoleCredits.GetValueOrDefault(role));
        }

        if (state.PendingDepositRoleId == null)
            _ = _db.ClearPlayerAntagTokenSelection(userId.UserId);
        else
            _ = _db.SetPlayerAntagTokenSelection(userId.UserId, AntagTokenCatalog.DepositSelectionTokenId, state.PendingDepositRoleId);
    }

    private void SaveAll()
    {
        foreach (var (userId, state) in _states)
        {
            PersistState(userId, state);
        }
    }

    private PlayerTokenState? EnsureStateExists(NetUserId userId)
    {
        if (_states.TryGetValue(userId, out var state))
            return state;

        if (!_playerManager.TryGetSessionById(userId, out _))
            return null;

        state = new PlayerTokenState();
        NormalizeMonthlyState(state, DateTime.UtcNow);
        _states[userId] = state;
        _onlineRewards.TryAdd(userId, new OnlineRewardState(DateTime.UtcNow));
        return state;
    }

private void NormalizeMonthlyState(PlayerTokenState state, DateTime nowUtc, NetUserId? userId = null)
{
    if (state.MonthlyYear == nowUtc.Year && state.MonthlyMonth == nowUtc.Month)
        return;

    // Сброс месяца
    state.MonthlyYear = nowUtc.Year;
    state.MonthlyMonth = nowUtc.Month;
    state.MonthlyEarned = 0;

        // Начисляем ежемесячный бонус донатерам
        if (userId != null)
        {
            var sponsorLevel = GetEffectiveSponsorLevel(userId.Value);
            if (sponsorLevel > 0)
            {
                var bonusAmount = GetDonorBonusByLevel(sponsorLevel);
                if (bonusAmount > 0)
                {
                    // Добавляем бонус в обход лимита (прямо в баланс)
                    state.Balance += bonusAmount;

                    if (_playerManager.TryGetSessionById(userId.Value, out var session))
                        ShowPopup(session, $"Ежемесячный бонус донатера: +{bonusAmount} монет!");
                }
            }
        }
    }

    private int? GetMonthlyCap(NetUserId userId)
    {
        var sponsorLevel = GetEffectiveSponsorLevel(userId);
        // Если есть донат (уровень > 0) — лимита нет (null)
        // Если доната нет — возвращаем лимит, например 100 монет в месяц
        return sponsorLevel > 0 ? null : 100;
    }

    private bool IsRoleSaturated(string roleId, NetUserId exceptUserId)
    {
        var connectedPlayers = _playerManager.Sessions.Count(s => s.Status is not (SessionStatus.Disconnected or SessionStatus.Zombie));
        var maxDeposits = Math.Max(1, connectedPlayers / 10);
        var currentDeposits = _states
            .Where(kv => kv.Key != exceptUserId)
            .Count(kv => kv.Value.PendingDepositRoleId == roleId);

        return currentDeposits >= maxDeposits;
    }

    private static void SpendForRole(PlayerTokenState state, AntagRoleDefinition role, bool useRoleCredit)
    {
        if (useRoleCredit)
            state.RoleCredits[role.Id] = Math.Max(0, state.RoleCredits.GetValueOrDefault(role.Id) - 1);
        else
            state.Balance -= role.Cost;
    }

    private static void RefundRolePurchase(PlayerTokenState state, AntagRoleDefinition role, bool usedRoleCredit)
    {
        if (usedRoleCredit)
            state.RoleCredits[role.Id] = state.RoleCredits.GetValueOrDefault(role.Id) + 1;
        else
            state.Balance += role.Cost;
    }

    private static void RefundPendingDeposit(NetUserId userId, PlayerTokenState state)
    {
        if (state.PendingDepositRoleId == null ||
            !AntagTokenCatalog.TryGetRole(state.PendingDepositRoleId, out var role))
        {
            state.PendingDepositRoleId = null;
            state.PendingDepositUsedRoleCredit = false;
            return;
        }

        RefundRolePurchase(state, role, state.PendingDepositUsedRoleCredit);
        state.PendingDepositRoleId = null;
        state.PendingDepositUsedRoleCredit = false;
    }

    private static string GetRoleName(AntagRoleDefinition role)
    {
        return role.Id switch
        {
            AntagTokenCatalog.ThiefRole => "Вор",
            AntagTokenCatalog.AgentRole => "Агент",
            AntagTokenCatalog.NinjaRole => "Ниндзя",
            AntagTokenCatalog.DragonRole => "Космический дракон",
            AntagTokenCatalog.AbductorRole => "Абдуктор",
            AntagTokenCatalog.InitialInfectedRole => "Нулевой заражённый",
            AntagTokenCatalog.RevenantRole => "Ревенант",
            AntagTokenCatalog.YaoRole => "Ядерный оперативник",
            AntagTokenCatalog.HeadRevRole => "Глава революции",
            AntagTokenCatalog.CosmicCultRole => "Космический культист",
            AntagTokenCatalog.DevilRole => "Дьявол",
            AntagTokenCatalog.BlobRole => "Блоб",
            AntagTokenCatalog.WizardRole => "Маг",
            AntagTokenCatalog.SlaughterDemonRole => "Демон резни",
            AntagTokenCatalog.SlasherRole => "Мясник",
            AntagTokenCatalog.ChangelingRole => "Генокрад",
            AntagTokenCatalog.HereticRole => "Еретик",
            AntagTokenCatalog.ShadowlingRole => "Шедоулинг",
            AntagTokenCatalog.XenomorphRole => "Ксеноморф",
            _ => role.Id
        };
    }

    private void ShowPopup(ICommonSession session, string message)
    {
        if (session.AttachedEntity is { Valid: true } uid)
            _popup.PopupEntity(message, uid, uid);
    }

    private readonly record struct ReservedGhostRuleState(NetUserId UserId, string RoleId, bool UsedRoleCredit);

    private sealed class OnlineRewardState(DateTime connectedAtUtc)
    {
        public DateTime ConnectedAtUtc { get; } = connectedAtUtc;
        public HashSet<TimeSpan> GrantedThresholds { get; } = new();
    }

    public sealed class PlayerTokenState
    {
        public int Balance { get; set; }
        public int MonthlyEarned { get; set; }
        public int MonthlyYear { get; set; }
        public int MonthlyMonth { get; set; }
        public string? PendingDepositRoleId { get; set; }
        public bool PendingDepositUsedRoleCredit { get; set; }
        public Dictionary<string, int> RoleCredits { get; } = new();
    }
}
