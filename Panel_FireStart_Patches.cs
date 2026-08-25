using HarmonyLib;
using Il2Cpp;

namespace QoLFireStart;

internal class Panel_FireStart_Patches
{
    [HarmonyPatch(typeof(Panel_FireStart), "Enable")]
    internal class Panel_FireStart_Update
    {
        private static void Postfix(
            Panel_FireStart __instance, bool enable)
        {
            Main.FireStartManager.DoSelect(__instance, enable);
        }
    }
}