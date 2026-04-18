// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT

using System;
using System.Collections.Generic;

namespace Content.Shared._Mini.AntagTokens;

public static class AntagTokenCatalog
{
    public const string BalanceEntryId = "balance";
    public const string MonthlyEarnedEntryId = "monthly-earned";
    public const string MonthlyYearEntryId = "monthly-year";
    public const string MonthlyMonthEntryId = "monthly-month";
    public const string LastDonorBonusClaimEntryId = "last-donor-bonus-claim";
    public const string DepositSelectionTokenId = "deposit";
    public const string DepositUsedRoleCreditEntryId = "deposit-used-role-credit";

    public const string CurrencyIconPath = "/Textures/_Mini/Interface/Antags/token_currency.png";

    public const string ThiefRole = "thief";
    public const string AgentRole = "agent";
    public const string NinjaRole = "ninja";
    public const string DragonRole = "dragon";
    public const string AbductorRole = "abductor";
    public const string InitialInfectedRole = "initial_infected";
    public const string RevenantRole = "revenant";
    public const string YaoRole = "yao";
    public const string HeadRevRole = "headrev";
    public const string CosmicCultRole = "cosmic_cult";
    public const string DevilRole = "devil";
    public const string BlobRole = "blob";
    public const string WizardRole = "wizard";
    public const string SlaughterDemonRole = "slaughter_demon";
    public const string SlasherRole = "slasher";
    public const string ChangelingRole = "changeling";
    public const string HereticRole = "heretic";
    public const string ShadowlingRole = "shadowling";
    public const string XenomorphRole = "xenomorph";
    public const string BingleRole = "bingle";

    public static readonly (TimeSpan Threshold, int RewardAmount)[] OnlineRewardMilestones =
    [
        (TimeSpan.FromHours(2), 1),
        (TimeSpan.FromHours(4), 1),
        (TimeSpan.FromHours(6), 2),
    ];

    private static readonly Dictionary<int, int> SponsorMonthlyCaps = new()
    {
        [1] = 10,
        [2] = 15,
        [3] = 20,
        [4] = 45,
        [5] = 60,
    };

    public static int? GetSponsorMonthlyCap(int sponsorLevel)
    {
        return SponsorMonthlyCaps.GetValueOrDefault(sponsorLevel);
    }

    public static string GetRoleCreditEntryId(string roleId)
    {
        return $"role-credit:{roleId}";
    }
}

public enum AntagPurchaseMode : byte
{
    LobbyDeposit,
    GhostRule,
    Unavailable,
}

public sealed record AntagRoleDefinition(
    string Id,
    string NameLocKey,
    string DescriptionLocKey,
    int Cost,
    string IconPath,
    AntagPurchaseMode Mode,
    string? AntagId = null,
    string? GameRuleId = null,
    string? TagLocKey = null,
    int MinimumPlayers = 0,
    bool RequiresInRound = false,
    bool RequiresPreRoundLobby = false,
    int MinimumTimeFromRoundStart = 0,
    string? UnavailableReasonLocKey = null,
    string? GhostRulesLocKey = null);
