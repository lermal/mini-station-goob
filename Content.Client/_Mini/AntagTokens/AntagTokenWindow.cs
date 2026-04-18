// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT
using System;
using System.Numerics;
using Content.Client.Resources;
using Content.Shared._Mini.AntagTokens;
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

namespace Content.Client._Mini.AntagTokens;

public sealed class AntagTokenWindow : DefaultWindow
{
    private static readonly Color WindowBackgroundColor = Color.FromHex("#0b0f14");
    private static readonly Color HeroPanelColor = Color.FromHex("#131822").WithAlpha(0.9f);
    private static readonly Color AccentColor = Color.FromHex("#539bf5");
    private static readonly Color CardBackgroundColor = Color.FromHex("#1c2128").WithAlpha(0.6f);
    private static readonly Color CardBorderColor = Color.FromHex("#3d444d").WithAlpha(0.5f);
    private static readonly Color PurchasedCardColor = Color.FromHex("#1e4d3a").WithAlpha(0.7f);
    private static readonly Color PurchasedBorderColor = Color.FromHex("#3fb950").WithAlpha(0.8f);

    private const int GridColumns = 3;
    private const string CoinIconPath = "/Textures/_Mini/Interface/Coin.png";

    public event Action<string>? OnPurchasePressed;
    public event Action? OnClearPressed;

    private readonly IResourceCache _resourceCache;
    private readonly Texture _coinTexture;

    private Label _balanceLabel = null!;
    private Label _capLabel = null!;
    private Label _depositLabel = null!;
    private Button _clearButton = null!;
    private GridContainer _roleGrid = null!;

    public AntagTokenWindow()
    {
        IoCManager.InjectDependencies(this);
        _resourceCache = IoCManager.Resolve<IResourceCache>();
        _coinTexture = _resourceCache.GetTexture(CoinIconPath);

        Title = Loc.GetString("antag-token-window-title");
        MinSize = new Vector2(1000, 650);
        SetSize = new Vector2(1000, 650);

        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 24,
            Margin = new Thickness(24, 16)
        };

