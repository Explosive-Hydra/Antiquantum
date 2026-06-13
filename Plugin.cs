using System.Collections.Generic;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using MossLib.Tool;
using Quantum.Lang;
using UnityEngine;

namespace Quantum;

[BepInPlugin(Guid, Name, Version)]
public class Plugin : BaseUnityPlugin
{
    public const string Guid = "org.explosivehydra.quantum";
    public const string Name = "Quantum";
    public const string Version = "1.0.0";
    internal new static ManualLogSource Logger;
    private readonly Harmony _harmony = new(Guid);
    private static readonly Dictionary<string, ConfigEntryBase> Registry = new();

    // Info
    public static ConfigEntry<bool> CtrlToExpand;
    public static ConfigEntry<bool> AmmunitionUi;

    // Item - Gun
    public static ConfigEntry<bool> AutoRack;
    public static ConfigEntry<bool> IndestructibleGun;
    public static ConfigEntry<bool> InfiniteAmmunition;
    public static ConfigEntry<bool> NeverJam;
    public static ConfigEntry<bool> NoCasing;
    public static ConfigEntry<bool> Recoilless;
    
    // UI
    public static ConfigEntry<KeyCode> SortKey;

    public void Awake()
    {
        Logger = base.Logger;

        LocaleGenerator.SetLogger(Logger);
        LocaleGenerator.Register(new EnLangGenerator(), Logger);
        LocaleGenerator.Register(new ZhCnLangGenerator(), Logger);
        LocaleGenerator.Register(new ZhTwLangGenerator(), Logger);
        LocaleGenerator.GenerateAll();

        ModLocale.Initialize(Logger);
        _harmony.PatchAll();

        // Info
        CtrlToExpand = RegisterConfigInfo(Config, nameof(CtrlToExpand).ToSnakeCase(), true);
        
        // Item - Gun
        AutoRack = RegisterConfigItemGun(Config, nameof(AutoRack).ToSnakeCase(), false);
        IndestructibleGun = RegisterConfigItemGun(Config, nameof(IndestructibleGun).ToSnakeCase(), false);
        InfiniteAmmunition = RegisterConfigItemGun(Config, nameof(InfiniteAmmunition).ToSnakeCase(), false);
        NeverJam = RegisterConfigItemGun(Config, nameof(NeverJam).ToSnakeCase(), false);
        NoCasing = RegisterConfigItemGun(Config, nameof(NoCasing).ToSnakeCase(), false);
        Recoilless = RegisterConfigItemGun(Config, nameof(Recoilless).ToSnakeCase(), false);
        
        // UI
        AmmunitionUi = RegisterConfigUi(Config, nameof(AmmunitionUi).ToSnakeCase(), true);
        SortKey = RegisterConfigUi(Config, nameof(SortKey).ToSnakeCase(), KeyCode.E);
    }

    private static ConfigEntry<T> RegisterConfigInfo<T>(ConfigFile configFile, string key, T defaultValue)
    {
        return RegisterConfig(configFile, "Info", key, defaultValue);
    }

    private static ConfigEntry<T> RegisterConfigItem<T>(ConfigFile configFile, string sectionPostfix, string key,
        T defaultValue)
    {
        return RegisterConfig(configFile, $"Item - {sectionPostfix}", key, defaultValue);
    }

    private static ConfigEntry<T> RegisterConfigItemGun<T>(ConfigFile configFile, string key, T defaultValue)
    {
        return RegisterConfigItem(configFile, "Gun", key, defaultValue);
    }
    
    private static ConfigEntry<T> RegisterConfigUi<T>(ConfigFile configFile, string key, T defaultValue)
    {
        return RegisterConfig(configFile, "UI", key, defaultValue);
    }

    private static string SectionToLocalePrefix(string section)
    {
        return section.ToLower().Replace(" - ", ".");
    }

    private static ConfigEntry<T> RegisterConfig<T>(ConfigFile configFile, string section, string key, T defaultValue)
    {
        var sectionPrefix = SectionToLocalePrefix(section);
        return MossLib.Tool.Config.Register(configFile, section, key, defaultValue,
            _ => Locale($"config.{sectionPrefix}.{key}.description"), Registry);
    }

    private static string Locale(string key)
    {
        return ModLocale.GetFormat(key);
    }
}

internal static class StringExtensions
{
    public static string ToSnakeCase(this string str)
    {
        return string.IsNullOrEmpty(str)
            ? str
            :
            // 在非首字母的大写字母前插入下划线，再将所有字母转为小写
            Regex.Replace(str, "(?<=[a-z0-9])([A-Z])", "_$1").ToLower();
    }
}