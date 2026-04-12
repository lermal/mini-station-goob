using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Content.Shared.GameTicking;
using Npgsql;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Timing;

namespace Content.Server.Sponsors;

public sealed class SponsorSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private float _updateTimer;
    private const float UpdateInterval = 5f;

    public record struct SponsorInfo(string Uid, int Level);
    public ImmutableList<SponsorInfo> Sponsors { get; private set; } = ImmutableList<SponsorInfo>.Empty;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);

        // Первичная загрузка при старте
        _ = LoadSponsors();
    }

    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        // Внеочередное обновление после раунда
        _ = Task.Run(async () =>
        {
            try
            {
                await LoadSponsors();
                Log.Info("[Sponsors] Данные обновлены после раунда.");
            }
            catch (Exception e)
            {
                Log.Error($"[Sponsors] Ошибка обновления после раунда: {e}");
            }
        });
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Пропускаем обновление, если игра приостановлена
        if (!_timing.IsFirstTimePredicted)
            return;

        _updateTimer += frameTime;

        if (_updateTimer < UpdateInterval)
            return;

        _updateTimer -= UpdateInterval;

        // Запускаем асинхронную загрузку, не блокируя игровой цикл
        _ = Task.Run(async () =>
        {
            try
            {
                await LoadSponsors();
                Log.Debug("[Sponsors] Периодическое обновление завершено.");
            }
            catch (Exception e)
            {
                Log.Error($"[Sponsors] Ошибка периодического обновления: {e}");
            }
        });
    }

    public async Task LoadSponsors()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = _cfg.GetCVar<string>("database.pg_host"),
            Port = _cfg.GetCVar<int>("database.pg_port"),
            Database = _cfg.GetCVar<string>("database.pg_database"),
            Username = _cfg.GetCVar<string>("database.pg_username"),
            Password = _cfg.GetCVar<string>("database.pg_password")
        };

        try
        {
            await using var dataSource = NpgsqlDataSource.Create(builder.ConnectionString);

            await using var cmd = dataSource.CreateCommand(@"
                SELECT DISTINCT da.user_id, ds.sponsor_level
                FROM discord_sponsor ds
                JOIN discord_auth da ON ds.discord_id = da.discord_id");

            await using var reader = await cmd.ExecuteReaderAsync();
            var tempList = new List<SponsorInfo>();

            while (await reader.ReadAsync())
            {
                tempList.Add(new SponsorInfo(
                    reader.GetGuid(0).ToString(),
                    reader.GetInt32(1)
                ));
            }

            Sponsors = tempList.ToImmutableList();
            Log.Info($"[Sponsors] Загружено {Sponsors.Count} спонсоров из БД.");
        }
        catch (Exception ex)
        {
            Log.Error($"[Sponsors] Критическая ошибка БД: {ex}");
        }
    }
}
