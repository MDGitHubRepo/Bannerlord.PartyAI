using HarmonyLib;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Localization;

namespace PartyAIControls.HarmonyPatches
{
  [HarmonyPatch]
  internal class PartyVMPatches
  {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PartyVM), "TitleLbl", MethodType.Getter)]
    private static void TitleLbl(ref string __result, PartyVM __instance)
    {
      if (__result != __instance.HeaderLbl)
      {
        __result = __instance.HeaderLbl;
      }
    }

    // refreshes text on done button tooltip if it didn't auto update itself
    [HarmonyPostfix]
    [HarmonyPatch(typeof(PartyVM), "ExecuteDone")]
    private static void ExecuteDone(PartyVM __instance)
    {
      try
      {
        if (!__instance.PartyScreenLogic.IsDoneActive())
        {
          __instance.IsDoneDisabled = !__instance.PartyScreenLogic.IsDoneActive();
          __instance.DoneHint.HintText = new TextObject("{=!}" + __instance.PartyScreenLogic.DoneReasonString);
          __instance.OnPropertyChanged("DoneHint");
          __instance.OnPropertyChanged("IsDoneDisabled");
        }
      }
      catch
      {

      }
    }
  }
}
