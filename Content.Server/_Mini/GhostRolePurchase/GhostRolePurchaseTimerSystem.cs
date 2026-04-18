// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Shared._Mini.GhostRolePurchase;
using Content.Shared.GameTicking;
using Robust.Server.Player;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Mini.GhostRolePurchase;
public sealed class GhostRolePurchaseTimerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        _sawmill = Logger.GetSawmill("ghost_role_timer");
        
        _sawmill.Info("=== GhostRolePurchaseTimerSystem Initialize() called ===");
        
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
        SubscribeLocalEvent<GhostRolePurchasedEvent>(OnGhostRolePurchased);
        SubscribeLocalEvent<PlayerJoinedLobbyEvent>(OnPlayerJoinedLobby);
        
        _sawmill.Info("GhostRolePurchaseTimerSystem initialized and subscribed to events");
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<GhostRolePurchaseTimerComponent>();
        while (query.MoveNext(out var uid, out var timer))
        {
            if (!timer.IsBlocked)
                continue;

            if (timer.TimerEndTime == null)
                continue;

            if (_timing.CurTime >= timer.TimerEndTime.Value)
            {
                timer.IsBlocked = false;
                timer.TimerEndTime = null;
                Dirty(uid, timer);
                
                foreach (var session in _playerManager.Sessions)
                {
                    RaiseNetworkEvent(new GhostRolePurchaseTimerUpdateEvent(TimeSpan.Zero), session);
                }
            }
        }
    }

    private void OnRoundStarted(RoundStartedEvent ev)
    {
        _sawmill.Info($"Round started (ID: {ev.RoundId}), starting ghost role purchase timer");
        StartTimer();
    }

    private void OnGhostRolePurchased(GhostRolePurchasedEvent ev)
    {
        _sawmill.Info($"Ghost role purchased by {ev.UserId}, restarting timer");
        StartTimer();
    }

    private void OnPlayerJoinedLobby(PlayerJoinedLobbyEvent ev)
    {
        SendCurrentTimerState(ev.PlayerSession);
    }

    private void SendCurrentTimerState(ICommonSession session)
    {
        var timerEntity = GetOrCreateTimerEntity();
        if (timerEntity == null)
        {
            RaiseNetworkEvent(new GhostRolePurchaseTimerUpdateEvent(TimeSpan.Zero), session);
            return;
        }

        if (!TryComp<GhostRolePurchaseTimerComponent>(timerEntity.Value, out var timer))
        {
            RaiseNetworkEvent(new GhostRolePurchaseTimerUpdateEvent(TimeSpan.Zero), session);
            return;
        }

        if (!timer.IsBlocked || !timer.TimerEndTime.HasValue)
        {
            RaiseNetworkEvent(new GhostRolePurchaseTimerUpdateEvent(TimeSpan.Zero), session);
            return;
        }

        var remaining = timer.TimerEndTime.Value - _timing.CurTime;
        RaiseNetworkEvent(new GhostRolePurchaseTimerUpdateEvent(remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero), session);
    }

    public void StartTimer()
    {
        var timerEntity = GetOrCreateTimerEntity();
        if (timerEntity == null)
        {
            _sawmill.Error("Failed to get or create timer entity!");
            return;
        }

        var timer = Comp<GhostRolePurchaseTimerComponent>(timerEntity.Value);
        timer.TimerEndTime = _timing.CurTime + timer.BlockDuration;
        timer.IsBlocked = true;
        Dirty(timerEntity.Value, timer);

        _sawmill.Info($"Timer started. Duration: {timer.BlockDuration.TotalMinutes} minutes, End time: {timer.TimerEndTime}");

        var remaining = timer.BlockDuration;
        foreach (var session in _playerManager.Sessions)
        {
            RaiseNetworkEvent(new GhostRolePurchaseTimerUpdateEvent(remaining), session);
        }
        
        _sawmill.Info($"Sent timer update to {_playerManager.Sessions.Length} players");
    }

    public bool IsTimerActive()
    {
        _sawmill.Debug("IsTimerActive() called");
        
        var timerEntity = GetOrCreateTimerEntity();
        if (timerEntity == null)
        {
            _sawmill.Warning("IsTimerActive: timer entity is null!");
            return false;
        }

        if (!TryComp<GhostRolePurchaseTimerComponent>(timerEntity.Value, out var timer))
        {
            _sawmill.Warning("IsTimerActive: timer component not found!");
            return false;
        }

        var isActive = timer.IsBlocked && timer.TimerEndTime.HasValue && _timing.CurTime < timer.TimerEndTime.Value;
        
        _sawmill.Debug($"IsTimerActive: IsBlocked={timer.IsBlocked}, HasEndTime={timer.TimerEndTime.HasValue}, Result={isActive}");
        
        if (isActive && timer.TimerEndTime.HasValue)
        {
            var remaining = timer.TimerEndTime.Value - _timing.CurTime;
            _sawmill.Debug($"Timer is active. Remaining: {remaining.TotalSeconds:F1}s");
        }
        
        return isActive;
    }

    public TimeSpan GetRemainingTime()
    {
        var timerEntity = GetOrCreateTimerEntity();
        if (timerEntity == null)
            return TimeSpan.Zero;

        if (!TryComp<GhostRolePurchaseTimerComponent>(timerEntity.Value, out var timer))
            return TimeSpan.Zero;

        if (!timer.IsBlocked || !timer.TimerEndTime.HasValue)
            return TimeSpan.Zero;

        var remaining = timer.TimerEndTime.Value - _timing.CurTime;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }

    private EntityUid? GetOrCreateTimerEntity()
    {
        var query = EntityQueryEnumerator<GhostRolePurchaseTimerComponent>();
        if (query.MoveNext(out var uid, out _))
            return uid;

        var timerEntity = EntityManager.SpawnEntity(null, MapCoordinates.Nullspace);
        EnsureComp<GhostRolePurchaseTimerComponent>(timerEntity);
        return timerEntity;
    }
}
