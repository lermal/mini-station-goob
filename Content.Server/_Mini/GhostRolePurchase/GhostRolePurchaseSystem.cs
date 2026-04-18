// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

using Content.Server._Mini.AntagTokens;
using Content.Server.GameTicking;
using Content.Server.Popups;
using Content.Shared._Mini.GhostRolePurchase;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Player;
using System.Threading.Tasks;

namespace Content.Server._Mini.GhostRolePurchase;

public sealed class GhostRolePurchaseSystem : EntitySystem
{
    [Dependency] private readonly GhostRolePurchaseTimerSystem _timerSystem = default!;
    [Dependency] private readonly AntagTokenSystem _antagTokens = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private ISawmill _sawmill = default!;

    private static readonly HashSet<string> AnyModePresets = new(StringComparer.OrdinalIgnoreCase)
    {
        "Survival", "SurvivalPlusMid", "Traitor", "Zombie", "Revolutionary", "Nukeops",
        "Blob", "Heretic", "Shadowling", "Xenomorphs", "Changeling",
        "TheGhost", "TheGuide", "SecretPlusLow", "SecretPlusMid", "SecretPlusAdmeme",
        "Secret", "AllAtOnce", "AllerAtOnce", "OopsAllThieves", "CosmicCult",
        "RevTraitor", "RevLing", "NukeTraitor", "NukeLing", "Traitorling",
        "Honkops", "KesslerSyndrome", "Zombieteors", "Deathmatch",
    };

    private static readonly Dictionary<string, HashSet<string>> RoleAllowedPresets =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["Thief"] = AnyModePresets,

        ["Agent"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "TheGhost", "TheGuide", "SecretPlusLow", "SecretPlusMid",
            "Survival", "SurvivalPlusMid",
            "SecretPlusAdmeme",
            "Traitor", "RevTraitor", "NukeTraitor", "Traitorling",
            "OopsAllThieves",
        },

        ["Ninja"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "TheGuide", "SecretPlusMid",
            "Survival", "SurvivalPlusMid",
        },

        ["Dragon"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Survival", "SurvivalPlusMid",
        },

        ["Abductor"] = AnyModePresets,

        ["InitialInfected"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Zombie", "Zombieteors",
            "TheGuide", "SecretPlusMid",
            "Survival", "SurvivalPlusMid",
        },

        ["Revenant"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Survival", "SurvivalPlusMid",
            "Traitor", "Revolutionary", "Zombie", "Nukeops", "Blob",
            "Heretic", "Shadowling", "Xenomorphs", "Changeling",
            "TheGhost", "TheGuide", "SecretPlusLow", "SecretPlusMid", "SecretPlusAdmeme",
            "Secret", "AllAtOnce", "AllerAtOnce", "OopsAllThieves", "CosmicCult",
            "RevTraitor", "RevLing", "NukeTraitor", "NukeLing", "Traitorling",
            "Honkops", "KesslerSyndrome", "Zombieteors", "Deathmatch",
        },

        ["Yao"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Nukeops", "NukeTraitor", "NukeLing",
            "Survival", "SurvivalPlusMid",
        },

        ["RevolutionaryHead"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Revolutionary", "RevTraitor", "RevLing",
        },

        ["SpaceCultist"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "CosmicCult",
        },

        ["Blob"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Blob",
            "Survival", "SurvivalPlusMid",
        },

        ["Wizard"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Survival", "SurvivalPlusMid",
        },

        ["SlaughterDemon"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Survival", "SurvivalPlusMid",
        },

        ["Heretic"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Survival", "SurvivalPlusMid",
            "Heretic",
        },

        ["Changeling"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Survival", "SurvivalPlusMid",
            "Changeling", "Traitorling", "NukeLing", "RevLing",
        },

        ["Shadowling"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Shadowling",
            "Survival", "SurvivalPlusMid",
        },

        ["Slasher"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Survival", "SurvivalPlusMid",
        },

        ["Xenomorph"] = new(StringComparer.OrdinalIgnoreCase)
        {
            "Survival", "SurvivalPlusMid",
        },

