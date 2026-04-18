// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT
using Content.Shared._Mini.AntagTokens;
using Content.Shared._Mini.GhostRolePurchase;
using Robust.Shared.Localization;
using Robust.Shared.Timing;

namespace Content.Client._Mini.AntagTokens;

public sealed class AntagTokenUiSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    
    private AntagTokenWindow? _window;
    private bool _awaitingOpen;
    private TimeSpan _timerRemaining = TimeSpan.Zero;
    private TimeSpan _lastUpdate = TimeSpan.Zero;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<AntagTokenStateEvent>(OnState);
        SubscribeNetworkEvent<GhostRolePurchaseTimerUpdateEvent>(OnTimerUpdate);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_window == null || _window.Disposed)
            return;

        if (_timerRemaining > TimeSpan.Zero)
        {
            var now = _timing.CurTime;
            if (now - _lastUpdate >= TimeSpan.FromSeconds(1))
            {
                _lastUpdate = now;
                _timerRemaining -= TimeSpan.FromSeconds(1);
                if (_timerRemaining < TimeSpan.Zero)
                    _timerRemaining = TimeSpan.Zero;
                
                _window.UpdateTimer(_timerRemaining);
            }
        }
    }

    public void RequestOpen()
    {
        _awaitingOpen = true;
        RaiseNetworkEvent(new AntagTokenOpenRequestEvent());
    }

    private void OnState(AntagTokenStateEvent ev)
    {
        if (_window == null || _window.Disposed)
        {
            if (!_awaitingOpen)
                return;

            EnsureWindow();
        }

        _window?.UpdateState(ev.State);
    }

    private void OnTimerUpdate(GhostRolePurchaseTimerUpdateEvent ev)
    {
        _timerRemaining = ev.TimerEndTime;
        _lastUpdate = _timing.CurTime;
        _window?.UpdateTimer(_timerRemaining);
    }

    private void EnsureWindow()
    {
        if (_window != null && !_window.Disposed)
            return;

        CleanupWindow();

        _window = new AntagTokenWindow();
        _window.Title = Loc.GetString("antag-token-window-title");
        _window.OnPurchasePressed += OnPurchasePressed;
        _window.OnClearPressed += OnClearPressed;
        _window.OnClose += OnClosed;
        _window.OpenCentered();
    }

    private void OnPurchasePressed(string roleId)
    {
        RaiseNetworkEvent(new AntagTokenPurchaseRequestEvent(roleId));
    }

    private void OnClearPressed()
    {
        RaiseNetworkEvent(new AntagTokenClearRequestEvent());
    }

    private void OnClosed()
    {
        CleanupWindow();
        _awaitingOpen = false;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CleanupWindow();
        _awaitingOpen = false;
    }

    private void CleanupWindow()
    {
        if (_window == null)
            return;

        _window.OnPurchasePressed -= OnPurchasePressed;
        _window.OnClearPressed -= OnClearPressed;
        _window.OnClose -= OnClosed;

        if (!_window.Disposed)
            _window.Dispose();

        _window = null;
    }
}