        var backdrop = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat { BackgroundColor = WindowBackgroundColor }
        };
        Contents.AddChild(backdrop);
        backdrop.AddChild(root);

        root.AddChild(BuildHero());

        root.AddChild(new Label
        {
            Text = Loc.GetString("antag-token-window-roles-title"),
            StyleClasses = { "LabelHeading" },
            Margin = new Thickness(4, 0, 0, 0),
            Modulate = Color.FromHex("#adbac7")
        });

        var gridPanel = new PanelContainer
        {
            VerticalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#10141c"),
                BorderColor = Color.FromHex("#2d3748").WithAlpha(0.3f),
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 12,
                ContentMarginTopOverride = 12,
                ContentMarginRightOverride = 12,
                ContentMarginBottomOverride = 12
            }
        };
        root.AddChild(gridPanel);

        var scroll = new ScrollContainer
        {
            VerticalExpand = true,
            HorizontalExpand = true,
            HScrollEnabled = false
        };
        gridPanel.AddChild(scroll);

        _roleGrid = new GridContainer
        {
            Columns = GridColumns,
            HSeparationOverride = 16,
            VSeparationOverride = 16,
            HorizontalAlignment = HAlignment.Stretch
        };
        scroll.AddChild(_roleGrid);

        _clearButton.OnPressed += _ => OnClearPressed?.Invoke();
    }

    public void UpdateState(AntagTokenState state)
    {
        _balanceLabel.Text = Loc.GetString("antag-token-window-balance", ("amount", state.Balance));
        _capLabel.Text = state.MonthlyCap.HasValue
            ? Loc.GetString("antag-token-window-cap", ("earned", state.MonthlyEarned), ("cap", state.MonthlyCap.Value))
            : Loc.GetString("antag-token-window-cap-free", ("earned", state.MonthlyEarned));

        _depositLabel.Text = state.ActiveDepositRoleId != null &&
                             AntagTokenCatalog.TryGetRole(state.ActiveDepositRoleId, out var role)
            ? Loc.GetString("antag-token-window-deposit", ("role", Loc.GetString(role.NameLocKey)))
            : Loc.GetString("antag-token-window-no-deposit");

        _clearButton.Disabled = state.ActiveDepositRoleId == null;

        _roleGrid.RemoveAllChildren();
        foreach (var roleEntry in state.Roles)
        {
            _roleGrid.AddChild(CreateRoleCard(roleEntry));
        }
    }

    private Control BuildHero()
    {
        var panel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = HeroPanelColor,
                BorderColor = AccentColor.WithAlpha(0.2f),
                BorderThickness = new Thickness(0, 0, 0, 1),
                ContentMarginLeftOverride = 20,
                ContentMarginTopOverride = 20,
                ContentMarginRightOverride = 20,
                ContentMarginBottomOverride = 20
            }
        };

        var content = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 20
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
            Text = Loc.GetString("antag-token-window-title"),
            StyleClasses = { "LabelHeading" },
            Modulate = Color.White
        });

        left.AddChild(new Label
        {
            Text = Loc.GetString("antag-token-window-subtitle"),
            Modulate = Color.FromHex("#8b949e")
        });

        var infoRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 24
        };
        left.AddChild(infoRow);

        var balanceBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6
        };
        balanceBox.AddChild(new Label
        {
            Text = "Баланс:",
            Modulate = Color.FromHex("#8b949e")
        });
        _balanceLabel = new Label
        {
            Modulate = AccentColor
        };
        balanceBox.AddChild(_balanceLabel);
        infoRow.AddChild(balanceBox);

        var capBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6
        };
        capBox.AddChild(new Label
        {
            Text = "Лимит:",
            Modulate = Color.FromHex("#8b949e")
        });
        _capLabel = new Label
        {
            Modulate = Color.FromHex("#adbac7")
        };
        capBox.AddChild(_capLabel);
        infoRow.AddChild(capBox);

        var depositBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6
        };
        depositBox.AddChild(new Label
        {
            Text = "В очереди:",
            Modulate = Color.FromHex("#8b949e")
        });
        _depositLabel = new Label
        {
            Modulate = Color.FromHex("#adbac7")
        };
        depositBox.AddChild(_depositLabel);
        infoRow.AddChild(depositBox);

        var right = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            VerticalAlignment = VAlignment.Center
        };
        content.AddChild(right);

        _clearButton = new Button
        {
            Text = Loc.GetString("antag-token-window-clear"),
            MinSize = new Vector2(120, 38),
            Modulate = Color.White
        };
        right.AddChild(_clearButton);

        return panel;
    }

    private Control CreateRoleCard(AntagTokenRoleEntry entry)
    {
        AntagTokenCatalog.TryGetRole(entry.RoleId, out var roleDef);

        var panel = new PanelContainer
        {
            MinSize = new Vector2(290, 0),
            MaxSize = new Vector2(290, 1000),
            VerticalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = entry.Purchased ? PurchasedCardColor : CardBackgroundColor,
                BorderColor = entry.Purchased ? PurchasedBorderColor : CardBorderColor,
                BorderThickness = new Thickness(1),
                ContentMarginLeftOverride = 16,
                ContentMarginTopOverride = 16,
                ContentMarginRightOverride = 16,
                ContentMarginBottomOverride = 16
            }
        };

        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 8,
            VerticalExpand = true
        };
        panel.AddChild(root);

        var imageBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            MinSize = new Vector2(0, 140)
        };
        root.AddChild(imageBox);

        if (roleDef != null &&
            !string.IsNullOrWhiteSpace(roleDef.IconPath) &&
            _resourceCache.TryGetResource<TextureResource>(new ResPath(roleDef.IconPath), out var textureResource))
        {
            imageBox.AddChild(new TextureRect
            {
                Texture = textureResource.Texture,
                MinSize = new Vector2(96, 96),
                MaxSize = new Vector2(96, 96),
                Stretch = TextureRect.StretchMode.KeepAspectCentered
            });
        }
        else
        {
            imageBox.AddChild(new Label { Text = "?", Modulate = Color.White });
        }

        root.AddChild(new Label
        {
            Text = roleDef == null ? entry.RoleId : Loc.GetString(roleDef.NameLocKey),
            StyleClasses = { "LabelHeading" },
            Modulate = Color.White,
            HorizontalAlignment = HAlignment.Center,
            MaxWidth = 268
        });

        if (entry.TagLocKey != null)
        {
            root.AddChild(new Label
            {
                Text = Loc.GetString(entry.TagLocKey),
                Modulate = AccentColor,
                HorizontalAlignment = HAlignment.Center,
                MaxWidth = 268
            });
        }

        if (entry.StatusLocKey != null)
        {
            root.AddChild(new Label
            {
                Text = Loc.GetString(entry.StatusLocKey),
                Modulate = entry.Purchased ? PurchasedBorderColor : Color.FromHex("#e5534b"),
                HorizontalAlignment = HAlignment.Center,
                MaxWidth = 268
            });
        }

        if (entry.FreeUnlocks > 0)
        {
            root.AddChild(new Label
            {
                Text = Loc.GetString("antag-token-window-free-unlocks", ("amount", entry.FreeUnlocks)),
                Modulate = AccentColor,
                HorizontalAlignment = HAlignment.Center,
                MaxWidth = 268
            });
        }

        root.AddChild(new Control { VerticalExpand = true });

        var buyButton = new Button
        {
            MinSize = new Vector2(268, 40),
            MaxSize = new Vector2(268, 40),
            Disabled = IsButtonDisabled(entry),
            ToolTip = GetButtonTooltip(entry)
        };

        var roleId = entry.RoleId;
        buyButton.OnPressed += _ => OnPurchasePressed?.Invoke(roleId);

        var buttonContent = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center
        };

        if (entry.Purchased)
        {
            buttonContent.AddChild(new Label
            {
                Text = "✓",
                Modulate = Color.White,
                StyleClasses = { "LabelHeading" },
                VerticalAlignment = VAlignment.Center
            });
        }
        else if (entry.FreeUnlocks > 0)
        {
            buttonContent.AddChild(new Label
            {
                Text = Loc.GetString("antag-token-window-button-free"),
                Modulate = Color.White,
                VerticalAlignment = VAlignment.Center
            });
        }
        else
        {
            buttonContent.AddChild(new Label
            {
                Text = entry.Cost.ToString(),
                Modulate = Color.White,
                StyleClasses = { "LabelHeading" },
                VerticalAlignment = VAlignment.Center
            });

            buttonContent.AddChild(new TextureRect
            {
                Texture = _coinTexture,
                MinSize = new Vector2(16, 16),
                MaxSize = new Vector2(16, 16),
                TextureScale = new Vector2(0.4f, 0.4f),
                Stretch = TextureRect.StretchMode.KeepCentered,
                VerticalAlignment = VAlignment.Center
            });
        }

        buyButton.AddChild(buttonContent);
        root.AddChild(buyButton);

        return panel;
    }

    private static bool IsButtonDisabled(AntagTokenRoleEntry entry)
    {
        if (entry.Purchased || !entry.Available || !entry.CanAfford)
            return true;

        if (entry.StatusLocKey == "antag-store-status-has-other-deposit")
            return true;

        return entry.Mode == AntagPurchaseMode.LobbyDeposit && entry.Saturated;
    }

    private static string GetButtonTooltip(AntagTokenRoleEntry entry)
    {
        if (entry.StatusLocKey != null)
            return Loc.GetString(entry.StatusLocKey);

        if (entry.FreeUnlocks > 0)
            return Loc.GetString("antag-token-window-tooltip-free");

        return entry.Mode switch
        {
            AntagPurchaseMode.GhostRule => Loc.GetString("antag-token-window-tooltip-ghost"),
            AntagPurchaseMode.LobbyDeposit => Loc.GetString("antag-token-window-tooltip-deposit"),
            _ => Loc.GetString("antag-token-window-button-unavailable")
        };
    }
}
