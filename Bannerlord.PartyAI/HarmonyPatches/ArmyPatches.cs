using HarmonyLib;
using System.Reflection;
using TaleWorlds.CampaignSystem;

namespace PartyAIControls.HarmonyPatches
{
  // this patches an issue where Army._hourlyTickEvent will be null for some reason and cause a crash on army dispersion

  [HarmonyPatch(typeof(Army), "DisperseInternal")]
  internal class ArmyPatches
  {
    private static readonly FieldInfo _hourlyTickEvent = AccessTools.Field(typeof(Army), "_hourlyTickEvent");
    private static readonly MethodInfo AddEventHandlers = AccessTools.Method(typeof(Army), "AddEventHandlers");
    private static void Prefix(Army __instance)
    {
      if (_hourlyTickEvent.GetValue(__instance) == null)
      {
        AddEventHandlers.Invoke(__instance, new object[] { });
      }
    }
  }
}
