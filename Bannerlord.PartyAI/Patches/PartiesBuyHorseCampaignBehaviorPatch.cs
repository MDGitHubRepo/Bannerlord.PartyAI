using Bannerlord.PartyAI.Domain;
using HarmonyLib;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.PartyAI.Patches;

internal class PartiesBuyHorseCampaignBehaviorPatch
{
    public static void Apply(Harmony harmony)
    {
        harmony.Patch<PartiesBuyHorseCampaignBehavior>()
            .Method(x => x.OnSettlementEntered(default, default, default))
                .Prefix(OnSettlementEnteredPrefix);
    }

    private static bool OnSettlementEnteredPrefix(MobileParty mobileParty, Settlement settlement)
    {
        var tradedHorses = PartyHorseTrading.TryBuyAndSellHorses(mobileParty, settlement);

        return !tradedHorses;
    }
}
