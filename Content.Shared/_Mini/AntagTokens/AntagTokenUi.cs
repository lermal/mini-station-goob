// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Shared._Mini.AntagTokens;

[Serializable, NetSerializable]
public sealed class AntagTokenOpenRequestEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class AntagTokenPurchaseRequestEvent(string roleId) : EntityEventArgs
{
    public string RoleId { get; } = roleId;
}

[Serializable, NetSerializable]
public sealed class AntagTokenClearRequestEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class AntagTokenStateEvent(AntagTokenState state) : EntityEventArgs
{
    public AntagTokenState State { get; } = state;
}

[Serializable, NetSerializable]
public sealed class AntagTokenState(
    int balance,
    int monthlyEarned,
    int? monthlyCap,
    string? activeDepositRoleId,
    List<AntagTokenRoleEntry> roles) : BoundUserInterfaceState
{
    public int Balance { get; } = balance;
    public int MonthlyEarned { get; } = monthlyEarned;
    public int? MonthlyCap { get; } = monthlyCap;
    public string? ActiveDepositRoleId { get; } = activeDepositRoleId;
    public List<AntagTokenRoleEntry> Roles { get; } = roles;
}

[Serializable, NetSerializable]
public sealed class AntagTokenRoleEntry(
    string roleId,
    int cost,
    AntagPurchaseMode mode,
    bool purchased,
    int freeUnlocks,
    bool canAfford,
    bool saturated,
    bool available,
    string? tagLocKey,
    string? statusLocKey)
{
    public string RoleId { get; } = roleId;
    public int Cost { get; } = cost;
    public AntagPurchaseMode Mode { get; } = mode;
    public bool Purchased { get; } = purchased;
    public int FreeUnlocks { get; } = freeUnlocks;
    public bool CanAfford { get; } = canAfford;
    public bool Saturated { get; } = saturated;
    public bool Available { get; } = available;
    public string? TagLocKey { get; } = tagLocKey;
    public string? StatusLocKey { get; } = statusLocKey;
}
