// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Mini.GhostRolePurchase;

[RegisterComponent, NetworkedComponent]
public sealed partial class GhostRolePurchaseTimerComponent : Component
{
    [DataField]
    public TimeSpan? TimerEndTime;

    [DataField]
    public bool IsBlocked = false;

    [DataField]
    public TimeSpan BlockDuration = TimeSpan.FromMinutes(15);
}
