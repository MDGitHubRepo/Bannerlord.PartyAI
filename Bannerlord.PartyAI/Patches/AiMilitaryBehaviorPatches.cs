using HarmonyLib;
using HarmonyLib.PatchBuilder;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.PartyAI.Patches;

internal class AiMilitaryBehaviorPatches
{
    public static void Apply(Harmony harmony)
    {
        harmony.Patch<AiMilitaryBehavior>()
            .Method("AiHourlyTick")
                .Postfix(AiHourlyTickPostfix);
    }

    private static void AiHourlyTickPostfix(MobileParty mobileParty, PartyThinkParams p)
    {
        PreventSoloRaidingAndSieging(mobileParty, p);
    }

    private static void PreventSoloRaidingAndSieging(MobileParty mobileParty, PartyThinkParams partyThinkParams)
    {
        if (!SubModule.PartySettingsManager.IsHeroManageable(mobileParty.LeaderHero))
        {
            return;
        }

        PartyAIClanPartySettings heroSettings = SubModule.PartySettingsManager.Settings(mobileParty.LeaderHero);

        if (heroSettings.AllowRaidVillages && heroSettings.AllowSieging)
        {
            return;
        }

        List<AIBehaviorData> overrides = new();

        for (int i = 0; i < partyThinkParams.AIBehaviorScores.Count; i++)
        {
            (AIBehaviorData, float) score = partyThinkParams.AIBehaviorScores[i];
            if (score.Item1.AiBehavior == AiBehavior.RaidSettlement && !heroSettings.AllowRaidVillages)
            {
                overrides.Add(score.Item1);
            }

            if (score.Item1.AiBehavior == AiBehavior.BesiegeSettlement && !heroSettings.AllowSieging)
            {
                overrides.Add(score.Item1);
            }
        }

        foreach (AIBehaviorData o in overrides)
        {
            partyThinkParams.SetBehaviorScore(o, 0f);
        }
    }
}
