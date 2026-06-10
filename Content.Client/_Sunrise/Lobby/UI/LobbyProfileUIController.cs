using Content.Client.Lobby;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Sunrise.Lobby.UI;

[UsedImplicitly]
public sealed class LobbyProfileUIController : UIController, IOnStateExited<LobbyState>
{
    private LobbyProfileWindow? _window;

    public void OpenWindow()
    {
        EnsureWindow();

        _window!.OpenCentered();
        _window.MoveToFront();
    }

    public void ToggleWindow()
    {
        EnsureWindow();

        if (_window!.IsOpen)
        {
            _window.Close();
            return;
        }

        OpenWindow();
    }

    public void OnStateExited(LobbyState state)
    {
        _window?.Close();
    }

    private void EnsureWindow()
    {
        if (_window is { Disposed: false })
            return;

        _window = UIManager.CreateWindow<LobbyProfileWindow>();
    }
}
