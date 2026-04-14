using Content.Shared.Borgs;
using Content.Shared.Silicons.Borgs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.Borgs
{
    public sealed class MimicrySystem : EntitySystem
    {
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<MimicryComponent, AppearanceChangeEvent>(OnAppearanceChange);
        }
        private void OnAppearanceChange(EntityUid uid, MimicryComponent component, ref AppearanceChangeEvent args)
        {
            if (args.Sprite == null)
                return;

            if (_appearance.TryGetData<bool>(uid, MiniBorgVisuals.Eng, out var eng, args.Component) && eng)
            {
                if (TryApplyDisguiseChassisAppearance(uid, component, args))
                    return;

                args.Sprite.LayerSetState(BorgVisualLayers.Body, component.EngState, component.EngSpritePath);
                args.Sprite.LayerSetState(BorgVisualLayers.Light, component.EngState1, component.EngSpritePath);
                args.Sprite.LayerSetState("light", component.EngState2, component.EngSpritePath);
            }
            else if (_appearance.TryGetData<bool>(uid, MiniBorgVisuals.Real, out var real, args.Component) && real)
            {
                if (TryApplyCurrentChassisAppearance(uid, args))
                    return;

                args.Sprite.LayerSetState(BorgVisualLayers.Body, component.RealState);
                args.Sprite.LayerSetState(BorgVisualLayers.Light, component.RealState1);
                args.Sprite.LayerSetState("light", component.RealState2);
            }
        }

        private bool TryApplyCurrentChassisAppearance(EntityUid uid, AppearanceChangeEvent args)
        {
            if (!TryComp(uid, out BorgSwitchableTypeComponent? switchableType) ||
                switchableType.SelectedBorgType == null ||
                !TryComp(uid, out BorgSwitchableSubtypeComponent? switchableSubtype) ||
                switchableSubtype.BorgSubtype == null ||
                !_prototype.TryIndex(switchableType.SelectedBorgType.Value, out BorgTypePrototype? prototype) ||
                !_prototype.TryIndex(switchableSubtype.BorgSubtype.Value, out BorgSubtypePrototype? subtype))
            {
                return false;
            }

            var hasPlayer = _appearance.TryGetData<bool>(uid, BorgVisuals.HasPlayer, out var hasMind, args.Component) && hasMind;
            var mindState = hasPlayer ? prototype.SpriteHasMindState : prototype.SpriteNoMindState;
            var spritePath = subtype.SpritePath.ToString();

            args.Sprite!.LayerSetState(BorgVisualLayers.Body, prototype.SpriteBodyState, spritePath);
            args.Sprite.LayerSetState(BorgVisualLayers.Light, mindState, spritePath);
            args.Sprite.LayerSetState("light", prototype.SpriteToggleLightState, spritePath);
            return true;
        }

        private bool TryApplyDisguiseChassisAppearance(EntityUid uid, MimicryComponent component, AppearanceChangeEvent args)
        {
            if (!TryComp(uid, out BorgSwitchableTypeComponent? switchableType) ||
                switchableType.SelectedBorgType == null ||
                !component.DisguiseTypeMap.TryGetValue(switchableType.SelectedBorgType.Value, out var disguiseTypeId) ||
                !_prototype.TryIndex(disguiseTypeId, out BorgTypePrototype? disguiseType))
            {
                return false;
            }

            var hasPlayer = _appearance.TryGetData<bool>(uid, BorgVisuals.HasPlayer, out var hasMind, args.Component) && hasMind;
            var mindState = hasPlayer ? disguiseType.SpriteHasMindState : disguiseType.SpriteNoMindState;

            args.Sprite!.LayerSetState(BorgVisualLayers.Body, disguiseType.SpriteBodyState, component.EngSpritePath);
            args.Sprite.LayerSetState(BorgVisualLayers.Light, mindState, component.EngSpritePath);
            args.Sprite.LayerSetState("light", disguiseType.SpriteToggleLightState, component.EngSpritePath);
            return true;
        }
    }
}
