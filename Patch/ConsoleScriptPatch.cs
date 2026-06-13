using HarmonyLib;
using System;
using System.Linq;
using UnityEngine;
using Quantum;

namespace Quantum.Patch;

[HarmonyPatch(typeof(ConsoleScript))]
public class ConsoleScriptPatch
{
    private static string[] _candidates = [];
    private static int _index;
    private static string _cmdName = "";
    private static int _paramIdx = -1;
    private static string _lastPartial = "";
    private static float _lastUpTime;
    private static float _lastDownTime;

    private static int MaxVisible => Plugin.MaxVisibleCandidates.Value;

    [HarmonyPatch(nameof(ConsoleScript.TryFinishCommandPart))]
    [HarmonyPrefix]
    private static bool BlockTryFinishCommandPart()
    {
        return _candidates.Length == 0;
    }

    [HarmonyPatch(nameof(ConsoleScript.GoToCommandHistory))]
    [HarmonyPrefix]
    private static bool BlockGoToCommandHistory()
    {
        return _candidates.Length == 0;
    }
    
    [HarmonyPatch(nameof(ConsoleScript.HandleDescriptionText))]
    [HarmonyPostfix]
    private static void PostHandleDescriptionText(ConsoleScript __instance)
    {
        if (_candidates.Length == 0 || __instance.descriptionText == null)
            return;

        var text = __instance.descriptionText.text;
        if (string.IsNullOrEmpty(text))
            return;

        text = text.Replace("<color=yellow>", "").Replace("</color>", "");

        var newlineIdx = text.IndexOf('\n');
        var header = newlineIdx >= 0
            ? text.Substring(0, newlineIdx)
            : text;

        var maxVisible = MaxVisible;
        int windowStart, windowEnd;
        if (_candidates.Length <= maxVisible)
        {
            windowStart = 0;
            windowEnd = _candidates.Length;
        }
        else
        {
            var half = maxVisible / 2;
            windowStart = _index - half;
            windowEnd = windowStart + maxVisible;

            if (windowStart < 0)
            {
                windowStart = 0;
                windowEnd = maxVisible;
            }
            else if (windowEnd > _candidates.Length)
            {
                windowEnd = _candidates.Length;
                windowStart = windowEnd - maxVisible;
            }
        }

        var sb = new System.Text.StringBuilder();
        sb.Append(header);
        sb.Append('\n');

        for (var i = windowStart; i < windowEnd; i++)
        {
            if (i == _index)
            {
                sb.Append("<b><color=yellow>");
                sb.Append(_candidates[i]);
                sb.Append("</color></b>\n");
            }
            else
            {
                sb.Append(_candidates[i]);
                sb.Append('\n');
            }
        }

        __instance.descriptionText.text = sb.ToString();
    }

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    private static void PostUpdate(ConsoleScript __instance)
    {
        if (!__instance.active || __instance.input == null)
            return;

        var text = __instance.input.text;
        var args = text.Split([' '], StringSplitOptions.None);

        if (Input.GetKeyDown(KeyCode.KeypadEnter) && !string.IsNullOrEmpty(text))
        {
            __instance.ExecuteCommand(text);
            return;
        }

        if (args.Length < 2 || string.IsNullOrEmpty(args[0]))
        {
            ClearState();
            return;
        }

        var cmd = ConsoleScript.SearchExact(args[0]);
        if (cmd?.argAutofill == null)
        {
            ClearState();
            return;
        }

        var paramIdx = args.Length - 2;
        if (!cmd.argAutofill.TryGetValue(paramIdx, out var fills))
        {
            ClearState();
            return;
        }

        var partial = args[args.Length - 1];
        var filteredFills = ConsoleScript.SearchArgumentAutofill(partial, fills);

        if (_cmdName != args[0] || _paramIdx != paramIdx)
        {
            _cmdName = args[0];
            _paramIdx = paramIdx;
            _candidates = filteredFills.ToArray();
            _lastPartial = partial;
            _index = 0;
        }
        else if (_lastPartial != partial)
        {
            _candidates = filteredFills.ToArray();
            _lastPartial = partial;
            ClampIndex();
        }

        if (_candidates.Length == 0)
            return;

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            DoReplace(__instance, args);
        }
        else if (Input.GetKey(KeyCode.UpArrow))
        {
            if (!(Time.unscaledTime - _lastUpTime > 0.1f)) return;
            _lastUpTime = Time.unscaledTime;
            _index = (_index - 1 + _candidates.Length) % _candidates.Length;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            if (!(Time.unscaledTime - _lastDownTime > 0.1f)) return;
            _lastDownTime = Time.unscaledTime;
            _index = (_index + 1) % _candidates.Length;
        }
    }

    private static void ClearState()
    {
        _candidates = [];
        _cmdName = "";
        _paramIdx = -1;
        _lastPartial = "";
        _index = 0;
        _lastUpTime = 0f;
        _lastDownTime = 0f;
    }

    private static void ClampIndex()
    {
        if (_candidates.Length == 0)
            _index = 0;
        else if (_index >= _candidates.Length)
            _index = _candidates.Length - 1;
    }

    private static void DoReplace(ConsoleScript instance, string[] args)
    {
        var prefix = string.Join(" ", args.Take(args.Length - 1));
        var replacement = _candidates[_index];
        instance.input.text = $"{prefix} {replacement}";
        instance.SetCaretToEnd();

        _lastPartial = replacement;
    }
}
