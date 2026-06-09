using MossLib.Base;

namespace Quantum.Lang;

public class ZhCnLangGenerator : ModLangGenBase
{
    protected override string LanguageCode => "zh-CN";

    protected override void BuildLocaleData()
    {
        Add("hover.info.usable.true", "可直接使用");
        Add("hover.info.usable.false", "不可直接使用");
        Add("hover.info.usable_on_limb.true", "可对肢体使用");
        Add("hover.info.usable_on_limb.false", "不可对肢体使用");
        Add("hover.info.auto_attack", "长按时持续使用");
        Add("hover.info.usable_with_lrb", "只能左键使用");
        Add("hover.info.ignore_depression", "无视抑郁状态");
        Add("hover.info.recipe", "合成配方: ");

        Add("key.shift_to_expand.down", "松开Shift折叠");
        Add("key.ctrl_to_expand.up", "按住Ctrl展开更多信息");
        Add("key.ctrl_to_expand.down", "松开Ctrl折叠更多信息");

        // Config - Info
        Add("config.info.ammunition_ui.name", "弹药UI");
        Add("config.info.ammunition_ui.description", "在原枪械菜单的上方显示枪械剩余弹量和最大弹量");
        Add("config.info.ctrl_to_expand.name", "Ctrl 更多信息");
        Add("config.info.ctrl_to_expand.description", "按下Ctrl才显示更多信息");
        
        // Item - Gun
        Add("config.item.gun.auto_rock.name", "自动上膛");
        Add("config.item.gun.auto_rock.description", "开启后，当有弹药时，枪械将自动拉栓并保持拉栓状态");
        Add("config.item.gun.indestructible_gun.name", "不毁枪械");
        Add("config.item.gun.indestructible_gun.description", "开启后，枪械将不会损坏");
        Add("config.item.gun.infinite_ammunition.name", "无限弹药");
        Add("config.item.gun.infinite_ammunition.description", "∞ 无限子弹 ∞");
        Add("config.item.gun.never_jam.name", "永不卡壳");
        Add("config.item.gun.never_jam.description", "开启后，枪械将不会卡壳");
        Add("config.item.gun.recoilless.name", "无后座力");
        Add("config.item.gun.recoilless.description", "开启后，枪械将没有后坐力");
    }
}