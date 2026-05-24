using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using static TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior;

namespace PartyAIControls.HarmonyPatches
{
  internal class RecruitmentCampaignBehaviorPatches
  {
    [HarmonyPatch(typeof(RecruitmentCampaignBehavior), "ApplyInternal")]
    internal class ApplyInternal
    {
      private static bool Prefix(MobileParty side1Party, Settlement settlement, Hero individual, CharacterObject troop, int number, int bitCode, RecruitingDetail detail)
      {
        if (!SubModule.PartySettingsManager.IsManageable(side1Party.LeaderHero))
        {
          return true;
        }

        PartyAIClanPartySettings heroSettings = SubModule.PartySettingsManager.Settings(side1Party.LeaderHero);

        if (!heroSettings.AllowRecruitment)
        {
          return false;
        }

        // if we're going to convert the troop anyway, it doesn't matter
        if (SubModule.PartySettingsManager.AllowTroopConversion && heroSettings.PartyTemplate != null)
        {
          return true;
        }

        PartyCompositionObect comp = SubModule.PartyTroopRecruiter.GetPartyComposition(side1Party.Party, heroSettings);
        if (!SubModule.PartyTroopRecruiter.ShouldRecruit(comp, heroSettings, troop, side1Party.Party))
        {
          return false;
        }

        return true;
      }
    }
  }
}
