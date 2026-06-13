using HarmonyLib;
using UnityEngine;

namespace Quantum.Patch;

[HarmonyPatch(typeof(ConsoleScript))]
public class ConsoleScriptPatch
{
    private static bool _imeOff;

    [HarmonyPatch("Update")]
    [HarmonyPostfix]
    private static void OnUpdatePostfix(ConsoleScript __instance)
    {
        if (!__instance.active)
            return;

        if (__instance.input == null)
            return;

        if (!Input.GetKeyDown(KeyCode.KeypadEnter))
            return;

        var inputText = __instance.input.text;
        if (string.IsNullOrEmpty(inputText))
            return;

        __instance.ExecuteCommand(inputText);
    }
}
