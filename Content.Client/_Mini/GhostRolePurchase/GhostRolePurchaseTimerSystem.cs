using Content.Shared._Mini.GhostRolePurchase;
using Robust.Shared.Timing;

namespace Content.Client._Mini.GhostRolePurchase;

public sealed class GhostRolePurchaseTimerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    
    private TimeSpan? _timerEndTime;
    
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<GhostRolePurchaseTimerUpdateEvent>(OnTimerUpdate);
    }
    
    private void OnTimerUpdate(GhostRolePurchaseTimerUpdateEvent ev)
    {
        if (ev.TimerEndTime == TimeSpan.Zero)
        {
            _timerEndTime = null;
        }
        else
        {
            _timerEndTime = _timing.CurTime + ev.TimerEndTime;
        }
    }
    
    public bool IsTimerActive()
    {
        return _timerEndTime.HasValue && _timing.CurTime < _timerEndTime.Value;
    }
    
    public TimeSpan GetRemainingTime()
    {
        if (!_timerEndTime.HasValue)
            return TimeSpan.Zero;
            
        var remaining = _timerEndTime.Value - _timing.CurTime;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}