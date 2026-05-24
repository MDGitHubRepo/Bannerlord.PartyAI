using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace PartyAIControls.HarmonyPatches
{
  [HarmonyPatch(typeof(AiVisitSettlementBehavior), "GetApproximateVolunteersCanBeRecruitedDataFromSettlement")]
  internal class AiVisitSettlementBehaviorPatches
  {
    internal static void Postfix(ref (int, float) __result, Hero hero, Settlement settlement)
        {
      if (!SubModule.PartySettingsManager.IsHeroManageable(hero) || 
          hero.PartyBelongedTo == null ||
          hero.PartyBelongedTo.LeaderHero != hero)
      {
        return;
      }

      MobileParty mobileParty = hero.PartyBelongedTo;
      PartyAIClanPartySettings heroSettings = SubModule.PartySettingsManager.Settings(hero);

      if (!heroSettings.AllowRecruitment)
      {
        __result = (0, 0f);
        return;
      }

      // if we're going to convert the troop anyway, it doesn't matter
      if (SubModule.PartySettingsManager.AllowTroopConversion && heroSettings.PartyTemplate != null)
      {
        return;
      }

      PartyCompositionObect comp = SubModule.PartyTroopRecruiter.GetPartyComposition(mobileParty.Party, heroSettings);

      int allowedCount = 0;
      int totalWage = 0;

      int maxSlotsPerNotable = (hero.MapFaction != settlement.MapFaction) ? 2 : 4;

            foreach (Hero notable in settlement.Notables)
            {
                if (!notable.IsAlive)
                    continue;

                int maxIndex = Campaign.Current.Models.VolunteerModel.MaximumIndexHeroCanRecruitFromHero(
                  mobileParty.IsGarrison ? mobileParty.Party.Owner : mobileParty.LeaderHero,
                  notable
                );

                for (int i = 0; i <= maxIndex && i < notable.VolunteerTypes.Length; i++)
                {
                    CharacterObject troop = notable.VolunteerTypes[i];
                    if (troop == null)
                        continue;

                    if (SubModule.PartyTroopRecruiter.ShouldRecruit(comp, heroSettings, troop, mobileParty.Party))
                    {
                        allowedCount++;
                        totalWage += Campaign.Current.Models.PartyWageModel.GetCharacterWage(troop);
                    }
                }
            }

            if (allowedCount > 0)
            {
                float avgWage = (float)totalWage / allowedCount;
                __result = (allowedCount, avgWage);
            }
            else
            {
                __result = (0, 0f);
            }
        }
    }
}