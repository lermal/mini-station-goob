using Robust.Client;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using Robust.Client.Console;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Input;
using Robust.Shared.Enums;

namespace Content.Client.Lobby.UI;

public sealed class ServerListBox : BoxContainer
{
    [Dependency] private readonly IClientConsoleHost _consoleHost = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;
    private IGameController _gameController;
    private List<Button> _connectButtons = new();
    private IUriOpener _uriOpener;

    private void OpenDailyRewards()
    {
        _consoleHost.ExecuteCommand("dailyrewardmenu");
    }

    private void OpenAntagTokens()
    {
        _consoleHost.ExecuteCommand("antagtokenmenu");
    }

    public ServerListBox()
    {
        IoCManager.InjectDependencies(this);

        _gameController = IoCManager.Resolve<IGameController>();
        _uriOpener = IoCManager.Resolve<IUriOpener>();
        Orientation = LayoutOrientation.Vertical;

        // Добавляем контейнер для кнопок действий (вертикальное расположение)
        var actionButtonsContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
            Margin = new Thickness(0, 0, 0, 10)
        };

        // Кнопка ежедневных наград
        var dailyRewardsButton = CreateActionButton(
            "Награды",
            "/Textures/_Mini/Interface/Clock.png", // Путь к вашей иконке (можно null)
            OpenDailyRewards
        );
        dailyRewardsButton.Margin = new Thickness(0, 0, 0, 5);

        // Кнопка токенов антагониста
        var antagTokensButton = CreateActionButton(
            "Терминал",
            "/Textures/_Mini/Interface/Coin.png", // Путь к вашей иконке (можно null)
            OpenAntagTokens
        );

        actionButtonsContainer.AddChild(dailyRewardsButton);
        actionButtonsContainer.AddChild(antagTokensButton);

        AddChild(actionButtonsContainer);

        var scrollContainer = new ScrollContainer
        {
            HScrollEnabled = false,
            VScrollEnabled = true,
            MinHeight = 80,
            MaxHeight = 330,
            HorizontalExpand = false,
            VerticalExpand = true
        };

        var serverContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };

        scrollContainer.AddChild(serverContainer);
        AddChild(scrollContainer);

        // AddServers(serverContainer);
    }

    /// <summary>
    /// Создаёт кнопку с иконкой и текстом
    /// </summary>
    /// <param name="text">Текст кнопки</param>
    /// <param name="iconPath">Путь к иконке (может быть null)</param>
    /// <param name="onPressed">Действие при нажатии</param>
    /// <returns>Готовая кнопка</returns>
    private Button CreateActionButton(string text, string? iconPath, Action onPressed)
    {
        var button = new Button
        {
            HorizontalExpand = true,
            MinHeight = 40
        };

        // Создаём контейнер для содержимого кнопки
        var contentContainer = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center
        };

        // Добавляем иконку, если указан путь
        if (iconPath != null)
        {
            try
            {
                var texture = _resourceCache.GetResource<TextureResource>(iconPath);
                var icon = new TextureRect
                {
                    Texture = texture,
                    TextureScale = new Vector2(0.4f, 0.4f), // Масштаб иконки
                    Margin = new Thickness(0, 0, 5, 0) // Отступ справа от иконки
                };
                contentContainer.AddChild(icon);
            }
            catch
            {
                // Если текстура не найдена, просто пропускаем добавление иконки
                // Можно добавить логгирование ошибки при необходимости
            }
        }

        // Добавляем текст
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HAlignment.Center
        };
        contentContainer.AddChild(label);

        button.AddChild(contentContainer);
        button.OnPressed += _ => onPressed();

        return button;
    }

    // private void AddServers(BoxContainer container)
    // {
    //     AddServerInfo(container, "МИНИ-СТАНЦИЯ:ОАЗИС", "ss14://ministation.qeqk.ru:1215", "Вайтлист с высоким уровнем отыгрыша", null);
    // }

    private void AddServerInfo(BoxContainer container, string serverName, string serverUrl, string description, string? discord)
    {
        var serverBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            MinHeight = 20,
            Margin = new Thickness(0, 0, 0, 5)
        };

        var nameAndDescriptionBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
        };

        var serverNameLabel = new Label
        {
            Text = serverName,
            MinWidth = 150
        };

        var descriptionLabel = new RichTextLabel
        {
            MaxWidth = 500
        };
        descriptionLabel.SetMessage(FormattedMessage.FromMarkup(description));

        var buttonBox = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            HorizontalExpand = true,
            HorizontalAlignment = HAlignment.Right
        };

        var connectButton = new Button
        {
            Text = "Зайти"
        };

        if (discord != null)
        {
            var discordButton = new Button
            {
                Text = "Discord"
            };

            discordButton.OnPressed += _ =>
            {
                _uriOpener.OpenUri(discord);
            };

            buttonBox.AddChild(discordButton);
        }

        _connectButtons.Add(connectButton);

        connectButton.OnPressed += _ =>
        {
            _gameController.Redial(serverUrl, "Connecting to another server...");

            foreach (var button in _connectButtons)
            {
                button.Disabled = true;
            }
        };

        buttonBox.AddChild(connectButton);

        nameAndDescriptionBox.AddChild(serverNameLabel);
        nameAndDescriptionBox.AddChild(descriptionLabel);

        serverBox.AddChild(nameAndDescriptionBox);
        serverBox.AddChild(buttonBox);

        container.AddChild(serverBox);
    }
}
