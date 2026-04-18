using System.Threading.Tasks;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server._Mini.AntagTokens;
using Content.Shared._White;
using Content.Shared.Database;
using Content.Shared.Ghost;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server._White.Ghost;

public sealed class GhostReturnToRoundSystem : EntitySystem
{
    private const int RespawnCost = 1;
    private const int RequiredPlayerCount = 20;

    [Dependency] private readonly IChatManager _chatManager = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly AntagTokenSystem _antagTokens = default!;

    public override void Initialize()
    {
        SubscribeNetworkEvent<GhostReturnToRoundRequest>(OnGhostReturnToRoundRequest);
    }

    private void OnGhostReturnToRoundRequest(GhostReturnToRoundRequest msg, EntitySessionEventArgs args)
    {
        var uid = args.SenderSession.AttachedEntity;

        if (uid == null)
            return;

        var connectedClient = args.SenderSession.Channel;
        var userId = args.SenderSession.UserId;

        TryGhostReturnToRound(uid.Value, connectedClient, userId, out var message, out var wrappedMessage);

        _chatManager.ChatMessageToOne(
            Shared.Chat.ChatChannel.Server,
            message,
            wrappedMessage,
            default,
            false,
            connectedClient,
            Color.Red);
    }

    private void TryGhostReturnToRound(EntityUid uid, INetChannel connectedClient, NetUserId userId, out string message, out string wrappedMessage)
    {
        if (_playerManager.PlayerCount <= RequiredPlayerCount)
        {
            message = Loc.GetString("ghost-respawn-max-players", ("players", RequiredPlayerCount));
            wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            return;
        }

        var deathTime = EnsureComp<GhostComponent>(uid).TimeOfDeath;
        var timeUntilRespawn = _cfg.GetCVar(WhiteCVars.GhostRespawnTime);
        var timePast = (_gameTiming.CurTime - deathTime).TotalMinutes;
        if (timePast >= timeUntilRespawn)
        {
            _ = Task.Run(async () =>
            {
                var spendResult = await _antagTokens.TrySpendBalance(userId, RespawnCost);
                if (!spendResult.success)
                {
                    var errorMessage = Loc.GetString("ghost-respawn-not-enough-currency", ("amount", RespawnCost));
                    var errorWrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", errorMessage));
                    _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, errorMessage, errorWrapped, default, false, connectedClient, Color.Red);
                    return;
                }

                var ticker = Get<GameTicker>();
                _playerManager.TryGetSessionById(userId, out var targetPlayer);

                if (targetPlayer != null)
                    ticker.Respawn(targetPlayer);

                _adminLogger.Add(LogType.Mind, LogImpact.Medium, $"{Loc.GetString("ghost-respawn-log-return-to-lobby", ("userName", connectedClient.UserName))}");

                var successMessage = Loc.GetString("ghost-respawn-window-rules-footer");
                var successWrapped = Loc.GetString("chat-manager-server-wrap-message", ("message", successMessage));
                _chatManager.ChatMessageToOne(Shared.Chat.ChatChannel.Server, successMessage, successWrapped, default, false, connectedClient, Color.White);
            });
            
            message = "Processing respawn request...";
            wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
            return;
        }

        message = Loc.GetString("ghost-respawn-time-left", ("time", (int) (timeUntilRespawn - timePast)));
        wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", message));
    }
}
