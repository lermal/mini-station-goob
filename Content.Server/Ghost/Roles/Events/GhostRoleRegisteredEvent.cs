// SPDX-FileCopyrightText: 2026
//
// SPDX-License-Identifier: MIT

using Robust.Shared.GameObjects;

namespace Content.Server.Ghost.Roles.Events;

/// <summary>
/// Raised on an entity after its ghost role is registered in the ghost role list.
/// </summary>
public sealed class GhostRoleRegisteredEvent : EntityEventArgs;
