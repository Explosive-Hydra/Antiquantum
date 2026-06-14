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

        // Config - Misc
        Add("config.misc.no_observer.name", "No Observer");
        Add("config.misc.no_observer.description", "If true, the world will not have observers");

        // Config - UI
        Add("config.ui.ammunition_ui.name", "Ammunition UI");
        Add("config.ui.ammunition_ui.description", "Display your ammunition in real time!");
        Add("config.ui.sort_key.name", "Sort Key");
        Add("config.ui.sort_key.description", "Press to sort container items");
        Add("config.ui.max_visible_candidates.name", "Max Candidates");
        Add("config.ui.max_visible_candidates.description", "Maximum number of candidate lines displayed in console autocomplete");
        Add("config.ui.max_history_size.name", "History Size");
        Add("config.ui.max_history_size.description", "Maximum number of executed commands kept in console history");

        // UI - Sort
        Add("ui.sort.mode.name", "Name");
        Add("ui.sort.mode.value", "Value");
        Add("ui.sort.mode.weight", "Weight");
        Add("ui.sort.ascending", "↑ Ascending");
        Add("ui.sort.descending", "↓ Descending");
        Add("ui.sort.mode_tip", "Sort Mode");
        Add("ui.sort.mode_desc", "Sort by {0}");
        Add("ui.sort.order_tip", "Sort Order");
        Add("ui.sort.completed", "Sorted: {0} {1}");
        Add("ui.sort.no_change", "Already sorted");
        Add("ui.sort.execute_tip", "Sort");
        Add("ui.sort.execute_desc", "Sort container items");

        // Pinyin Search
        Add("pinyin.library.not_found", "TinyPinyin library not loaded — pinyin search disabled");
        Add("pinyin.api_not_found", "TinyPinyin API methods not found — pinyin search disabled");
        Add("pinyin.init_failed", "TinyPinyin init failed: {0} — pinyin search disabled");
    }
}