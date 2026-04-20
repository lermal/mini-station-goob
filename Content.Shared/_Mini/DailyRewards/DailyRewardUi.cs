// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT
using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;

namespace Content.Shared._Mini.DailyRewards;

[Serializable, NetSerializable]
public enum DailyRewardUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class DailyRewardUpdateMessage(
    int currentStreak,
    int nextRewardDay,
    bool canClaim,
    bool isTrackingActiveTime,
    bool hasLastClaim,
    TimeSpan timeUntilExpiration,
    TimeSpan timeUntilNextClaim,
    TimeSpan currentActiveTime,
    TimeSpan requiredActiveTime,
    List<DailyRewardEntry> rewards,
    TimeSpan onlineElapsed,
    List<TimeSpan> onlineGrantedThresholds) : BoundUserInterfaceState
{
    public int CurrentStreak { get; } = currentStreak;
    public int NextRewardDay { get; } = nextRewardDay;
    public bool CanClaim { get; } = canClaim;
    public bool IsTrackingActiveTime { get; } = isTrackingActiveTime;
    public bool HasLastClaim { get; } = hasLastClaim;
    public TimeSpan TimeUntilExpiration { get; } = timeUntilExpiration;
    public TimeSpan TimeUntilNextClaim { get; } = timeUntilNextClaim;
    public TimeSpan CurrentActiveTime { get; } = currentActiveTime;
    public TimeSpan RequiredActiveTime { get; } = requiredActiveTime;
    public List<DailyRewardEntry> Rewards { get; } = rewards;
    public TimeSpan OnlineElapsed { get; } = onlineElapsed;
    public List<TimeSpan> OnlineGrantedThresholds { get; } = onlineGrantedThresholds;
}

[Serializable, NetSerializable]
public sealed class DailyRewardClaimMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class DailyRewardOpenRequestEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class DailyRewardClaimRequestEvent : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed class DailyRewardStateEvent(DailyRewardUpdateMessage state) : EntityEventArgs
{
    public DailyRewardUpdateMessage State { get; } = state;
}

[Serializable, NetSerializable]
public sealed class DailyRewardEntry(
    int day,
    string? rewardName,
    bool hasReward,
    string iconPath,
    bool isClaimed,
    bool isCurrent)
{
    public int Day { get; } = day;
    public string? RewardName { get; } = rewardName;
    public bool HasReward { get; } = hasReward;
    public string IconPath { get; } = iconPath;
    public bool IsClaimed { get; } = isClaimed;
    public bool IsCurrent { get; } = isCurrent;
}
