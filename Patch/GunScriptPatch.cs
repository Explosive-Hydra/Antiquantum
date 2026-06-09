using HarmonyLib;

namespace Quantum.Patch;

[HarmonyPatch(typeof(GunScript))]
public static class GunScriptPatch
{
    internal static bool HasOne;

    [HarmonyPatch("Update")]
    [HarmonyPrefix]
    private static void UpdatePrefix(GunScript __instance)
    {
        HasOne = __instance.roundInChamber == GunScript.RoundInChamber.Round;

        if (Plugin.AutoRack.Value
            && __instance.roundInChamber
                is GunScript.RoundInChamber.Casing or GunScript.RoundInChamber.None
            && __instance.roundsInMag > 0)
        {
            __instance.roundsInMag--;
            __instance.roundInChamber = GunScript.RoundInChamber.Round;
            __instance.racked = false;
        }

        if (Plugin.InfiniteAmmunition.Value) __instance.roundsInMag = __instance.magCapacity;
        __instance.knockBack = Plugin.Recoilless.Value 
            ? 0 
            : 8;
        if (Plugin.IndestructibleGun.Value) __instance.conditionLossPerShot = 0;
        if (!Plugin.AmmunitionUi.Value) PlayerCameraPatch.DestroyAmmunitionUi();
    }

    [HarmonyPatch("JamChance")]
    [HarmonyPostfix]
    private static void JamChancePostfix(ref float __result)
    {
        if (!Plugin.NeverJam.Value) return;
        __result = 0;
    }
}