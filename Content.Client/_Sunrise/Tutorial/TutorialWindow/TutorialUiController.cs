using Content.Client.Lobby;
using Content.Shared._Sunrise.SunriseCCVars;
using Content.Shared._Sunrise.Tutorial.Prototypes;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Tutorial.TutorialWindow;

public sealed class TutorialUIController : UIController, IOnStateEntered<LobbyState>, IOnStateExited<LobbyState>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [UISystemDependency] private readonly TutorialSystem _tutorialSystem = default!;
    private TutorialWindow? _window;
    private Action<TutorialSequencePrototype>? _startTutorialHandler;
    private bool _shown;
    private bool _autoOpenEnabled = true;
    private bool _windowDataSubscribed;

    public void ToggleTutorial()
    {
        if (_window != null)
        {
            _window.Close();
            return;
        }

        _shown = true;
        _window = new TutorialWindow();

        if (_tutorialSystem.CompletedTutorialsReceived)
            _window.SetCompletedTutorials(_tutorialSystem.CompletedTutorials);

        if (!_windowDataSubscribed)
        {
            _tutorialSystem.WindowDataReceived += OnWindowDataReceived;
            _windowDataSubscribed = true;
        }

        _startTutorialHandler = proto =>
            _tutorialSystem.RequestStartTutorial(new ProtoId<TutorialSequencePrototype>(proto.ID));

        _window.OnTutorialButtonPressed += _startTutorialHandler;
        _window.OnRequestCompletedTutorials += _tutorialSystem.RequestWindowData;

        _window.OnClose += () =>
        {
            _window.OnTutorialButtonPressed -= _startTutorialHandler;
            _window.OnRequestCompletedTutorials -= _tutorialSystem.RequestWindowData;

            _startTutorialHandler = null;
            _window = null;
        };

        _window.OpenCentered();
    }

    private void TryOpenTutorial()
    {
        if (_shown || _window != null)
            return;

        ToggleTutorial();
    }
    public void OnStateEntered(LobbyState state)
    {
        _cfg.OnValueChanged(SunriseCCVars.TutorialWindowAutoOpen, OnAutoOpenChanged, true);

        if (!_autoOpenEnabled)
            return;

        if (!_windowDataSubscribed)
        {
            _tutorialSystem.WindowDataReceived += OnWindowDataReceived;
            _windowDataSubscribed = true;
        }

        if (!_tutorialSystem.CompletedTutorialsReceived)
            _tutorialSystem.RequestWindowData();

        TryOpenTutorial();
    }

    public void OnStateExited(LobbyState state)
    {
        _cfg.UnsubValueChanged(SunriseCCVars.TutorialWindowAutoOpen, OnAutoOpenChanged);

        _window?.Close();

        if (!_windowDataSubscribed)
            return;

        _tutorialSystem.WindowDataReceived -= OnWindowDataReceived;
        _windowDataSubscribed = false;
    }

    private void OnAutoOpenChanged(bool value)
    {
        _autoOpenEnabled = value;
    }

    private void OnWindowDataReceived()
    {
        if (_tutorialSystem.CompletedTutorialsReceived)
            _window?.SetCompletedTutorials(_tutorialSystem.CompletedTutorials);

        TryOpenTutorial();
    }
}
