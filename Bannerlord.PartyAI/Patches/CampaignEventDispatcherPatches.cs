using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.PartyAI.Patches;

internal class CampaignEventDispatcherPatches
{
    public static void Apply(Harmony harmony)
    {
        harmony.Patch<CampaignEventDispatcher>()
            .Method("AiHourlyTick")
                .Postfix(AiHourlyTickPostfix);
    }

    private static void AiHourlyTickPostfix(MobileParty party, ref PartyThinkParams partyThinkParams)
    {
        SubModule.PartyThinker.ProcessOrder(party, partyThinkParams);
    }
}
