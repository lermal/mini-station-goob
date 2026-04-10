// SPDX-FileCopyrightText: 2026 Casha
//Мини-станция/Freaky-station - All rights reserved. Do not copy. Do not host.

using Robust.Shared.Network;

namespace Content.Server._Mini.AntagTokens.Components;

[RegisterComponent]
public sealed partial class ReservedGhostRoleComponent : Component
{
    public NetUserId ReservedUserId;
}
