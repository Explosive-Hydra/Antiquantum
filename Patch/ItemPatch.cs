using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using MossLib.Tool;
using Newtonsoft.Json.Linq;

namespace Quantum.Patch;

[HarmonyPatch(typeof(Item))]
public static class ItemPatch
{
    private const string LocaleKeyPre = "item.";
    private const string LogLocaleKeyPre = "log.item_patch.";

    private static readonly Dictionary<string, Dictionary<string, string>> LangCache = new();
    private static readonly Dictionary<string, int> AlertedItemPercents = [];
    private static double _lastDurabilityCheckTime;
    private const double DurabilityCheckInterval = 2.0;
    private static int _lastCheckedFrame = -1;

    [HarmonyPatch("Update")]
    [HarmonyPrefix]
    private static void UpdatePrefix()
    {
        if (Item.GlobalItems == null)
            return;

        var currentFrame = Time.frameCount;
        if (currentFrame == _lastCheckedFrame)
            return;
        _lastCheckedFrame = currentFrame;

        var now = Time.realtimeSinceStartup;
        if (now - _lastDurabilityCheckTime < DurabilityCheckInterval)
            return;
        _lastDurabilityCheckTime = now;

        var threshold = Plugin.FavouritedItemDurabilityExhaustionAlert.Value;

        if (threshold <= 0f)
            return;

        var items = Inventory.GetAllItems();
        if (items == null)
            return;

        const int alertStep = 5; // 每下降 5% 提醒一次

        foreach (var item in items
                     .Where(item => item != null
                                    && item.favourited
                                    && item.id != null))
        {
            var condPercent = Mathf.FloorToInt(item.condition * 100f);

            if (item.condition >= threshold)
            {
                // 耐久恢复至阈值以上，清除记录
                AlertedItemPercents.Remove(item.id);
                continue;
            }

            // 首次低于阈值或下降 >= 5% 时提醒
            if (!AlertedItemPercents.TryGetValue(item.id, out var lastPercent))
            {
                // 首次：直接提醒，记录当前百分比
                AlertedItemPercents[item.id] = condPercent;
            }
            else if (lastPercent - condPercent >= alertStep)
            {
                // 下降了 >= 5%，再次提醒，更新记录
                AlertedItemPercents[item.id] = condPercent;
            }
            else
            {
                continue;
            }

            var itemName = item.fullName ?? item.id;

            LogAlert("durability_exhaustion_alert",
                itemName, condPercent);
        }
    }

    [HarmonyPatch("SetupItems")]
    [HarmonyPostfix]
    public static void SetupItemsPostfix()
    {
        if (Item.GlobalItems == null)
            return;

        var query = from kvp in Item.GlobalItems
            let itemId = kvp.Key
            let itemInfo = kvp.Value
            where itemInfo != null
            select new { itemId, itemInfo };

        foreach (var item in query)
        {
            var extra = BuildInfo(item.itemId, item.itemInfo);
            if (!string.IsNullOrEmpty(extra))
                item.itemInfo.description = AppendIfMissing(
                    item.itemInfo.description ?? "", extra);
        }
    }

    private static string BuildInfo(string id, ItemInfo info)
    {
        if (info == null)
            return null;


        var result = "";
        result += $"ID: {id}\n";

        // 双语名称：在物品原名后附加指定语言的翻译
        var bilingualCode = Plugin.BilingualName.Value?.Trim();
        if (!string.IsNullOrEmpty(bilingualCode)
            && !string.Equals(bilingualCode, global::Locale.currentLangName, StringComparison.OrdinalIgnoreCase))
        {
            var secondName = GetItemNameInLang(id, bilingualCode);
            if (!string.IsNullOrEmpty(secondName) && !info.fullName.Contains(secondName))
            {
                result += RichText.Italic($"({secondName})\n");
            }
        }

        if (!ModLocale.HasLocaleKey(LocaleKeyPre + id))
            return string.IsNullOrEmpty(result.Trim())
                ? null
                : result.TrimEnd('\n');
        result += Locale(id);
        result += "\n";

        return string.IsNullOrEmpty(result.Trim())
            ? null
            : result.TrimEnd('\n');
    }

    private static string GetItemNameInLang(string itemId, string langCode)
    {
        // 尝试从缓存获取
        if (LangCache.TryGetValue(langCode, out var mainDict))
            return mainDict?.TryGetValue(itemId, out var name) == true ? name : null;

        // 加载语言文件
        var path = $"{Application.dataPath}/Lang/{langCode}.json";
        if (!File.Exists(path))
        {
            LangCache[langCode] = null;
            return null;
        }

        try
        {
            var json = File.ReadAllText(path);
            var obj = JObject.Parse(json);
            mainDict = obj["main"]?.ToObject<Dictionary<string, string>>();
            LangCache[langCode] = mainDict; // 可能为 null（JSON 中无 main 字段时）
        }
        catch
        {
            LangCache[langCode] = null;
            return null;
        }

        return mainDict?.TryGetValue(itemId, out var result) == true ? result : null;
    }

    private static string AppendIfMissing(string current, string addition)
    {
        if (string.IsNullOrWhiteSpace(addition)
            || current.IndexOf(addition,
                StringComparison.OrdinalIgnoreCase) >= 0)
            return current;

        return string.IsNullOrWhiteSpace(current)
            ? addition
            : $"{current.TrimEnd()}\n\n{addition}";
    }

    private static string Locale(string key, params object[] args)
    {
        return ModLocale.GetFormat($"{LocaleKeyPre}{key}", args);
    }
    
    private static void LogAlert(string text, params object[] args)
    {
        Log.Alert(ModLocale.GetFormat(LogLocaleKeyPre + text, args), Plugin.Logger, false);
    }
}