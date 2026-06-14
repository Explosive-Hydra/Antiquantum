using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using MossLib.Tool;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Quantum.Patch;

[HarmonyPatch(typeof(PlayerCamera))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class PlayerCameraPatch
{
    private static readonly ManualLogSource Logger = Plugin.Logger;
    private static readonly Dictionary<string, List<Recipe>> ProductToRecipes = new();

    private static readonly Dictionary<string, string> PinyinCache = new();
    private static bool _pinyinInitialized;
    private static bool _hasPinyinLibrary;
    private static Func<string, string, string> _getPinyin;
    private static Func<string, string, string> _getPinyinInitials;
    private static string _pinyinFilterCache;

    private static TextMeshProUGUI _ammunitionText;
    private static GameObject _ammunitionUiObject;
    private static int _remainingAmmunition;
    private static int _maximumAmmunition;

    private static SortMode _currentSortMode;
    private static bool _currentSortAscending = true;
    private static GameObject _sortButton;
    private static GameObject _orderButton;
    private static GameObject _executeButton;

    private static TMP_FontAsset GameFont
    {
        get
        {
            field = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault(f
                => f.name.Contains("Retro Gaming SDF"));
            return field;
        }
    }

    private static void EnsurePinyinLibrary()
    {
        if (_pinyinInitialized)
            return;
        _pinyinInitialized = true;

        try
        {
            var type = Type.GetType("TinyPinyin.PinyinHelper, TinyPinyin");
            if (type == null)
            {
                Warning("pinyin.library.not_found");
                return;
            }

            var getPinyin = type.GetMethod("GetPinyin", [typeof(string), typeof(string)]);
            var getInitials = type.GetMethod("GetPinyinInitials", [typeof(string), typeof(string)]);

            if (getPinyin == null || getInitials == null)
            {
                Warning("pinyin.api_not_found");
                return;
            }

            _getPinyin = (str, sep) => (string)getPinyin.Invoke(null, [str, sep]);
            _getPinyinInitials = (str, sep) => (string)getInitials.Invoke(null, [str, sep]);
            _hasPinyinLibrary = true;
        }
        catch (Exception ex)
        {
            Warning("pinyin.init_failed", ex.Message);
        }
    }

    [HarmonyPatch("RefreshRecipeList")]
    [HarmonyPrefix]
    private static void PreRefreshRecipeList(PlayerCamera __instance)
    {
        _pinyinFilterCache = null;

        var filter = __instance.recipeFilter;
        if (string.IsNullOrEmpty(filter))
            return;

        // 如果 filter 包含非 ASCII 字符（中文直接输入），走原始逻辑即可
        var isAscii = filter.All(t => t <= 127);

        if (!isAscii)
            return;

        // 纯 ASCII → 可能是拼音输入，绕过原始过滤
        EnsurePinyinLibrary();
        if (!_hasPinyinLibrary)
            return;

        _pinyinFilterCache = filter;
        __instance.recipeFilter = "";
    }

    [HarmonyPatch("RefreshRecipeList")]
    [HarmonyPostfix]
    private static void PostRefreshRecipeList(PlayerCamera __instance)
    {
        var rawFilter = _pinyinFilterCache;
        _pinyinFilterCache = null;
        if (rawFilter == null)
            return;

        var cleanFilter = rawFilter.Replace(" ", "").ToUpperInvariant();

        // recipeObjects 是 PlayerCamera 的私有字段，通过反射访问
        var recipeObjectsField = AccessTools.Field(typeof(PlayerCamera), "recipeObjects");
        var recipeObjects = (List<GameObject>)recipeObjectsField.GetValue(__instance);
        if (recipeObjects == null || recipeObjects.Count == 0)
            return;

        // 先重排：将匹配项推到列表前面，不匹配项推到后面
        // 再过滤：销毁不匹配项
        var matched = new List<GameObject>(recipeObjects.Count);
        var unmatched = new List<GameObject>(recipeObjects.Count);

        foreach (var go in recipeObjects)
        {
            var displayName = go.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text;
            if (IsPinyinMatch(displayName, cleanFilter))
                matched.Add(go);
            else
                unmatched.Add(go);
        }

        // 重排：匹配项在前，不匹配项在后
        recipeObjects.Clear();
        recipeObjects.AddRange(matched);
        recipeObjects.AddRange(unmatched);

        // 过滤：销毁不匹配项
        foreach (var go in unmatched)
            Object.Destroy(go);

        // 修正匹配项的 anchoredPosition，使其连续排列在顶部
        // 原始代码中每个 recipe 的位置是 -index * 64
        for (var i = 0; i < matched.Count; i++)
        {
            var rect = matched[i].GetComponent<RectTransform>();
            if (rect != null)
                rect.anchoredPosition = new Vector2(-9f, -i * 64);
        }

        // 修正 recipeListContent 的 sizeDelta 以适应新的数量
        var recipeListContent =
            (RectTransform)AccessTools.Field(typeof(PlayerCamera), "recipeListContent").GetValue(__instance);
        if (recipeListContent != null)
            recipeListContent.sizeDelta = new Vector2(1f, matched.Count * 64);
    }

    private static bool IsPinyinMatch(string displayName, string filter)
    {
        // 1. 原名匹配
        if (displayName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        // 2. 拼音匹配
        EnsurePinyinLibrary();
        if (!_hasPinyinLibrary)
            return false;

        if (!PinyinCache.TryGetValue(displayName, out var pinyin))
        {
            try
            {
                var full = _getPinyin(displayName, "");
                var initials = _getPinyinInitials(displayName, "");
                pinyin = full + "\n" + initials;
            }
            catch
            {
                pinyin = "";
            }

            PinyinCache[displayName] = pinyin;
        }

        if (string.IsNullOrEmpty(pinyin))
            return false;

        var parts = pinyin.Split('\n');

        // 全拼匹配
        if (parts[0].IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        // 首字母简码匹配
        if (parts.Length > 1 && parts[1].IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        // 逐字符顺序匹配：用于 "bdai" → "BENGDAI" 这类模糊拼音
        // 检查 filter 的每个字符是否按顺序出现在全拼中
        if (SequentialMatch(parts[0], filter))
            return true;

        // 简码也尝试逐字符
        return parts.Length > 1 && SequentialMatch(parts[1], filter);
    }

    private static bool SequentialMatch(string text, string pattern)
    {
        var ti = 0;
        foreach (var t in pattern)
        {
            ti = text.IndexOf(t, ti);
            if (ti < 0)
                return false;
            ti++;
        }

        return true;
    }

    [HarmonyPatch("OpenContainer")]
    [HarmonyPostfix]
    private static void OnContainerOpened(PlayerCamera __instance)
    {
        CreateSortButtons(__instance);
    }

    [HarmonyPatch("CloseContainer")]
    [HarmonyPostfix]
    private static void OnContainerClosed()
    {
        DestroySortButtons();
    }

    [HarmonyPatch("HandleInput")]
    [HarmonyPostfix]
    private static void OnHandleInputPostfix(PlayerCamera __instance)
    {
        if (!Input.GetKeyDown(Plugin.SortKey.Value))
            return;
        if (!CanSort(__instance))
            return;
        SortContainer(__instance);
    }

    private static bool CanSort(PlayerCamera camera)
    {
        if (camera == null || !camera.isActiveAndEnabled)
            return false;
        if (camera.currentContainer == null)
            return false;
        if (camera.craftingPanel != null && camera.craftingPanel.activeSelf)
            return false;
        if (camera.tradeMenu != null && camera.tradeMenu.activeSelf)
            return false;
        return camera.dragItem == null;
    }

    private static void SortContainer(PlayerCamera camera)
    {
        var container = camera.currentContainer;
        if (container == null)
            return;

        var items = GetDirectContainerItems(container);
        if (items.Count <= 1)
            return;

        var sorted = SortItems(items);
        if (HasSameOrder(items, sorted))
        {
            ShowSortNotification(true);
            return;
        }

        foreach (var item in items)
            container.UnloadItem(item);

        foreach (var item in sorted)
            container.LoadItem(item);

        camera.RepopulateContainer();
        camera.PlayUISound(PlayerCamera.UISoundType.Click);
        ShowSortNotification(false);
    }

    private static List<Item> GetDirectContainerItems(Container container)
    {
        var items = new List<Item>(container.transform.childCount);
        for (var i = 0; i < container.transform.childCount; i++)
        {
            var child = container.transform.GetChild(i);
            if (child == null) continue;
            var item = child.GetComponent<Item>();
            if (item != null)
                items.Add(item);
        }

        return items;
    }

    private static List<Item> SortItems(List<Item> items)
    {
        var ascending = _currentSortAscending;

        return _currentSortMode switch
        {
            SortMode.Name => ascending
                ? items.OrderByDescending(i => i.favourited)
                    .ThenBy(i => i.id, StringComparer.OrdinalIgnoreCase).ToList()
                : items.OrderByDescending(i => i.favourited)
                    .ThenByDescending(i => i.id, StringComparer.OrdinalIgnoreCase).ToList(),

            SortMode.Value => ascending
                ? items.OrderByDescending(i => i.favourited)
                    .ThenBy(GetItemValue).ToList()
                : items.OrderByDescending(i => i.favourited)
                    .ThenByDescending(GetItemValue).ToList(),

            SortMode.Weight => ascending
                ? items.OrderByDescending(i => i.favourited)
                    .ThenBy(i => i.Stats.weight).ToList()
                : items.OrderByDescending(i => i.favourited)
                    .ThenByDescending(i => i.Stats.weight).ToList(),

            _ => items
        };
    }

    private static float GetItemValue(Item item)
    {
        float val = item.Stats.value;
        if (item.condition > 0f)
            val *= item.condition;
        return val;
    }

    private static bool HasSameOrder(IReadOnlyList<Item> current, IReadOnlyList<Item> sorted)
    {
        if (current.Count != sorted.Count)
            return false;
        return !current.Where((t, i) => t != sorted[i]).Any();
    }

    private static void CreateSortButtons(PlayerCamera camera)
    {
        if (camera.containerMenu == null)
            return;
        var top = camera.containerMenu.transform.Find("Top");
        if (top == null)
            return;
        var weight = top.Find("Weight");
        if (weight == null)
            return;

        DestroySortButtons();

        var weightRect = weight.GetComponent<RectTransform>();
        var baseX = weightRect.anchoredPosition.x + weightRect.sizeDelta.x + 5f;
        var y = weightRect.anchoredPosition.y - 3f;
        var layer = LayerMask.NameToLayer("UI");

        var modeLabel = GetSortModeLabel(_currentSortMode);
        _sortButton = CreateSmallButton(top, "SortButton", layer,
            modeLabel.Length > 0
                ? modeLabel.Substring(0, 1)
                : "?", baseX - 50f, y - 5f);

        _orderButton = CreateSmallButton(top, "OrderButton", layer,
            _currentSortAscending
                ? "↑"
                : "↓", baseX, y - 5f);

        _executeButton = CreateSmallButton(top, "ExecuteButton", layer,
            "S", baseX + 50f, y - 5f);

        AddSortModeClickEvent(_sortButton, camera);
        AddSortExecuteClickEvent(_executeButton, camera);
        AddOrderClickEvent(_orderButton, camera);

        AddTooltip(_sortButton,
            Locale("ui.sort.mode_tip"),
            Locale("ui.sort.mode_desc", GetSortModeLabel(_currentSortMode)));

        AddTooltip(_executeButton,
            Locale("ui.sort.execute_tip"),
            Locale("ui.sort.execute_desc"));

        AddTooltip(_orderButton,
            Locale("ui.sort.order_tip"),
            _currentSortAscending
                ? Locale("ui.sort.ascending")
                : Locale("ui.sort.descending"));
    }

    private static void DestroySortButtons()
    {
        if (_sortButton != null)
        {
            Object.Destroy(_sortButton);
            _sortButton = null;
        }

        if (_executeButton != null)
        {
            Object.Destroy(_executeButton);
            _executeButton = null;
        }

        if (_orderButton == null) return;
        Object.Destroy(_orderButton);
        _orderButton = null;
    }

    private static GameObject CreateSmallButton(
        Transform parent, string name, int uiLayer,
        string buttonText, float xPos, float yPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.layer = uiLayer;

        var rect = go.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(48f, 48f);
        rect.anchoredPosition = new Vector2(xPos, yPos);

        // 外层白色 — 形成边框背景
        var image = go.AddComponent<Image>();
        image.raycastTarget = true;
        image.color = Color.white;

        // 内层黑色填充（游戏处理为透明），比父级小 4px 露出白色边框
        var fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(go.transform, false);
        fillGo.layer = uiLayer;
        var fillRect = fillGo.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = new Vector2(-4f, -4f);
        var fillImage = fillGo.AddComponent<Image>();
        fillImage.color = Color.black;
        fillImage.raycastTarget = false;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        textGo.layer = uiLayer;
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = buttonText;
        tmp.fontSize = 16f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        var oldBtn = go.GetComponent<Button>();
        if (oldBtn != null)
            Object.Destroy(oldBtn);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = fillImage;
        btn.colors = new ColorBlock
        {
            normalColor = new Color(0f, 0f, 0f, 1f),
            highlightedColor = new Color(0f, 0f, 0f, 1f),
            pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
        btn.navigation = new Navigation { mode = Navigation.Mode.None };

        return go;
    }

    private static void AddTooltip(GameObject target, string name, string desc)
    {
        var old = target.GetComponent<UITooltip>();
        if (old != null)
            Object.Destroy(old);
        var tip = target.AddComponent<UITooltip>();
        tip.tipName = name;
        tip.tipDesc = desc;
        tip.skipLocale = true;
    }

    private static void AddSortModeClickEvent(GameObject button, PlayerCamera camera)
    {
        var trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener(data =>
        {
            var ped = (PointerEventData)data;
            switch (ped.button)
            {
                // 左键 / 右键 → 切换排序模式
                case PointerEventData.InputButton.Left:
                case PointerEventData.InputButton.Right:
                {
                    var modes = Enum.GetValues(typeof(SortMode));
                    _currentSortMode = (SortMode)(((int)_currentSortMode + 1) % modes.Length);
                    UpdateSortButtonText();
                    UpdateOrderButtonText();
                    camera.PlayUISound(PlayerCamera.UISoundType.Click);
                    break;
                }
                case PointerEventData.InputButton.Middle:
                default:
                    break;
            }
        });
        trigger.triggers.Add(entry);
    }

    private static void AddSortExecuteClickEvent(GameObject button, PlayerCamera camera)
    {
        var trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener(data =>
        {
            var ped = (PointerEventData)data;
            switch (ped.button)
            {
                case PointerEventData.InputButton.Left:
                case PointerEventData.InputButton.Right:
                    SortContainer(camera);
                    return;
                case PointerEventData.InputButton.Middle:
                default:
                    break;
            }
        });
        trigger.triggers.Add(entry);
    }

    private static void AddOrderClickEvent(GameObject button, PlayerCamera camera)
    {
        var trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.AddComponent<EventTrigger>();
        trigger.triggers.Clear();

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
        entry.callback.AddListener(data =>
        {
            var ped = (PointerEventData)data;
            if (ped.button is not (PointerEventData.InputButton.Left or PointerEventData.InputButton.Right))
                return;

            _currentSortAscending = !_currentSortAscending;

            var tmp = _orderButton?.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = _currentSortAscending
                    ? "↑"
                    : "↓";

            AddTooltip(_orderButton,
                Locale("ui.sort.order_tip"),
                _currentSortAscending
                    ? Locale("ui.sort.ascending")
                    : Locale("ui.sort.descending"));

            camera.PlayUISound(PlayerCamera.UISoundType.Click);
        });
        trigger.triggers.Add(entry);
    }

    private static string GetSortModeLabel(SortMode mode)
    {
        return mode switch
        {
            SortMode.Name => Locale("ui.sort.mode.name"),
            SortMode.Value => Locale("ui.sort.mode.value"),
            SortMode.Weight => Locale("ui.sort.mode.weight"),
            _ => "?"
        };
    }

    private static void UpdateSortButtonText()
    {
        var tmp = _sortButton?.transform.Find("Text")?.GetComponent<TextMeshProUGUI>();
        if (tmp != null)
        {
            var label = GetSortModeLabel(_currentSortMode);
            tmp.text = label.Length > 0
                ? label.Substring(0, 1)
                : "?";
        }

        AddTooltip(_sortButton,
            Locale("ui.sort.mode_tip"),
            Locale("ui.sort.mode_desc", GetSortModeLabel(_currentSortMode)));
    }

    private static void UpdateOrderButtonText()
    {
        AddTooltip(_orderButton,
            Locale("ui.sort.order_tip"),
            _currentSortAscending
                ? Locale("ui.sort.ascending")
                : Locale("ui.sort.descending"));
    }

    private static void ShowSortNotification(bool noChange)
    {
        var modeLabel = _currentSortMode switch
        {
            SortMode.Name => Locale("ui.sort.mode.name"),
            SortMode.Value => Locale("ui.sort.mode.value"),
            SortMode.Weight => Locale("ui.sort.mode.weight"),
            _ => ""
        };
        var orderLabel = _currentSortAscending
            ? Locale("ui.sort.ascending")
            : Locale("ui.sort.descending");

        Alert(noChange
            ? Locale("ui.sort.no_change")
            : Locale("ui.sort.completed", modeLabel, orderLabel));
    }

    [HarmonyPatch("HandleRadialMenu")]
    [HarmonyPostfix]
    private static void HandleRadialMenuPostfix(PlayerCamera __instance)
    {
        if (__instance.weightText == null || __instance.body == null)
            return;

        var totalValue = Inventory.GetAllItemInfosThorough().Sum(info => info.value);
        __instance.weightText.text +=
            $" <color=#f6ff73>{totalValue}</color>";
    }

    [HarmonyPatch("HandleGunMenu")]
    [HarmonyPostfix]
    private static void HandleGunMenuPostfix(PlayerCamera __instance)
    {
        if (!Plugin.AmmunitionUi.Value) return;

        var handSlot = __instance.body.handSlot;
        if (!__instance.body.HoldingItem(handSlot))
        {
            DestroyAmmunitionUi();
            return;
        }

        var item = __instance.body.GetItem(handSlot);
        if (!item.Stats.HasTag("gun"))
        {
            DestroyAmmunitionUi();
            return;
        }

        var component = item.GetComponent<GunScript>();

        _remainingAmmunition = component.roundsInMag;
        _maximumAmmunition = component.magCapacity;

        CreateOrUpdateAmmunitionUi(__instance);
        UpdateAmmunitionUi();

        SyncVisibility(__instance.gunMenu);
    }

    private static void CreateOrUpdateAmmunitionUi(PlayerCamera camera)
    {
        if (_ammunitionUiObject == null)
        {
            var ammunitionUi = new GameObject("AmmunitionUi");
            Object.DontDestroyOnLoad(ammunitionUi);

            var canvas = ammunitionUi.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var canvasScaler = ammunitionUi.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920f, 1080f);

            ammunitionUi.AddComponent<GraphicRaycaster>();

            _ammunitionUiObject = ammunitionUi;

            var gameObject = new GameObject("AmmunitionText");
            gameObject.transform.SetParent(_ammunitionUiObject.transform, false);

            var rectTransform = gameObject.AddComponent<RectTransform>();
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = new Vector2(150f, 30f);

            _ammunitionText = gameObject.AddComponent<TextMeshProUGUI>();
            _ammunitionText.alignment = TextAlignmentOptions.Center;

            _ammunitionText.font = GameFont;
        }

        var gunMenuPos = GetGunMenuPosition(camera);
        var textRectTransform = _ammunitionText.GetComponent<RectTransform>();
        textRectTransform.anchoredPosition = new Vector2(gunMenuPos.x, gunMenuPos.y - 450f);

        SyncVisibility(camera.gunMenu);
    }

    private static Vector2 GetGunMenuPosition(PlayerCamera camera)
    {
        if (camera.gunMenu == null) return new Vector2(0f, 50f);
        var gunMenuRect = camera.gunMenu.GetComponent<RectTransform>();
        if (gunMenuRect == null) return new Vector2(0f, 50f);
        var pos = gunMenuRect.anchoredPosition;
        pos.y -= gunMenuRect.rect.height * 0.5f;
        return pos;
    }

    private static void UpdateAmmunitionUi()
    {
        var realRemainingAmmunition = GunScriptPatch.HasOne ? _remainingAmmunition + 1 : _remainingAmmunition;
        if (_ammunitionText == null)
            return;

        if (!Plugin.InfiniteAmmunition.Value)
        {
            if (realRemainingAmmunition >= 0.8)
                _ammunitionText.color = Color.green;
            else if (realRemainingAmmunition >= 0.5)
                _ammunitionText.color = Color.yellow;
            else
                _ammunitionText.color = Color.red;

            _ammunitionText.fontSize = 32;
            _ammunitionText.text = $"{realRemainingAmmunition} / {_maximumAmmunition + 1}";
        }
        else
        {
            _ammunitionText.fontSize = 64;
            _ammunitionText.color = Color.black;
            _ammunitionText.text = "∞";
        }
    }

    private static void SyncVisibility(GameObject gunMenu)
    {
        if (_ammunitionUiObject == null || gunMenu == null)
            return;

        // 弹药 UI 仅在枪械菜单打开且没有其他覆盖界面时显示
        // 制作界面、医疗面板、交易菜单、暂停界面打开时隐藏
        var camera = PlayerCamera.main;
        if (camera == null)
        {
            _ammunitionUiObject.SetActive(gunMenu.activeSelf);
            return;
        }

        var shouldShow = gunMenu.activeSelf
                         && !camera.craftingPanel.activeSelf
                         && !camera.woundView.activeSelf
                         && !camera.tradeMenu.activeSelf
                         && !PauseHandler.paused;

        _ammunitionUiObject.SetActive(shouldShow);
    }

    public static void DestroyAmmunitionUi()
    {
        if (_ammunitionUiObject == null) return;
        Object.Destroy(_ammunitionUiObject);
        _ammunitionUiObject = null;
        _ammunitionText = null;
    }

    [HarmonyPatch("ItemHoverDescription")]
    [HarmonyPostfix]
    public static void Postfix(Item item, ref ValueTuple<string, string> __result)
    {
        if (item == null || item.Stats?.rec is not { recognizable: true })
            return;

        // Shift 没按住时原版显示"按住Shift展开"，不干涉
        if (!Input.GetKey(KeyBinds.GetBind("expanddesc")))
            return;

        var description = __result.Item2;
        var extraInfo = BuildTechnicalInfo(item);
        if (string.IsNullOrEmpty(extraInfo)) return;

        // Shift 按住时原版"按住Shift展开"消失，加上"松开Shift"替代
        var hint =
            $"<color=#a2e8af><sprite index=2 tint=1><i>{Locale("key.shift_to_expand.down")}</i></color>\n";
        extraInfo = hint + extraInfo;

        if (string.IsNullOrEmpty(description))
            __result.Item2 = extraInfo;
        else if (description.IndexOf(extraInfo, StringComparison.OrdinalIgnoreCase) < 0)
            __result.Item2 = $"{description.TrimEnd()}\n{extraInfo}";
    }

    private static string BuildTechnicalInfo(Item item)
    {
        var info = item.Stats;
        if (info == null)
            return null;

        var needCtrl = Plugin.CtrlToExpand.Value;
        var ctrlHeld = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        var result = "";

        // 物品专属描述
        if (ModLocale.HasLocaleKey($"hover.{item.id}"))
        {
            result += "\n";
            result += Locale($"hover.{item.id}");
            result += "\n\n";
        }

        // Ctrl 提示行 + 配方（二级展开）
        var recipeInfo = BuildRecipeString(item.id);
        var showRecipe = !needCtrl || ctrlHeld;

        var ctrlHint = ctrlHeld
            ? Locale("key.ctrl_to_expand.down")
            : Locale("key.ctrl_to_expand.up");
        if (needCtrl) result += $"<color=#a2e8af><sprite index=2 tint=1><i>{ctrlHint}</i></color>\n";

        if (showRecipe && !string.IsNullOrEmpty(recipeInfo))
            result += recipeInfo + "\n\n";

        // 技术标志（始终显示）
        result += info.usable
            ? RichText.Green("✓ " + Locale("hover.info.usable.true"))
            : RichText.Red("X  " + Locale("hover.info.usable.false"));
        result += "\n";

        result += info.usableOnLimb
            ? RichText.Green("✓ " + Locale("hover.info.usable_on_limb.true"))
            : RichText.Red("X  " + Locale("hover.info.usable_on_limb.false"));
        result += "\n";

        result += info.autoAttack
            ? Locale("hover.info.auto_attack") + "\n"
            : null;

        result += info.usableWithLMB
            ? Locale("hover.info.usable_with_lrb") + "\n"
            : null;

        result += info.ignoreDepression
            ? RichText.Color(Locale("hover.info.ignore_depression"), "#FFFB91") + "\n"
            : null;

        return string.IsNullOrEmpty(result.Trim())
            ? null
            : result.TrimEnd('\n');
    }

    private static string BuildRecipeString(string itemId)
    {
        var recipes = GetRecipesByProduct(itemId);
        if (recipes == null || recipes.Count == 0)
            return null;

        var recipeBlocks = new List<string>();

        foreach (var recipe in recipes)
        {
            if (recipe?.items == null || recipe.items.Count == 0)
                continue;

            // 合并相同材料
            var grouped = recipe.items
                .Where(ri => ri != null)
                .GroupBy(ri => new
                {
                    ri.specific,
                    ri.specificId,
                    ri.isLiquid,
                    qualityId = ri.quality?.id,
                    qualityAmount = Math.Round(ri.quality?.amount ?? 0f, 4),
                    ri.minimumCondition,
                    ri.destroyItem
                })
                .Select(g => new { Item = g.First(), Count = g.Count() })
                .ToList();

            var blockLines = new List<string>();

            foreach (var g in grouped)
            {
                var ri = g.Item;
                var count = g.Count;

                string nameLine;
                if (!ri.specific)
                {
                    if (ri.isLiquid)
                        nameLine = global::Locale.GetOther("craftanyliquid");
                    else if (ri.quality is { id: "hammering" or "cutting" })
                        nameLine = global::Locale.GetOther("craftanytool");
                    else
                        nameLine = global::Locale.GetOther("craftanyitem");
                }
                else
                {
                    nameLine = ri.isLiquid
                        ? global::Locale.GetOther(ri.specificId)
                        : global::Locale.GetItem(ri.specificId);
                }

                if (count > 1)
                    nameLine += $" x{count}";

                var constraints = new List<string>();

                if (ri.isLiquid)
                {
                    switch (ri.specific)
                    {
                        case false when ri.quality != null:
                        {
                            var q = global::Locale.GetOther("craftliquidquality")
                                .Replace("<1>", ri.quality.amount.ToString("0.#"))
                                .Replace("<2>", ri.quality.LocaleName);
                            constraints.Add(q);
                            break;
                        }
                        case true when ri.minimumCondition > 0f:
                        {
                            var m = global::Locale.GetOther("craftml")
                                .Replace("<>", ri.minimumCondition.ToString("0.#"));
                            constraints.Add(m);
                            break;
                        }
                    }
                }
                else
                {
                    if (!ri.specific && ri.quality != null)
                    {
                        var q = global::Locale.GetOther("craftitemquality")
                            .Replace("<1>", ri.quality.amount.ToString("0.#"))
                            .Replace("<2>", ri.quality.LocaleName);
                        constraints.Add(q);

                        if (Recipes.QualityExamples != null)
                        {
                            var example = Recipes.QualityExamples
                                .FirstOrDefault(kvp =>
                                    kvp.Key.id == ri.quality.id &&
                                    Math.Abs(kvp.Key.amount - ri.quality.amount) < 0.001f);
                            if (example.Value != null)
                            {
                                var ex = global::Locale.GetOther("craftexample")
                                    .Replace("<>", global::Locale.GetItem(example.Value));
                                constraints.Add(ex);
                            }
                        }
                    }

                    if (ri.minimumCondition > 0f)
                    {
                        var c = global::Locale.GetOther("craftcondition")
                            .Replace("<>",
                                PlayerCamera.ConditionToColorCode(ri.minimumCondition) +
                                (ri.minimumCondition * 100f).ToString("0.#") +
                                "</color>"
                            );
                        constraints.Add(c);
                    }
                }

                if (constraints.Count > 0)
                    nameLine += " " + string.Join(" ", constraints);

                blockLines.Add($"  - {nameLine}");
            }

            if (blockLines.Count <= 0) continue;

            recipeBlocks.Add(string.Join("\n", blockLines));
        }

        return recipeBlocks.Count > 0
            ? RichText.White("\n" +
                             Locale("hover.info.recipe") +
                             "\n" +
                             string.Join("\n", recipeBlocks))
            : null;
    }

    private static void EnsureRecipeLookup()
    {
        if (ProductToRecipes.Count > 0)
            return;
        if (Recipes.recipes == null || Recipes.recipes.Count == 0)
            return;
        foreach (var recipe in Recipes.recipes)
        {
            if (recipe?.result == null || string.IsNullOrEmpty(recipe.result.id))
                continue;
            var pid = recipe.result.id;
            if (!ProductToRecipes.ContainsKey(pid))
                ProductToRecipes[pid] = [];
            ProductToRecipes[pid].Add(recipe);
        }
    }

    private static List<Recipe> GetRecipesByProduct(string productId)
    {
        EnsureRecipeLookup();
        ProductToRecipes.TryGetValue(productId, out var list);
        return list ?? [];
    }

    private static string Locale(string key, params object[] args)
    {
        return ModLocale.GetFormat(key, args);
    }

    private static void Alert(string text, bool important = false, float delay = 0f)
    {
        Log.Alert(text, Logger, important, delay);
    }

    private static void Warning(string text, params object[] args)
    {
        Log.Warning(Locale(text, args), Logger);
    }

    private enum SortMode
    {
        Name,
        Value,
        Weight
    }
}