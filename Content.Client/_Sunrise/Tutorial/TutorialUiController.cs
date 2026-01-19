using Content.Client.Lobby;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Player;

namespace Content.Client._Sunrise.Tutorial;

public sealed class TutorialUIController : UIController, IOnStateEntered<LobbyState>, IOnStateExited<LobbyState>
{
    private TutorialWindow? _window;
    private bool _shown;

    public void OnStateEntered(LobbyState state)
    {
        if (_shown || _window != null)
            return;

        ToggleTutorial();
    }
    public void ToggleTutorial()
    {
        if (_window != null)
        {
            _window.Close();
            return;
        }

        _shown = true;
        _window = new TutorialWindow();
        _window.OnClose += () => _window = null;

        _window.OpenCentered();
    }

    public void OnStateExited(LobbyState state)
    {
        if (_window != null)
        {
            _window.Close();
            _window = null;
        }

        _shown = false;
    }
}
