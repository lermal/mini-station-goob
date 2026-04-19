// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT
using System;
using System.Numerics;
using Content.Client.Resources;
using Content.Shared._Mini.DailyRewards;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._Mini.DailyRewards;

public sealed class DailyRewardWindow : DefaultWindow
{
    private const string ClockIconPath = "/Textures/_Mini/Interface/Clock.png";
    private const string CoinIconPath = "/Textures/_Mini/Interface/Coin.png";

    private static readonly Color WindowBackgroundColor = Color.FromHex("#0f1115");
    private static readonly Color HeroPanelColor = Color.FromHex("#1c1a24").WithAlpha(0.9f);
    private static readonly Color AccentColor = Color.FromHex("#9a8fb5");
    private static readonly Color ClaimReadyColor = Color.FromHex("#6b9e7a");
    private static readonly Color TimePanelColor = Color.FromHex("#4a7a5c").WithAlpha(0.3f);
    private static readonly Color PurchasedBorderColor = Color.FromHex("#4a7a5c").WithAlpha(0.15f);
    private static readonly Color CardBackgroundColor = Color.FromHex("#201e28").WithAlpha(0.8f);
    private static readonly Color CardBorderColor = Color.Transparent;
    private static readonly Color PurchasedCardColor = Color.FromHex("#2d2a38").WithAlpha(0.85f);
    private static readonly Color CurrentCardColor = Color.FromHex("#2c2a3a").WithAlpha(0.95f);
    private static readonly Color CurrentBorderColor = Color.Transparent;

    private static readonly Color FutureCardColor = CardBackgroundColor;
    private static readonly Color FutureBorderColor = CardBorderColor;
    private static readonly Color ClaimedCardColor = PurchasedCardColor;
    private static readonly Color ClaimedBorderColor = PurchasedBorderColor;

    public event Action? OnClaimPressed;

    private readonly IResourceCache _resourceCache;
    private readonly Label _streakValueLabel;
    private readonly Label _activeProgressLabel;
    private readonly Label _cooldownLabel;
    private readonly Label _expiryLabel;
    private readonly ProgressBar _activeProgressBar;
    private readonly Button _claimButton;
    private readonly BoxContainer _rewardTrack;
    private readonly Texture _clockTexture;
    private readonly Texture _coinTexture;
    private DailyRewardUpdateMessage? _state;

    public DailyRewardWindow()
    {
        IoCManager.InjectDependencies(this);
        _resourceCache = IoCManager.Resolve<IResourceCache>();
        _clockTexture = _resourceCache.GetTexture(ClockIconPath);
        _coinTexture = _resourceCache.GetTexture(CoinIconPath);

        Title = Loc.GetString("daily-reward-window-title");
        MinSize = new Vector2(900, 600);
        SetSize = new Vector2(900, 600);

        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 12,
            Margin = new Thickness(12)
        };

