using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using MossLib.Tool;
using Newtonsoft.Json.Linq;

namespace Quantum.Patch;

[HarmonyPatch(typeof(Item))]
public static class ItemPatch
{
    private const string LocaleKeyPre = "item.";

    private static readonly Dictionary<string, Dictionary<string, string>> LangCache = new();

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
        var path = $"{UnityEngine.Application.dataPath}/Lang/{langCode}.json";
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
}