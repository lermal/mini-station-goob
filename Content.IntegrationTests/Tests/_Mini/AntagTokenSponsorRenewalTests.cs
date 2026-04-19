// SPDX-FileCopyrightText: 2026 Mr_Samuel
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.IntegrationTests.Pair;
using NUnit.Framework;
using Content.Server._Mini.AntagTokens;
using Content.Server.Database;
using Robust.Server.Player;

namespace Content.IntegrationTests.Tests._Mini;

[TestFixture]
public sealed class AntagTokenSponsorRenewalTests
{
    [Test]
    public async Task DonorSponsorRenewal_GrantsWhenCooldownElapsed()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false,
        });
        pair.ServerLogHandler.IgnoredSawmills.Add("system.sponsor");

        var server = pair.Server;
        var playerMgr = server.ResolveDependency<IPlayerManager>();
        var userDb = server.ResolveDependency<UserDbDataManager>();

        await pair.RunTicksSync(30);

        await server.WaitAssertion(() =>
        {
            Assert.That(playerMgr.Sessions, Is.Not.Empty);
            var session = playerMgr.Sessions.First();
            Assert.That(userDb.IsLoadComplete(session), Is.True);
        });

        await server.WaitPost(() =>
        {
            var session = playerMgr.Sessions.First();
            var antag = server.System<AntagTokenSystem>();
            antag.SetSponsorLevelOverride(session.UserId, 1);
            antag.TestSetLastDonorBonusClaimUtc(session.UserId, DateTime.UtcNow.AddDays(-40));
            var before = antag.GetBalance(session.UserId);
            antag.TryGrantSponsorRenewalAfterStateLoaded(session);
            Assert.That(antag.GetBalance(session.UserId), Is.EqualTo(before + 20));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DonorSponsorRenewal_SkipsWithinCooldown()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Connected = true,
            DummyTicker = false,
        });
        pair.ServerLogHandler.IgnoredSawmills.Add("system.sponsor");

        var server = pair.Server;
        var playerMgr = server.ResolveDependency<IPlayerManager>();
        var userDb = server.ResolveDependency<UserDbDataManager>();

        await pair.RunTicksSync(30);

        await server.WaitAssertion(() =>
        {
            Assert.That(playerMgr.Sessions, Is.Not.Empty);
            var session = playerMgr.Sessions.First();
            Assert.That(userDb.IsLoadComplete(session), Is.True);
        });

        await server.WaitPost(() =>
        {
            var session = playerMgr.Sessions.First();
            var antag = server.System<AntagTokenSystem>();
            antag.SetSponsorLevelOverride(session.UserId, 1);
            antag.TestSetLastDonorBonusClaimUtc(session.UserId, DateTime.UtcNow.AddDays(-5));
            var before = antag.GetBalance(session.UserId);
            antag.TryGrantSponsorRenewalAfterStateLoaded(session);
            Assert.That(antag.GetBalance(session.UserId), Is.EqualTo(before));
        });

        await pair.CleanReturnAsync();
    }
}
