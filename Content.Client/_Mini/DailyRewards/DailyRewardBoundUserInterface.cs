// SPDX-FileCopyrightText: 2026 Casha
// Мини-станция/Freaky-station, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/ministation/mini-station-goob/master/LICENSE.TXT
using System;
using Content.Shared._Mini.DailyRewards;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Mini.DailyRewards;

[UsedImplicitly]
public sealed class DailyRewardBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private DailyRewardWindow? _window;

    public DailyRewardBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<DailyRewardWindow>();
        _window.Title = "Daily Rewards";
        _window.OnClaimPressed += OnClaimPressed;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not DailyRewardUpdateMessage msg || _window == null)
            return;

        _window.UpdateState(msg);
    }

    private void OnClaimPressed()
    {
        SendMessage(new DailyRewardClaimMessage());
    }
}
