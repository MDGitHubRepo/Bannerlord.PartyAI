using Bannerlord.PartyAI;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;

namespace Bannerlord.PartyAI.HarmonyPatches
{
    // this prevents disbanding for not having enough AI objectives which can be caused by the orders
    [HarmonyPatch(typeof(DisbandArmyAction), "ApplyByUnknownReason")]
    internal class DisbandArmyActionPatches
    {
        private static bool Prefix(Army army)
        {
            if (SubModule.PartySettingsManager.HasActiveOrder(army?.LeaderParty?.LeaderHero))
            {
                return false;
            }

            return true;
        }
    }
}
