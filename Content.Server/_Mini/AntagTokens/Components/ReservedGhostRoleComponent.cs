// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

using Robust.Shared.Network;

namespace Content.Server._Mini.AntagTokens.Components;

[RegisterComponent]
public sealed partial class ReservedGhostRoleComponent : Component
{
    public NetUserId ReservedUserId;
}
