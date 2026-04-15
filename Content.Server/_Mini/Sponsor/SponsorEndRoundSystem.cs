using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
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
    private static readonly TimeSpan RetryStep = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan RetryMax = TimeSpan.FromMinutes(10);
    private TimeSpan _retryDelay = TimeSpan.Zero;
    private TimeSpan _nextRetryAt = TimeSpan.Zero;
    private int _loadInProgress;
    private bool _dbUnavailable;

    public record struct SponsorInfo(string Uid, int Level);
    public ImmutableList<SponsorInfo> Sponsors { get; private set; } = ImmutableList<SponsorInfo>.Empty;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundEndMessageEvent>(OnRoundEnd);

        // Первичная загрузка при старте
        TryScheduleLoad("первичной загрузки", logSuccess: false);
    }

    private void OnRoundEnd(RoundEndMessageEvent ev)
    {
        // Внеочередное обновление после раунда
        TryScheduleLoad("обновления после раунда", logSuccess: true);
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
        TryScheduleLoad("периодического обновления", logSuccess: true);
    }

    private void TryScheduleLoad(string source, bool logSuccess)
    {
        if (_timing.CurTime < _nextRetryAt)
            return;

        if (Interlocked.CompareExchange(ref _loadInProgress, 1, 0) != 0)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                var loaded = await LoadSponsors();
                if (loaded && logSuccess)
                    Log.Debug($"[Sponsors] Данные обновлены после {source}.");
            }
            finally
            {
                Interlocked.Exchange(ref _loadInProgress, 0);
            }
        });
    }

    public async Task<bool> LoadSponsors()
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
            if (_dbUnavailable)
                Log.Info("[Sponsors] Подключение к БД восстановлено.");

            _dbUnavailable = false;
            _retryDelay = TimeSpan.Zero;
            _nextRetryAt = TimeSpan.Zero;
            Log.Info($"[Sponsors] Загружено {Sponsors.Count} спонсоров из БД.");
            return true;
        }
        catch (Exception ex)
        {
            _dbUnavailable = true;
            _retryDelay = _retryDelay == TimeSpan.Zero ? RetryStep : TimeSpan.FromSeconds(Math.Min(RetryMax.TotalSeconds, _retryDelay.TotalSeconds + RetryStep.TotalSeconds));
            _nextRetryAt = _timing.CurTime + _retryDelay;

            if (_retryDelay == RetryStep)
                Log.Warning($"[Sponsors] Критическая ошибка БД: {ex}");
            else
                Log.Warning($"[Sponsors] БД недоступна, следующая попытка через {_retryDelay.TotalSeconds:0} сек.");

            return false;
        }
    }
}
