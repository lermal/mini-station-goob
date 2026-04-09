// SPDX-FileCopyrightText: 2026 Casha
using System;
using System.Numerics;
using Content.Shared._Mini.AntagTokens;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using static Robust.Client.UserInterface.Controls.BoxContainer;
using Robust.Client.ResourceManagement;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Content.Client._Mini.AntagTokens;

public sealed class AntagTokenWindow : DefaultWindow
{
    private static readonly Color WindowBackgroundColor = Color.FromHex("#0d1117");
    private static readonly Color HeroPanelColor = Color.FromHex("#161b26").WithAlpha(0.7f);
    private static readonly Color AccentColor = Color.FromHex("#58a6ff");
    private static readonly Color CardBackgroundColor = Color.FromHex("#1a233a").WithAlpha(0.4f);
    private static readonly Color CardBorderColor = Color.FromHex("#30363d").WithAlpha(0.3f);
    private static readonly Color PurchasedCardColor = Color.FromHex("#1a4235").WithAlpha(0.5f);
    private static readonly Color PurchasedBorderColor = Color.FromHex("#3fb950").WithAlpha(0.4f);

    public event Action<string>? OnPurchasePressed;
    public event Action? OnClearPressed;

    private Label _balanceLabel = null!;
    private Label _capLabel = null!;
    private Label _depositLabel = null!;
    private Button _clearButton = null!;
    private GridContainer _roleGrid = null!;