        var backdrop = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = WindowBackgroundColor }
        };
        Contents.AddChild(backdrop);
        backdrop.AddChild(root);

        root.AddChild(BuildHeroSection(out _streakValueLabel, out _activeProgressLabel, out _cooldownLabel, out _expiryLabel, out _activeProgressBar, out _claimButton));

        root.AddChild(new Label
        {
            Text = Loc.GetString("daily-reward-window-road-title"),
            StyleClasses = { "LabelHeading" },
            Margin = new Thickness(4, 0, 0, 0)
        });

        var rewardsPanel = new PanelContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#0f1623"),
                BorderColor = Color.FromHex("#31415f"),
                BorderThickness = new Thickness(2),
                ContentMarginLeftOverride = 12,
                ContentMarginTopOverride = 12,
                ContentMarginRightOverride = 12,
                ContentMarginBottomOverride = 12
            }
        };
        root.AddChild(rewardsPanel);

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            HScrollEnabled = true,
            VScrollEnabled = false
        };
        rewardsPanel.AddChild(scroll);

        _rewardTrack = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 0,
            VerticalAlignment = VAlignment.Top
        };
        scroll.AddChild(_rewardTrack);

        _claimButton.OnPressed += _ => OnClaimPressed?.Invoke();
    }

    public void UpdateState(DailyRewardUpdateMessage state)
    {
        _state = state;
        RefreshState();
    }

    public void AdvanceTimers(float frameTime)
    {
        if (_state == null)
            return;

        var step = TimeSpan.FromSeconds(frameTime);
        var timeUntilExpiration = MaxZero(_state.TimeUntilExpiration - step);
        var timeUntilNextClaim = MaxZero(_state.TimeUntilNextClaim - step);
        var currentActiveTime = _state.CurrentActiveTime;

        if (_state.IsTrackingActiveTime && currentActiveTime < _state.RequiredActiveTime)
            currentActiveTime = Min(currentActiveTime + step, _state.RequiredActiveTime);

        var canClaim = currentActiveTime >= _state.RequiredActiveTime && timeUntilNextClaim == TimeSpan.Zero;
        _state = new DailyRewardUpdateMessage(
            _state.CurrentStreak,
            _state.NextRewardDay,
            canClaim,
            _state.IsTrackingActiveTime,
            _state.HasLastClaim,
            timeUntilExpiration,
            timeUntilNextClaim,
            currentActiveTime,
            _state.RequiredActiveTime,
            _state.Rewards);

        RefreshState();
    }

    private void RefreshState()
    {
        if (_state == null)
            return;

        var state = _state;

        _streakValueLabel.Text = Loc.GetString("daily-reward-window-streak-value",
            ("current", state.CurrentStreak),
            ("max", state.Rewards.Count));

        var progressRatio = state.RequiredActiveTime <= TimeSpan.Zero
            ? 1f
            : Math.Clamp((float)(state.CurrentActiveTime.TotalSeconds / state.RequiredActiveTime.TotalSeconds), 0f, 1f);

        _activeProgressBar.MinValue = 0;
        _activeProgressBar.MaxValue = 100;
        _activeProgressBar.Value = progressRatio * 100f;

        _activeProgressLabel.Text = progressRatio >= 1f
            ? Loc.GetString("daily-reward-window-active-ready")
            : Loc.GetString("daily-reward-window-active-progress",
                ("current", FormatActiveProgress(state.CurrentActiveTime)),
                ("required", FormatActiveProgress(state.RequiredActiveTime)));

        _cooldownLabel.Text = GetAvailabilityText(state);
        _expiryLabel.Text = state.HasLastClaim
            ? Loc.GetString("daily-reward-window-expiry", ("time", FormatCooldown(state.TimeUntilExpiration)))
            : Loc.GetString("daily-reward-window-expiry-idle");

        _claimButton.Disabled = !state.CanClaim;
        _claimButton.Text = Loc.GetString(state.CanClaim
            ? "daily-reward-window-claim-ready"
            : "daily-reward-window-claim-locked");

        _rewardTrack.RemoveAllChildren();
        for (var i = 0; i < state.Rewards.Count; i++)
        {
            var reward = state.Rewards[i];
            _rewardTrack.AddChild(CreateRewardColumn(reward, state));

            if (i < state.Rewards.Count - 1)
                _rewardTrack.AddChild(CreateConnector(reward, state.Rewards[i + 1]));
        }
    }

    private Control CreateRewardColumn(DailyRewardEntry reward, DailyRewardUpdateMessage state)
    {
        var column = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 6,
            MinSize = new Vector2(150, 0)
        };

        column.AddChild(CreateRewardCard(reward));

        if (reward.IsCurrent)
        {
            var timerLabel = new Label
            {
                Text = state.CanClaim
                    ? Loc.GetString("daily-reward-card-timer-ready")
                    : Loc.GetString("daily-reward-card-timer-wait", ("time", FormatCooldown(state.TimeUntilNextClaim))),
                StyleClasses = { "LabelHeading" },
                Modulate = ClaimReadyColor,                     // текст зелёный
                HorizontalAlignment = HAlignment.Center
            };

            var timerRow = new BoxContainer
            {
                Orientation = LayoutOrientation.Horizontal,
                SeparationOverride = 6,
                HorizontalAlignment = HAlignment.Center,
                VerticalAlignment = VAlignment.Center
            };
            timerRow.AddChild(new TextureRect
            {
                Texture = _clockTexture,
                MinSize = new Vector2(16, 16),
                TextureScale = new Vector2(0.5f, 0.5f),
                Stretch = TextureRect.StretchMode.KeepCentered,
                VerticalAlignment = VAlignment.Center
            });
            timerRow.AddChild(timerLabel);

            var timerPanel = new PanelContainer
            {
                Margin = new Thickness(0, 4, 0, 0),
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = TimePanelColor.WithAlpha(0.9f),
                    BorderColor = PurchasedCardColor,
                    BorderThickness = new Thickness(0),
                    ContentMarginLeftOverride =24,
                    ContentMarginTopOverride = 4,
                    ContentMarginRightOverride =23,
                    ContentMarginBottomOverride = 4
                },
                HorizontalAlignment = HAlignment.Center
            };
            timerPanel.AddChild(timerRow);

            column.AddChild(timerPanel);
        }

        return column;
    }

    private Control BuildHeroSection(
        out Label streakValueLabel,
        out Label activeProgressLabel,
        out Label cooldownLabel,
        out Label expiryLabel,
        out ProgressBar activeProgressBar,
        out Button claimButton)
    {
        var panel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = HeroPanelColor,
                BorderColor = AccentColor,
                BorderThickness = new Thickness(2),
                ContentMarginLeftOverride = 14,
                ContentMarginTopOverride = 14,
                ContentMarginRightOverride = 14,
                ContentMarginBottomOverride = 14
            }
        };

        var content = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 14
        };
        panel.AddChild(content);

        var left = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8,
            HorizontalExpand = true
        };
        content.AddChild(left);

        left.AddChild(new Label
        {
            Text = Loc.GetString("daily-reward-window-title"),
            StyleClasses = { "LabelHeading" },
            Modulate = Color.White
        });

        left.AddChild(new Label
        {
            Text = Loc.GetString("daily-reward-window-subtitle"),
            Modulate = Color.FromHex("#c5d3ed")
        });

        var streakPanel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#0f1725"),
                BorderColor = Color.FromHex("#455674"),
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 10,
                ContentMarginTopOverride = 6,
                ContentMarginRightOverride = 10,
                ContentMarginBottomOverride = 6
            }
        };
        left.AddChild(streakPanel);

        var streakBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8
        };
        streakPanel.AddChild(streakBox);

        streakBox.AddChild(new Label
        {
            Text = Loc.GetString("daily-reward-window-streak") + ":",
            Modulate = Color.FromHex("#9fb4d8")
        });

        streakValueLabel = new Label { Modulate = AccentColor };
        streakBox.AddChild(streakValueLabel);

        activeProgressLabel = new Label { Modulate = Color.White };
        left.AddChild(CreateIconLabelRow(_clockTexture, activeProgressLabel));

        activeProgressBar = new ProgressBar
        {
            MinSize = new Vector2(0, 18),
            BackgroundStyleBoxOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#0c1220") },
            ForegroundStyleBoxOverride = new StyleBoxFlat { BackgroundColor = ClaimReadyColor }
        };
        left.AddChild(activeProgressBar);

        var statusRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 20
        };
        left.AddChild(statusRow);

        cooldownLabel = new Label { Modulate = Color.FromHex("#d7e2f4") };
        expiryLabel = new Label { Modulate = Color.FromHex("#9fb4d8") };

        var right = new PanelContainer
        {
            MinSize = new Vector2(200, 0),
            VerticalAlignment = VAlignment.Center,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#0f1725"),
                BorderColor = Color.FromHex("#455674"),
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 12,
                ContentMarginTopOverride = 12,
                ContentMarginRightOverride = 12,
                ContentMarginBottomOverride = 12
            }
        };
        content.AddChild(right);

        var rightBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center
        };
        right.AddChild(rightBox);

        rightBox.AddChild(new Label
        {
            Text = Loc.GetString("daily-reward-window-claim-panel-title"),
            StyleClasses = { "LabelHeading" },
            Modulate = Color.White
        });

        claimButton = new Button
        {
            Text = Loc.GetString("daily-reward-window-claim"),
            MinSize = new Vector2(160, 40),
            Modulate = Color.White
        };
        rightBox.AddChild(claimButton);

        return panel;
    }

    private Control CreateRewardCard(DailyRewardEntry reward)
    {
        var state = GetCardState(reward);
        var (backgroundColor, borderColor) = state switch
        {
            DailyRewardCardState.Claimed => (ClaimedCardColor, ClaimedBorderColor),
            DailyRewardCardState.Current => (CurrentCardColor, CurrentBorderColor),
            _ => (FutureCardColor, FutureBorderColor)
        };

        var panel = new PanelContainer
        {
            MinSize = new Vector2(120, 160),
            Margin = new Thickness(0, 0, 6, 0),
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = backgroundColor,
                BorderColor = borderColor,
                BorderThickness = new Thickness(2),
                ContentMarginLeftOverride = 8,
                ContentMarginTopOverride = 8,
                ContentMarginRightOverride = 8,
                ContentMarginBottomOverride = 8
            }
        };

        var box = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 6,
            VerticalAlignment = VAlignment.Center,
            HorizontalAlignment = HAlignment.Center,
            VerticalExpand = true
        };
        panel.AddChild(box);

        var header = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center
        };
        box.AddChild(header);

        header.AddChild(new Label
        {
            Text = Loc.GetString("daily-reward-card-day", ("day", reward.Day)),
            StyleClasses = { "LabelHeading" },
            Modulate = Color.White
        });

        if (reward.HasReward)
        {
            var texture = TryGetRewardTexture(reward.IconPath) ?? _coinTexture;
            bool isCoinReward = reward.IconPath == CoinIconPath;

            if (isCoinReward)
            {
                var row = new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    SeparationOverride = 4,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center
                };

                var amountText = reward.RewardName?.TrimStart('+') ?? "1";
                row.AddChild(new Label
                {
                    Text = amountText,
                    Modulate = AccentColor,
                    HorizontalAlignment = HAlignment.Center,
                    VerticalAlignment = VAlignment.Center
                });

                row.AddChild(new TextureRect
                {
                    Texture = texture,
                    MinSize = new Vector2(24, 24),
                    TextureScale = new Vector2(0.4f, 0.4f),
                    Stretch = TextureRect.StretchMode.KeepAspectCentered,
                    VerticalAlignment = VAlignment.Center
                });

                box.AddChild(row);
            }
            else
            {
                box.AddChild(new TextureRect
                {
                    Texture = texture,
                    MinSize = new Vector2(56, 56),
                    TextureScale = new Vector2(0.6f, 0.6f),
                    Stretch = TextureRect.StretchMode.KeepAspectCentered,
                    HorizontalAlignment = HAlignment.Center
                });

                box.AddChild(new Label
                {
                    Text = Loc.GetString("daily-reward-card-token", ("name", reward.RewardName ?? "-")),
                    Modulate = AccentColor,
                    HorizontalAlignment = HAlignment.Center
                });
            }
        }

        return panel;
    }

    private Control CreateConnector(DailyRewardEntry left, DailyRewardEntry right)
    {
        var claimedAhead = left.IsClaimed && right.IsClaimed;
        var currentAhead = left.IsCurrent || right.IsCurrent;
        var color = claimedAhead
            ? ClaimedBorderColor
            : currentAhead
                ? CurrentBorderColor
                : FutureBorderColor;

        return new PanelContainer
        {
            MinSize = new Vector2(15, 2),
            Margin = new Thickness(1, 70, 1, 80),
            PanelOverride = new StyleBoxFlat { BackgroundColor = color }
        };
    }

    private Control CreateIconLabelRow(Texture texture, Label label)
    {
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4
        };

        row.AddChild(new TextureRect
        {
            Texture = texture,
            MinSize = new Vector2(14, 14),
            TextureScale = new Vector2(0.4f, 0.4f),
            Stretch = TextureRect.StretchMode.KeepCentered,
            VerticalAlignment = VAlignment.Center
        });

        row.AddChild(label);
        return row;
    }

    private Control CreateCenteredIconLabelRow(Texture texture, Label label)
    {
        var row = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 4,
            HorizontalAlignment = HAlignment.Center
        };

        row.AddChild(new TextureRect
        {
            Texture = texture,
            MinSize = new Vector2(14, 14),
            TextureScale = new Vector2(0.4f, 0.4f),
            Stretch = TextureRect.StretchMode.KeepCentered,
            VerticalAlignment = VAlignment.Center
        });

        row.AddChild(label);
        return row;
    }

    private Texture? TryGetRewardTexture(string? iconPath)
    {
        if (string.IsNullOrWhiteSpace(iconPath) || !iconPath.StartsWith('/'))
            return null;

        try
        {
            var path = new ResPath(iconPath);
            return _resourceCache.TryGetResource<TextureResource>(path, out var textureResource)
                ? textureResource.Texture
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static DailyRewardCardState GetCardState(DailyRewardEntry reward)
    {
        if (reward.IsClaimed)
            return DailyRewardCardState.Claimed;
        if (reward.IsCurrent)
            return DailyRewardCardState.Current;
        return DailyRewardCardState.Future;
    }

    private static string FormatActiveProgress(TimeSpan span)
    {
        if (span <= TimeSpan.Zero)
            return "00:00";
        return $"{span.Minutes:00}:{span.Seconds:00}";
    }

    private static string FormatCooldown(TimeSpan span)
    {
        if (span <= TimeSpan.Zero)
            return "00:00";
        var totalHours = (int)span.TotalHours;
        return $"{totalHours:00}:{span.Minutes:00}";
    }

    private static string GetAvailabilityText(DailyRewardUpdateMessage state)
    {
        if (state.TimeUntilNextClaim > TimeSpan.Zero)
            return Loc.GetString("daily-reward-window-cooldown-wait", ("time", FormatCooldown(state.TimeUntilNextClaim)));
        if (state.CurrentActiveTime < state.RequiredActiveTime)
            return Loc.GetString("daily-reward-window-active-needed");
        return Loc.GetString("daily-reward-window-cooldown-ready");
    }

    private static TimeSpan MaxZero(TimeSpan span) => span < TimeSpan.Zero ? TimeSpan.Zero : span;
    private static TimeSpan Min(TimeSpan left, TimeSpan right) => left <= right ? left : right;

    private enum DailyRewardCardState : byte
    {
        Claimed,
        Current,
        Future
    }
}
