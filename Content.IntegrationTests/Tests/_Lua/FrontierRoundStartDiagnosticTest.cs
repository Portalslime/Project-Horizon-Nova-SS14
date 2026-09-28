#nullable enable
using Content.IntegrationTests.Pair;
using Content.Server.GameTicking;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Pool;
using System.Collections.Generic;

namespace Content.IntegrationTests.Tests._Lua;

[TestFixture]
public sealed class FrontierRoundStartDiagnosticTest
{
    private static readonly ProtoId<JobPrototype> Pilot = "Pilot";

    [Test]
    public async Task StartRoundOnFrontierAsPilot()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            DummyTicker = false,
            Connected = true,
            InLobby = true
        });

        var cfg = pair.Server.CfgMan;
        cfg.SetCVar(CCVars.GameMap, "Frontier");

        var ticker = pair.Server.System<GameTicker>();
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));

        await pair.SetJobPriorities((Pilot, JobPriority.High));

        ticker.ToggleReadyAll(true);
        await pair.Server.WaitPost(() => ticker.StartRound());
        await pair.RunTicksSync(60);

        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.InRound));

        var uid = pair.Server.PlayerMan.SessionsDict.GetValueOrDefault(pair.Client.User!.Value)?.AttachedEntity;
        Assert.That(pair.Server.EntMan.EntityExists(uid), "Player did not get an entity spawned into the round.");

        await pair.CleanReturnAsync();
    }
}
