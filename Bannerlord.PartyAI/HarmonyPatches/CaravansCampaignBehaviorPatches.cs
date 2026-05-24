using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace PartyAIControls.HarmonyPatches
{
  [HarmonyPatch]
  internal class CaravansCampaignBehaviorPatches
  {
    [HarmonyPostfix]
    [HarmonyPatch(typeof(CaravansCampaignBehavior), "GetTradeScoreForTown")]
    private static void Postfix(ref float __result, MobileParty caravanParty, Town town, CampaignTime lastHomeVisitTimeOfCaravan, float caravanFullness, bool distanceCut)
    {
      if (!SubModule.PartySettingsManager.IsCaravanManageable(caravanParty.LeaderHero)) { return; }

      PartyAIClanPartySettings settings = SubModule.PartySettingsManager.Settings(caravanParty.LeaderHero);
      if (!settings.FilterSettlements || settings.FilteredSettlements?.Count < 2)
      {
        return;
      }

      if (!(settings.FilteredSettlements?.Contains(town?.Settlement) ?? false))
      {
        __result = -1f;
      }
    }
  }
}
