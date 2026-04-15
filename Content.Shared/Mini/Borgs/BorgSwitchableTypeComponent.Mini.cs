using Content.Shared.Silicons.Borgs.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Borgs.Components;

public sealed partial class BorgSwitchableTypeComponent
{
    [DataField]
    public ProtoId<BorgTypePrototype>[] AllowedBorgTypes = [];
}
