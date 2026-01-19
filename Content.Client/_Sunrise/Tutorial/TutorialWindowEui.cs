using Content.Client._Sunrise.Tutorial;
using Content.Client.Eui;
using Content.Client.GameTicking.Managers;
using Content.Shared._Sunrise.Tutorial.Eui;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Content.Shared.Administration;
using Content.Shared.Eui;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._Sunrise.Tutorial;

public sealed class TutorialWindowEui : BaseEui
{
    [Dependency] private readonly TutorialUIController _tutorialUIController = default!;
    private readonly TutorialWindow _window;

    public TutorialWindowEui()
    {
        IoCManager.InjectDependencies(this);
        _window = new TutorialWindow();

        _window.OnTutorialButtonPressed += OnPressed;
        _tutorialUIController.OnTutorialQuit += OnTutorialQuit;
    }

    private void OnTutorialQuit()
    {
        SendMessage(new TutorialQuitButtonPressedMessage());
    }

    private void OnPressed(TutorialSequencePrototype proto)
    {
        SendMessage(new TutorialButtonPressedEuiMessage(proto.PlayerEntity, proto.Grid));
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
        if (state is not TutorialWindowEuiState tutorialState)
            return;

        _window.SetCompletedTutorials(tutorialState.CompletedTutorials);
    }
}
