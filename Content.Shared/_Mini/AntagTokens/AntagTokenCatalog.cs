// SPDX-FileCopyrightText: 2026 Casha
//Мини-станция/Freaky-station - All rights reserved. Do not copy. Do not host.

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

    private static readonly Dictionary<string, AntagRoleDefinition> RoleDefinitions = new()
    {
        [ThiefRole] = new(ThiefRole, "antag-store-role-thief-name", "antag-store-role-thief-description", 1,
            "/Textures/_Mini/Interface/Antags/thief.png", AntagPurchaseMode.LobbyDeposit, "Thief", "OopsAllThieves",
            TagLocKey: "antag-token-window-tag-queue",
            RequiresPreRoundLobby: true),
        [AgentRole] = new(AgentRole, "antag-store-role-agent-name", "antag-store-role-agent-description", 2,
            "/Textures/_Mini/Interface/Antags/traitor.png", AntagPurchaseMode.LobbyDeposit, "Traitor", "Traitor",
            TagLocKey: "antag-token-window-tag-queue",
            RequiresPreRoundLobby: true),
        [NinjaRole] = new(NinjaRole, "antag-store-role-ninja-name", "antag-store-role-ninja-description", 3,
            "/Textures/_Mini/Interface/Antags/ninja.png", AntagPurchaseMode.GhostRule, null, "TokenNinjaSpawn",
            TagLocKey: "antag-token-window-tag-ghost",
            RequiresInRound: true),
        [DragonRole] = new(DragonRole, "antag-store-role-dragon-name", "antag-store-role-dragon-description", 7,
            "/Textures/_Mini/Interface/Antags/dragon.png", AntagPurchaseMode.GhostRule, null, "TokenDragonSpawn",
            TagLocKey: "antag-token-window-tag-ghost",
            MinimumPlayers: 25,
            RequiresInRound: true),
        [AbductorRole] = new(AbductorRole, "antag-store-role-abductor-name", "antag-store-role-abductor-description", 3,
            "/Textures/_Mini/Interface/Antags/abductor.png", AntagPurchaseMode.GhostRule, null, "TokenLoneAbductorSpawn",
            TagLocKey: "antag-token-window-tag-ghost",
            RequiresInRound: true),
        [InitialInfectedRole] = new(InitialInfectedRole, "antag-store-role-initial-infected-name", "antag-store-role-initial-infected-description", 8,
            "/Textures/_Mini/Interface/Antags/zombie.png", AntagPurchaseMode.LobbyDeposit, "InitialInfected", "Zombie",
            TagLocKey: "antag-token-window-tag-queue",
            MinimumPlayers: 30,
            RequiresPreRoundLobby: true),
        [RevenantRole] = new(RevenantRole, "antag-store-role-revenant-name", "antag-store-role-revenant-description", 3,
            "/Textures/_Mini/Interface/Antags/revenant.png", AntagPurchaseMode.GhostRule, null, "TokenRevenantSpawn",
            TagLocKey: "antag-token-window-tag-ghost",
            RequiresInRound: true),
        [YaoRole] = new(YaoRole, "antag-store-role-yao-name", "antag-store-role-yao-description", 6,
            "/Textures/_Mini/Interface/Antags/nukie.png", AntagPurchaseMode.GhostRule, null, "TokenLoneOpsSpawn",
            TagLocKey: "antag-token-window-tag-ghost",
            MinimumPlayers: 25,
            RequiresInRound: true),
        [HeadRevRole] = new(HeadRevRole, "antag-store-role-headrev-name", "antag-store-role-headrev-description", 6,
            "/Textures/_Mini/Interface/Antags/rev.png", AntagPurchaseMode.LobbyDeposit, "HeadRev", "Revolutionary",
            TagLocKey: "antag-token-window-tag-queue",
            MinimumPlayers: 30,
            RequiresPreRoundLobby: true),
        [CosmicCultRole] = new(CosmicCultRole, "antag-store-role-cosmic-cult-name", "antag-store-role-cosmic-cult-description", 4,
            "/Textures/_Mini/Interface/Antags/cultist.png", AntagPurchaseMode.LobbyDeposit, "CosmicAntagCultist", "CosmicCult",
            TagLocKey: "antag-token-window-tag-queue",
            MinimumPlayers: 20,
            RequiresPreRoundLobby: true),
        [DevilRole] = new(DevilRole, "antag-store-role-devil-name", "antag-store-role-devil-description", 3,
            "/Textures/_Mini/Interface/Antags/devil.png", AntagPurchaseMode.LobbyDeposit, "Devil", "Devil",
            TagLocKey: "antag-token-window-tag-queue",
            RequiresPreRoundLobby: true),
        [BlobRole] = new(BlobRole, "antag-store-role-blob-name", "antag-store-role-blob-description", 20,
            "/Textures/_Mini/Interface/Antags/blob.png", AntagPurchaseMode.GhostRule, null, "TokenBlobMidround",
            TagLocKey: "antag-token-window-tag-ghost",
            MinimumPlayers: 40,
            RequiresInRound: true),
        [WizardRole] = new(WizardRole, "antag-store-role-wizard-name", "antag-store-role-wizard-description", 15,
            "/Textures/_Mini/Interface/Antags/wizard.png", AntagPurchaseMode.GhostRule, null, "TokenWizard",
            TagLocKey: "antag-token-window-tag-ghost",
            RequiresInRound: true),
        [SlaughterDemonRole] = new(SlaughterDemonRole, "antag-store-role-slaughter-demon-name", "antag-store-role-slaughter-demon-description", 6,
            "/Textures/_Mini/Interface/Antags/slaughter_demon.png", AntagPurchaseMode.GhostRule, null, "TokenSlaughterDemon",
            TagLocKey: "antag-token-window-tag-ghost",
            MinimumPlayers: 30,
            RequiresInRound: true),
        [SlasherRole] = new(SlasherRole, "antag-store-role-slasher-name", "antag-store-role-slasher-description", 6,
            "/Textures/_Mini/Interface/Antags/slasher.png", AntagPurchaseMode.GhostRule, null, "TokenSlasherSpawn",
            TagLocKey: "antag-token-window-tag-ghost",
            MinimumPlayers: 25,
            RequiresInRound: true),
        [ChangelingRole] = new(ChangelingRole, "antag-store-role-changeling-name", "antag-store-role-changeling-description", 10,
            "/Textures/_Mini/Interface/Antags/changeling.png", AntagPurchaseMode.LobbyDeposit, "Changeling", "Changeling",
            TagLocKey: "antag-token-window-tag-queue",
            MinimumPlayers: 25,
            RequiresPreRoundLobby: true),
        [HereticRole] = new(HereticRole, "antag-store-role-heretic-name", "antag-store-role-heretic-description", 6,
            "/Textures/_Mini/Interface/Antags/heretic.png", AntagPurchaseMode.LobbyDeposit, "Heretic", "Heretic",
            TagLocKey: "antag-token-window-tag-queue",
            MinimumPlayers: 20,
            RequiresPreRoundLobby: true),
        [ShadowlingRole] = new(ShadowlingRole, "antag-store-role-shadowling-name", "antag-store-role-shadowling-description", 8,
            "/Textures/_Mini/Interface/Antags/shadowling.png", AntagPurchaseMode.LobbyDeposit, "Shadowling", "Shadowling",
            TagLocKey: "antag-token-window-tag-queue",
            MinimumPlayers: 30,
            RequiresPreRoundLobby: true),
        [XenomorphRole] = new(XenomorphRole, "antag-store-role-xenomorph-name", "antag-store-role-xenomorph-description", 8,
            "/Textures/_Mini/Interface/Antags/xenomorph.png", AntagPurchaseMode.LobbyDeposit, "XenomorphsInfestationRoundstart", "XenomorphsInfestationRoundstart",
            TagLocKey: "antag-token-window-tag-queue",
            MinimumPlayers: 35,
            RequiresPreRoundLobby: true),
        [BingleRole] = new(BingleRole, "antag-store-role-bingle-name", "antag-store-role-bingle-description", 3,
            "/Textures/_Mini/Interface/Antags/bingle.png", AntagPurchaseMode.GhostRule, null, "TokenBingleSpawn",
            TagLocKey: "antag-token-window-tag-ghost",
            MinimumPlayers: 15,
            RequiresInRound: true),
    };

    public static IReadOnlyDictionary<string, AntagRoleDefinition> Roles => RoleDefinitions;

    public static bool TryGetRole(string roleId, out AntagRoleDefinition definition)
    {
        return RoleDefinitions.TryGetValue(roleId, out definition!);
    }

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
    string? UnavailableReasonLocKey = null);
