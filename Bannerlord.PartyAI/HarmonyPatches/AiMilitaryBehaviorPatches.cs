using HarmonyLib;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;

namespace PartyAIControls.HarmonyPatches
{
  [HarmonyPatch(typeof(AiMilitaryBehavior), "AiHourlyTick")]
  internal class AiMilitaryBehaviorPatches
  {
    private static void Postfix(MobileParty mobileParty, PartyThinkParams p)
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

      // prevent raiding and sieging solo
      for (int i = 0; i < p.AIBehaviorScores.Count; i++)
      {
        (AIBehaviorData, float) score = p.AIBehaviorScores[i];
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
        p.SetBehaviorScore(o, 0f);
      }
    }
  }
}
