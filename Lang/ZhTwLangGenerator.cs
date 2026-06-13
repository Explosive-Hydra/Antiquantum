using MossLib.Base;

namespace Quantum.Lang;

public class ZhTwLangGenerator : ModLangGenBase
{
    protected override string LanguageCode => "zh-TW";

    protected override void BuildLocaleData()
    {
        Add("hover.info.usable.true", "可直接使用");
        Add("hover.info.usable.false", "不可直接使用");
        Add("hover.info.usable_on_limb.true", "可對肢體使用");
        Add("hover.info.usable_on_limb.false", "不可對肢體使用");
        Add("hover.info.auto_attack", "長按時持續使用");
        Add("hover.info.usable_with_lrb", "只能左鍵使用");
        Add("hover.info.ignore_depression", "無視抑鬱狀態");
        Add("hover.info.recipe", "合成配方: ");

        Add("key.shift_to_expand.down", "松開Shift摺疊");
        Add("key.ctrl_to_expand.up", "按住Ctrl展開更多資訊");
        Add("key.ctrl_to_expand.down", "松開Ctrl摺疊更多資訊");

        // Config - Info
        Add("config.info.ammunition_ui.name", "彈藥UI");
        Add("config.info.ammunition_ui.description", "在原槍械菜单的上方顯示槍械剩余弹量和最大弹量");
        Add("config.info.ctrl_to_expand.name", "Ctrl 更多信息");
        Add("config.info.ctrl_to_expand.description", "按下Ctrl才顯示更多資訊");

        // Config - Item - Gun
        Add("config.item.gun.auto_rack.name", "自動上膛");
        Add("config.item.gun.auto_rack.description", "開啟後，當有彈藥時，槍械將自動拉栓並保持拉栓狀態");
        Add("config.item.gun.indestructible_gun.name", "不毁槍械");
        Add("config.item.gun.indestructible_gun.description", "開啟後，槍械將不會損壞");
        Add("config.item.gun.infinite_ammunition.name", "無限彈藥");
        Add("config.item.gun.infinite_ammunition.description", "∞ 無限彈藥 ∞");
        Add("config.item.gun.never_jam.name", "永不卡殼");
        Add("config.item.gun.never_jam.description", "開啟後，槍械將不會卡殼");
        Add("config.item.gun.no_casing.name", "無彈殼");
        Add("config.item.gun.no_casing.description", "開啟後，槍械將不會彈出彈殼");
        Add("config.item.gun.recoilless.name", "無後座力");
        Add("config.item.gun.recoilless.description", "開啟後，槍械將沒有後座力");
    }
}