        ["Bingle"] = AnyModePresets,
    };

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = Logger.GetSawmill("ghost_role_purchase");
        SubscribeNetworkEvent<GhostRolePurchaseRequestEvent>(OnPurchaseRequest);
        
        _sawmill.Info("GhostRolePurchaseSystem initialized");
    }

    private void OnPurchaseRequest(GhostRolePurchaseRequestEvent ev, EntitySessionEventArgs args)
    {
        if (!TryPurchaseGhostRole(args.SenderSession, ev.RoleId, out var error))
        {
            if (args.SenderSession.AttachedEntity is { Valid: true } uid)
                _popup.PopupEntity(error ?? "Purchase failed.", uid, uid);
            return;
        }

        if (args.SenderSession.AttachedEntity is { Valid: true } successUid)
            _popup.PopupEntity("Ghost role purchased successfully!", successUid, successUid);
    }

    public bool TryPurchaseGhostRole(ICommonSession session, string roleId, out string? error)
    {
        error = null;

        _sawmill.Info($"=== PURCHASE ATTEMPT START ===");
        _sawmill.Info($"Purchase attempt by {session.Name} for role {roleId}");

        var timerActive = _timerSystem?.IsTimerActive() ?? false;
        _sawmill.Info($"Timer active: {timerActive}");

        if (timerActive)
        {
            var remaining = _timerSystem!.GetRemainingTime();
            error = $"Ghost role purchases are blocked. Time remaining: {remaining:mm\\:ss}";
            _sawmill.Info($"Purchase BLOCKED by timer. Remaining: {remaining.TotalSeconds:F1}s");
            return false;
        }

        _sawmill.Info($"Timer check passed, continuing with purchase...");

        if (!IsRoleAvailableInGameMode(roleId))
        {
            error = "This role is not available in the current game mode.";
            _sawmill.Info($"Role {roleId} not available in current game mode");
            return false;
        }

        if (!GhostRolePriceCatalog.TryGetPrice(roleId, out var price))
        {
            error = "Role price not found.";
            _sawmill.Warning($"Price not found for role {roleId}");
            return false;
        }

        if (session.AttachedEntity is not { Valid: true } uid)
        {
            error = "Player does not have a valid entity.";
            return false;
        }

        if (!TryComp<GhostRoleTicketComponent>(uid, out var tickets))
        {
            error = "Ticket component not found.";
            return false;
        }

        if (tickets.Tickets < price)
        {
            error = $"Not enough tickets. Required: {price}, available: {tickets.Tickets}";
            return false;
        }

        tickets.Tickets -= price;
        Dirty(uid, tickets);

        _sawmill.Info($"Purchase approved, starting async token purchase...");

        _ = Task.Run(async () =>
        {
            var purchaseResult = await _antagTokens.TryPurchaseForSession(session, roleId);
            if (!purchaseResult.success)
            {
                _sawmill.Warning($"Token purchase failed for {session.Name}, refunding tickets");
                tickets.Tickets += price;
                Dirty(uid, tickets);
                return;
            }

            _sawmill.Info($"Token purchase successful, starting timer...");
            _timerSystem?.StartTimer();
            RaiseNetworkEvent(new GhostRoleTicketUpdateEvent(tickets.Tickets), session);
            RaiseLocalEvent(new GhostRolePurchasedEvent(session.UserId, roleId));
            _sawmill.Info($"=== PURCHASE COMPLETE ===");
        });

        return true;
    }

    public bool IsRoleAvailableInGameMode(string roleId)
    {
        var preset = _gameTicker.RunLevel == GameRunLevel.PreRoundLobby
            ? _gameTicker.Preset
            : _gameTicker.CurrentPreset ?? _gameTicker.Preset;

        if (preset == null)
            return true;

        if (!RoleAllowedPresets.TryGetValue(roleId, out var allowed))
            return false;

        return allowed.Contains(preset.ID);
    }

    public int GetRolePrice(string roleId)
    {
        return GhostRolePriceCatalog.TryGetPrice(roleId, out var price) ? price : 0;
    }
}