    private const int GridColumns = 3;
    private const string CoinIconPath = "/Textures/_Mini/Interface/Coin.png";
    public AntagTokenWindow()
    {
        Title = Loc.GetString("antag-token-window-title");
        MinSize = new Vector2(1000, 650);
        SetSize = new Vector2(1000, 650);

        var root = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 16,
            Margin = new Thickness(16, 12)
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
            Modulate = Color.FromHex("#c9d1d9")
        });

        var gridPanel = new PanelContainer
        {
            VerticalExpand = true,
            // HorizontalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = Color.FromHex("#12161c"),
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
            HSeparationOverride = 12,
            VSeparationOverride = 12,
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
                ContentMarginLeftOverride = 16,
                ContentMarginTopOverride = 16,
                ContentMarginRightOverride = 16,
                ContentMarginBottomOverride = 16
            }
        };

        var content = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 20
        };
        panel.AddChild(content);

        // Левая часть с информацией
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

        // Информация в строку, а не в сетку
        var infoRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 24
        };
        left.AddChild(infoRow);

        // Баланс
        var balanceBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6
        };
        balanceBox.AddChild(new Label { Text = "Баланс:", Modulate = Color.FromHex("#8b949e") });
        _balanceLabel = new Label { Modulate = AccentColor };
        balanceBox.AddChild(_balanceLabel);
        infoRow.AddChild(balanceBox);

        // Лимит
        var capBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6
        };
        capBox.AddChild(new Label { Text = "Лимит:", Modulate = Color.FromHex("#8b949e") });
        _capLabel = new Label { Modulate = Color.FromHex("#c9d1d9") };
        capBox.AddChild(_capLabel);
        infoRow.AddChild(capBox);

        // Депозит
        var depositBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6
        };
        depositBox.AddChild(new Label { Text = "В очереди:", Modulate = Color.FromHex("#8b949e") });
        _depositLabel = new Label { Modulate = Color.FromHex("#c9d1d9") };
        depositBox.AddChild(_depositLabel);
        infoRow.AddChild(depositBox);

        // Правая часть с кнопкой
        var right = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            VerticalAlignment = VAlignment.Center
        };
        content.AddChild(right);

        _clearButton = new Button
        {
            Text = Loc.GetString("antag-token-window-clear"),
            MinSize = new Vector2(40, 36),
            Modulate = Color.White
        };
        right.AddChild(_clearButton);

        return panel;
    }

    private Control CreateRoleCard(AntagTokenRoleEntry entry)
    {
        AntagTokenCatalog.TryGetRole(entry.RoleId, out var roleDef);

        var isPurchased = entry.Purchased;
        var bgColor = isPurchased ? PurchasedCardColor : CardBackgroundColor;
        var borderColor = isPurchased ? PurchasedBorderColor : CardBorderColor;

        var panel = new PanelContainer
        {
            MinSize = new Vector2(300, 0),
            MaxSize = new Vector2(300, 1000),
            VerticalExpand = true,
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = bgColor,
                BorderColor = borderColor,
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
            SeparationOverride = 12,
            VerticalExpand = true
        };
        panel.AddChild(root);

        // Изображение роли
        var imageBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            MinSize = new Vector2(0, 140)
        };
        root.AddChild(imageBox);

        var cache = IoCManager.Resolve<IResourceCache>();

        if (roleDef != null && !string.IsNullOrEmpty(roleDef.IconPath))
        {
            var resPath = new ResPath(roleDef.IconPath);
            if (cache.TryGetResource<TextureResource>(resPath, out var textureResource))
            {
                imageBox.AddChild(new TextureRect
                {
                    Texture = textureResource.Texture,
                    MinSize = new Vector2(110, 110),
                    TextureScale = new Vector2(2.4f, 2.4f),
                    Stretch = TextureRect.StretchMode.KeepCentered
                });
            }
            else
            {
                imageBox.AddChild(new Label { Text = "🎭", Modulate = Color.White });
            }
        }
        else
        {
            imageBox.AddChild(new Label { Text = "🎭", Modulate = Color.White });
        }

        var titleLabel = new Label
        {
            Text = roleDef == null ? entry.RoleId : Loc.GetString(roleDef.NameLocKey),
            StyleClasses = { "LabelHeading" },
            Modulate = Color.White,
            HorizontalAlignment = HAlignment.Center,
            MaxWidth = 248  // Ширина карточки минус отступы (280 - 16*2 = 248)
        };
        root.AddChild(titleLabel);

        root.AddChild(new Control { VerticalExpand = true });

        // Кнопка
        var buyButton = new Button
        {
            MinSize = new Vector2(0, 36),
            HorizontalExpand = true,
            Disabled = IsButtonDisabled(entry)
        };

        var roleId = entry.RoleId;
        buyButton.OnPressed += _ => OnPurchasePressed?.Invoke(roleId);

        var buttonContent = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center
        };

        buttonContent.AddChild(new Label
        {
            Text = entry.Purchased ? "✓" : entry.Cost.ToString(),
            Modulate = Color.White,
            StyleClasses = { "LabelHeading" },
            VerticalAlignment = VAlignment.Center
        });

        var coinTexture = cache.GetResource<TextureResource>("/Textures/_Mini/Interface/Coin.png").Texture;
        buttonContent.AddChild(new TextureRect
        {
            Texture = coinTexture,
            MinSize = new Vector2(18, 18),
            TextureScale = new Vector2(0.4f, 0.4f),
            Stretch = TextureRect.StretchMode.KeepCentered,
            VerticalAlignment = VAlignment.Center
        });

        buyButton.AddChild(buttonContent);
        root.AddChild(buyButton);

        return panel;
    }



    private static string GetButtonText(AntagTokenRoleEntry entry)
    {
        if (entry.Purchased)
            return Loc.GetString("antag-token-window-button-deposited");

        return entry.Mode switch
        {
            AntagPurchaseMode.GhostRule => Loc.GetString("antag-token-window-button-ghost"),
            AntagPurchaseMode.LobbyDeposit => Loc.GetString("antag-token-window-button-deposit"),
            _ => Loc.GetString("antag-token-window-button-unavailable")
        };
    }

    private static bool IsButtonDisabled(AntagTokenRoleEntry entry)
    {
        if (entry.Purchased || !entry.Available || !entry.CanAfford)
            return true;

        if (entry.StatusLocKey == "antag-store-status-has-other-deposit")
            return true;

        return entry.Mode == AntagPurchaseMode.LobbyDeposit && entry.Saturated;
    }
}
