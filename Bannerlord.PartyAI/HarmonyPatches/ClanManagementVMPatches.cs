using HarmonyLib;
using PartyAIControls.UIExtenderPatches;
using SandBox.GauntletUI;
using TaleWorlds.CampaignSystem.GameState;

namespace PartyAIControls.HarmonyPatches
{
  internal class ClanManagementVMPatches
  {
    [HarmonyPatch(typeof(GauntletClanScreen), "OnActivate")]
    internal class OnActivate
    {
      private static void Prefix(GauntletClanScreen __instance)
      {
        if (ClanPartyItemVMMixin.SelectedParty != null)
        {
          ClanState state = (ClanState)AccessTools.Field(typeof(GauntletClanScreen), "_clanState").GetValue(__instance);
          AccessTools.Property(state.GetType(), "InitialSelectedParty").SetValue(state, ClanPartyItemVMMixin.SelectedParty);
          ClanPartyItemVMMixin.SelectedParty = null;
        }
      }
    }
  }
}
