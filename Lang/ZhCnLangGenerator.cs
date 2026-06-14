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
        Add("config.info.ctrl_to_expand.name", "Ctrl 更多信息");
        Add("config.info.ctrl_to_expand.description", "按下Ctrl才显示更多信息");
        Add("config.info.favourited_item_durability_exhaustion_alert.name", "收藏品耐久警报阈值");
        Add("config.info.favourited_item_durability_exhaustion_alert.description", "当收藏的物品耐久度低于此比例时发出警报（0 = 关闭）");

        // Config - Item - Gun
        Add("config.item.gun.auto_rack.name", "自动上膛");
        Add("config.item.gun.auto_rack.description", "开启后，当有弹药时，枪械将自动拉栓并保持拉栓状态");
        Add("config.item.gun.indestructible_gun.name", "不毁枪械");
        Add("config.item.gun.indestructible_gun.description", "开启后，枪械将不会损坏");
        Add("config.item.gun.infinite_ammunition.name", "无限弹药");
        Add("config.item.gun.infinite_ammunition.description", "∞ 无限子弹 ∞");
        Add("config.item.gun.never_jam.name", "永不卡壳");
        Add("config.item.gun.never_jam.description", "开启后，枪械将不会卡壳");
        Add("config.item.gun.no_casing.name", "无弹壳");
        Add("config.item.gun.no_casing.description", "开启后，枪械将不会弹出弹壳");
        Add("config.item.gun.recoilless.name", "无后座力");
        Add("config.item.gun.recoilless.description", "开启后，枪械将没有后坐力");

        // Config - Misc
        Add("config.misc.no_observer.name", "无观察者");
        Add("config.misc.no_observer.description", "开启后，再无观察者");

        // Config - UI
        Add("config.ui.ammunition_ui.name", "弹药UI");
        Add("config.ui.ammunition_ui.description", "在原枪械菜单的上方显示枪械剩余弹量和最大弹量");
        Add("config.ui.bilingual_name.name", "双语名称");
        Add("config.ui.bilingual_name.description", "设定后会在物品原名旁附加指定语言的翻译（如 EN / zh-CN / zh-TW），留空则只显示原名");
        Add("config.ui.sort_key.name", "整理按键");
        Add("config.ui.sort_key.description", "按下整理容器物品");
        Add("config.ui.max_visible_candidates.name", "最大候选数");
        Add("config.ui.max_visible_candidates.description", "控制台参数候选列表最多显示的行数");
        Add("config.ui.max_history_size.name", "历史记录上限");
        Add("config.ui.max_history_size.description", "控制台历史命令最多保留的条数");

        // UI - Sort
        Add("ui.sort.mode.name", "名称");
        Add("ui.sort.mode.value", "价值");
        Add("ui.sort.mode.weight", "重量");
        Add("ui.sort.ascending", "↑ 升序");
        Add("ui.sort.descending", "↓ 降序");
        Add("ui.sort.mode_tip", "排序方式");
        Add("ui.sort.mode_desc", "按{0}排序");
        Add("ui.sort.order_tip", "排序顺序");
        Add("ui.sort.completed", "已整理: {0} {1}");
        Add("ui.sort.no_change", "无需整理");
        Add("ui.sort.execute_tip", "整理");
        Add("ui.sort.execute_desc", "整理容器物品");

        // Log - PlayerCameraPatch - Pin Yin
        Add("log.player_camera_patch.pinyin.library.not_found", "TinyPinyin 库未加载 — 拼音搜索已禁用");
        Add("log.player_camera_patch.pinyin.api_not_found", "TinyPinyin API 方法未找到 — 拼音搜索已禁用");
        Add("log.player_camera_patch.pinyin.init_failed", "TinyPinyin 初始化失败: {0} — 拼音搜索已禁用");

        // Log - ItemPatch
        Add("log.item_patch.durability_exhaustion_alert", "{0} 的耐久度已降至 {1}%");
    }
}