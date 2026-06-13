using MossLib.Base;

namespace Quantum.Lang;

public class EnLangGenerator : ModLangGenBase
{
    protected override string LanguageCode => "EN";

    protected override void BuildLocaleData()
    {
        Add("hover.info.usable.true", "Can be used directly");
        Add("hover.info.usable.false", "Cannot be used directly");
        Add("hover.info.usable_on_limb.true", "Can be used on limbs");
        Add("hover.info.usable_on_limb.false", "Cannot be used on limbs");
        Add("hover.info.auto_attack", "Continuous use when long press");
        Add("hover.info.usable_with_lrb", "Can only be used with left click");
        Add("hover.info.ignore_depression", "Ignore depression status");
        Add("hover.info.recipe", "Recipes: ");

        Add("key.shift_to_expand.down", "Release Shift to Fold");
        Add("key.ctrl_to_expand.up", "Hold Ctrl to expand more information");
        Add("key.ctrl_to_expand.down", "Release Ctrl to Fold More Info");

        // Config - Info
        Add("config.info.ammunition_ui.name", "Ammunition UI");
        Add("config.info.ammunition_ui.description", "Display your ammunition in real time!");
        Add("config.info.ctrl_to_expand.name", "Ctrl for more information");
        Add("config.info.ctrl_to_expand.description", "Press Ctrl to show more information");

        // Config - Item - Gun
        Add("config.item.gun.auto_rack.name", "Auto Rack");
        Add("config.item.gun.auto_rack.description", "If true, guns will automatically rack and stay racked when ammo is available");
        Add("config.item.gun.indestructible_gun.name", "Indestructible Gun");
        Add("config.item.gun.indestructible_gun.description", "If true, guns will not be destroyed");
        Add("config.item.gun.infinite_ammunition.name", "Infinite Ammunition");
        Add("config.item.gun.infinite_ammunition.description", "∞ INFINITE AMMUNITION ∞");
        Add("config.item.gun.never_jam.name", "Never Jam");
        Add("config.item.gun.never_jam.description", "If true, guns will never jam");
        Add("config.item.gun.no_casing.name", "No Shell Case");
        Add("config.item.gun.no_casing.description", "If true, guns will not eject the cartridge casing");
        Add("config.item.gun.recoilless.name", "Recoilless");
        Add("config.item.gun.recoilless.description", "If true, guns will not have recoil");
    }
}