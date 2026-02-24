using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client.Eui;
using Content.Client._Sunrise.FancyCardControl;
using Content.Shared._Sunrise.Tutorial.Eui;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._Sunrise.Tutorial;

[UsedImplicitly]
public sealed class TutorialCompletionEui : BaseEui
{
    private readonly TutorialCompletionWindow _window;

    public TutorialCompletionEui()
    {
        _window = new TutorialCompletionWindow();
        _window.ActionPressed += id => SendMessage(new TutorialCompletionEuiActionMessage(id));
        _window.OnClose += () => SendMessage(new CloseEuiMessage());
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _window.Close();
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not TutorialCompletionEuiState cast)
            return;

        _window.ApplyState(cast);
    }
}
