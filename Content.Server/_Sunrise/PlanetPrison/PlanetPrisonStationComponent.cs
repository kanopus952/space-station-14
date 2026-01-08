using Content.Server.Maps;
using Content.Shared.Maps;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Whitelist;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._Sunrise.PlanetPrison;


[RegisterComponent]
public sealed partial class PlanetPrisonStationComponent : Component
{
    public MapId MapId = MapId.Nullspace;

    [DataField]
    public EntityUid Entity = EntityUid.Invalid;

    [DataField(required: true)]
    public List<ProtoId<BiomeTemplatePrototype>> Biomes = [];

    [DataField]
    public EntityWhitelist? ShuttleWhitelist;

    [DataField]
    public EntityUid PrisonGrid = EntityUid.Invalid;
}
