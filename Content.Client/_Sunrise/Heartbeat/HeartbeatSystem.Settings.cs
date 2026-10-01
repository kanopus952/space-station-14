using Content.Shared._Sunrise.Heartbeat;
using Content.Shared._Sunrise.SunriseCCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Client._Sunrise.Heartbeat;

public sealed partial class HeartbeatSystem : EntitySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private INetManager _netManager = default!;

    private bool _playHeartBeatSound;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(SunriseCCVars.PlayHeartBeatSound, OnOptionsChanged, true);

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(SunriseCCVars.PlayHeartBeatSound, OnOptionsChanged);
    }

    private void OnOptionsChanged(bool option)
    {
        _playHeartBeatSound = option;
        if (_netManager.IsConnected)
            RaiseNetworkEvent(new HeartbeatOptionsChangedEvent(_playHeartBeatSound));
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        RaiseNetworkEvent(new HeartbeatOptionsChangedEvent(_playHeartBeatSound));
    }
}
