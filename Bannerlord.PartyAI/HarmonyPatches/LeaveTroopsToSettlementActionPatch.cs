using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace PartyAIControls.HarmonyPatches
{
    [HarmonyPatch(typeof(GarrisonTroopsCampaignBehavior))]
    internal static class LeaveTroopsToSettlementActionPatch
    {
        // =========================
        // BLOCK DONATING TO GARRISON
        // =========================

        [HarmonyPatch("LeaveTroopsToGarrison")]
        [HarmonyPrefix]
        private static bool Prefix_LeaveTroopsToGarrison(
            MobileParty mobileParty,
            Settlement settlement,
            int numberOfTroopsToLeave,
            bool archersAreHighPriority)
        {
            // No hero → vanilla
            if (mobileParty?.LeaderHero == null)
                return true;

            var settings = SubModule.PartySettingsManager.Settings(mobileParty.LeaderHero);
            if (settings == null)
                return true;

            // If player has disabled donating troops for this hero, skip the method entirely
            return settings.AllowDonateTroops;
        }

        // =========================
        // BLOCK TAKING FROM GARRISON
        // =========================

        [HarmonyPatch("TakeTroopsFromGarrison")]
        [HarmonyPrefix]
        private static bool Prefix_TakeTroopsFromGarrison(
            MobileParty mobileParty,
            Settlement settlement,
            int numberOfTroopsToTake,
            bool archersAreHighPriority)
        {
            // No hero → vanilla
            if (mobileParty?.LeaderHero == null)
                return true;

            var settings = SubModule.PartySettingsManager.Settings(mobileParty.LeaderHero);
            if (settings == null)
                return true;

            // If player has disabled taking troops for this hero, skip the method
            return settings.AllowTakeTroopsFromSettlement;
        }
    }
}