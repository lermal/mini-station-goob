// SPDX-FileCopyrightText: 2026 Casha
//Мини-станция/Freaky-station - All rights reserved. Do not copy. Do not host.

using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Mini.GhostRolePurchase;

[RegisterComponent, NetworkedComponent]
public sealed partial class GhostRoleTicketComponent : Component
{
    [DataField]
    public int Tickets = 0;

    [DataField]
    public DateTime? LastTicketGrantTime;

    [DataField]
    public HashSet<TimeSpan> TicketMilestones = new();

    [DataField]
    public HashSet<int> StreakMilestones = new();
}
