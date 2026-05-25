using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.PartyAI.HarmonyPatches
{
    internal class CaravansCampaignBehaviorPatches
    {
        public static void Apply(Harmony harmony)
        {
            harmony.Patch<CaravansCampaignBehavior>()
                .Method("GetTradeScoreForTown")
                    .Postfix(GetTradeScoreForTownPostfix);
        }

        private static void GetTradeScoreForTownPostfix(ref float __result, MobileParty caravanParty, Town town, CampaignTime lastHomeVisitTimeOfCaravan, float caravanFullness, bool distanceCut)
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
