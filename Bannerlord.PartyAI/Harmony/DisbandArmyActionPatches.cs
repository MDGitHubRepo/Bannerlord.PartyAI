using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace Bannerlord.PartyAI.HarmonyPatches
{
    internal class DisbandArmyActionPatches
    {
        public static void Apply(Harmony harmony)
        {
            harmony.Patch()
                .Method(() => DisbandArmyAction.ApplyByUnknownReason(null))
                    .Prefix(ApplyByUnknownReasonPrefix);
        }

        private static bool ApplyByUnknownReasonPrefix(Army army)
        {
            // this prevents disbanding for not having enough AI objectives which can be caused by the orders
            if (SubModule.PartySettingsManager.HasActiveOrder(army?.LeaderParty?.LeaderHero))
            {
                return false;
            }

            return true;
        }
    }
}
