using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.MagicMirror;

/// <summary>
/// Allows humanoids to change their appearance mid-round.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(MagicMirrorSystem))]
public sealed partial class MagicMirrorComponent : Component
{
    /// <summary>
    /// The id for a doAfter our <see cref="Target"/> is doing. Stored as an ushort so it can be networked and one day predicted.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ushort? DoAfter;

    /// <summary>
    /// Magic mirror target, used for validating UI messages.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Target;

    [DataField(required: true)]
    public HashSet<ProtoId<OrganCategoryPrototype>> Organs = new();

    [DataField(required: true)]
    public HashSet<HumanoidVisualLayers> Layers = new();

    /// <summary>
    /// Do after time to add a new slot, adding hair to a person.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan AddSlotTime = TimeSpan.FromSeconds(10); // Sunrise-edit

    /// <summary>
    /// Do after time to modify an entity's markings.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ModifyTime = TimeSpan.FromSeconds(7);

    /// <summary>
    /// Do after time to remove a marking slot.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RemoveSlotTime = TimeSpan.FromSeconds(8); // Sunrise-edit

    /// <summary>
    /// Do after time to change a person's hairstyle.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan SelectSlotTime = TimeSpan.FromSeconds(6); // Sunrise-edit

    /// <summary>
    /// Do after time to change a person's hair color.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ChangeSlotTime = TimeSpan.FromSeconds(4); // Sunrise-edit

    /// <summary>
    /// Sound emitted when slots are changed.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? ChangeHairSound = new SoundPathSpecifier("/Audio/Items/scissors.ogg");
}
