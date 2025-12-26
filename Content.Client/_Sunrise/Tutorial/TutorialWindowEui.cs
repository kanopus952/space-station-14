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
    [Dependency] private readonly IEntitySystemManager _entSys = default!;
    private readonly TutorialWindow _window;
    private readonly ClientGameTicker _gameTicker;

    public TutorialWindowEui()
    {
        IoCManager.InjectDependencies(this);
        _gameTicker = _entSys.GetEntitySystem<ClientGameTicker>();
        _window = new TutorialWindow();
        _window.OnTutorialButtonPressed += OnPressed;
    }

    private void OnPressed(TutorialSequencePrototype proto)
    {
        if (!_gameTicker.IsGameStarted)


        _window.Close();

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
}
