using System.Text;
using Content.Client.Lobby;
using Content.Shared._Sunrise.Roadmap;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client._Sunrise.Roadmap;

public sealed class RoadmapUIController : UIController, IOnStateEntered<LobbyState>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IEntityManager _entMan = default!;

    private Roadmap? _window;

    public void OnStateEntered(LobbyState state)
    {
        _entMan.System<RoadmapSystem>().RequestLikes();

        if (_window != null)
            return;

        var roadmapId = _cfg.GetCVar(SunriseCCVars.RoadmapId);
        if (!_prototype.TryIndex<RoadmapVersionsPrototype>(roadmapId, out var roadmapVersions))
            return;

        var currentHash = ComputeHash(roadmapVersions);
        var lastSeenHash = _cfg.GetCVar(SunriseCCVars.RoadmapLastSeenHash);
        if (lastSeenHash == currentHash)
            return;

        OpenRoadmap();
        _cfg.SetCVar(SunriseCCVars.RoadmapLastSeenHash, currentHash);
        _cfg.SaveToFile();
    }

    public void ToggleRoadmap()
    {
        if (_window != null)
        {
            _window.Close();
            return;
        }

        OpenRoadmap();
    }

    private void OpenRoadmap()
    {
        _window = new Roadmap();
        _window.OnClose += () => _window = null;
        _window.OpenCentered();
    }

    private static string ComputeHash(RoadmapVersionsPrototype proto)
    {
        var sb = new StringBuilder();
        sb.Append(proto.ID);
        sb.Append(proto.Fork);
        foreach (var group in proto.Versions)
        {
            sb.Append(group.Name);
            foreach (var goal in group.Goals)
            {
                sb.Append(goal.Id);
                sb.Append(goal.Name);
                sb.Append(goal.Desc);
                sb.Append((int)goal.State);
            }
        }

        // FNV-1a 64-bit
        unchecked
        {
            var hash = 14695981039346656037UL;
            foreach (var c in sb.ToString())
            {
                hash ^= c;
                hash *= 1099511628211UL;
            }
            return hash.ToString("X16");
        }
    }
}
