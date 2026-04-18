// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT
using System;
using System.Collections.Generic;
using Content.Shared._Mini.AntagTokens;
using Robust.Shared.GameStates;

namespace Content.Shared._Mini.DailyRewards;

[RegisterComponent, NetworkedComponent]
public sealed partial class DailyRewardComponent : Component
{
    [DataField]
    public TimeSpan MinimumActiveTime = TimeSpan.FromMinutes(30);

    [DataField]
    public TimeSpan ClaimCooldown = TimeSpan.FromHours(24);

    [DataField]
    public TimeSpan ExpirationWindow = TimeSpan.FromHours(36);

    [DataField]
    public int MaxStreak = 30;

    [DataField]
    public int BaseRewardEveryDays = 1;

    [DataField]
    public int BaseRewardAmount = 1;

    [DataField]
    public Dictionary<int, int> BonusTokenRewards = new()
    {
        { 15, 3 },
        { 30, 4 },
    };

    [DataField]
    public Dictionary<int, string> BonusRoleUnlockRewards = new()
    {
        { 10, AntagTokenCatalog.SlasherRole },
        { 20, AntagTokenCatalog.XenomorphRole },
    };
}
