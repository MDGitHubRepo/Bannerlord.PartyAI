using Bannerlord.PartyAI.Domain;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using static TaleWorlds.CampaignSystem.CampaignBehaviors.RecruitmentCampaignBehavior;

namespace Bannerlord.PartyAI.Patches;

internal class RecruitmentCampaignBehaviorPatches
{
    public static void Apply(Harmony harmony)
    {
        harmony.Patch<RecruitmentCampaignBehavior>()
            .Method("ApplyInternal")
                .Prefix(ApplyInternalPrefix);
    }

    private static bool ApplyInternalPrefix(MobileParty side1Party, Settlement settlement, Hero individual, CharacterObject troop, int number, int bitCode, RecruitingDetail detail)
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

        PartyCompositionObect comp = Recruitment.GetPartyComposition(side1Party.Party, heroSettings);
        if (!Recruitment.ShouldRecruit(comp, heroSettings, troop, side1Party.Party))
        {
            return false;
        }

        return true;
    }
}
