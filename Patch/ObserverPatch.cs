using HarmonyLib;

namespace Quantum.Patch;

[HarmonyPatch(typeof(Observer))]
public class ObserverPatch
{
    [HarmonyPatch("Update")]
    [HarmonyPrefix]
    public static bool UpdatePrefix()
    {
        if (!Plugin.NoObserver.Value) return true;

        Observer.main.gameObject.SetActive(false);
        return false;
    }
}