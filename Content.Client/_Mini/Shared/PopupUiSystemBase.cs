using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._Mini.Shared;

public abstract class PopupUiSystemBase<TWindow, TState> : EntitySystem
    where TWindow : DefaultWindow
{
    private TWindow? _window;
    private bool _awaitingOpen;

    protected void RequestOpen(EntityEventArgs request)
    {
        _awaitingOpen = true;
        RaiseNetworkEvent(request);
    }

    protected void ReceiveState(TState state)
    {
        if (_window == null || _window.Disposed)
        {
            if (!_awaitingOpen)
                return;

            EnsureWindow();
        }

        if (_window == null)
            return;

        ApplyState(_window, state);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_window != null && !_window.Disposed)
            AdvanceTimers(_window, frameTime);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CleanupWindow();
        _awaitingOpen = false;
    }

    protected abstract TWindow CreateWindow();
    protected abstract void ApplyState(TWindow window, TState state);

    protected virtual void AdvanceTimers(TWindow window, float frameTime)
    {
    }

    protected virtual void BeforeWindowCleanup(TWindow window)
    {
    }

    private void EnsureWindow()
    {
        if (_window != null && !_window.Disposed)
            return;

        CleanupWindow();

        _window = CreateWindow();
        _window.OnClose += OnWindowClosed;
        _window.OpenCentered();
    }

    private void OnWindowClosed()
    {
        CleanupWindow();
        _awaitingOpen = false;
    }

    private void CleanupWindow()
    {
        if (_window == null)
            return;

        _window.OnClose -= OnWindowClosed;
        BeforeWindowCleanup(_window);

        if (!_window.Disposed)
            _window.Dispose();

        _window = null;
    }
}
