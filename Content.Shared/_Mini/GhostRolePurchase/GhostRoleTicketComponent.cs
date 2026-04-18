// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

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
