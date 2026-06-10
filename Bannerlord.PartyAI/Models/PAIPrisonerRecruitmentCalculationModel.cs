using Bannerlord.PartyAI.Domain;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.PartyAI.Models;

internal class PAIPrisonerRecruitmentCalculationModel : PrisonerRecruitmentCalculationModel
{
    public override int CalculateRecruitableNumber(PartyBase party, CharacterObject character)
    {
        return BaseModel.CalculateRecruitableNumber(party, character);
    }

    public override ExplainedNumber GetConformityChangePerHour(PartyBase party, CharacterObject character)
    {
        return BaseModel.GetConformityChangePerHour(party, character);
    }

    public override int GetConformityNeededToRecruitPrisoner(CharacterObject character)
    {
        return BaseModel.GetConformityNeededToRecruitPrisoner(character);
    }

    public override int GetPrisonerRecruitmentMoraleEffect(PartyBase party, CharacterObject character, int num)
    {
        return BaseModel.GetPrisonerRecruitmentMoraleEffect(party, character, num);
    }

    public override bool IsPrisonerRecruitable(PartyBase party, CharacterObject character, out int conformityNeeded)
    {
        bool result = BaseModel.IsPrisonerRecruitable(party, character, out conformityNeeded);

        if (!SubModule.PartySettingsManager.IsHeroManageable(party.LeaderHero))
        {
            return result;
        }

        PartyAIClanPartySettings heroSettings = SubModule.PartySettingsManager.Settings(party.LeaderHero);
        PartyComposition comp = Recruitment.GetPartyComposition(party, heroSettings);

        if (!heroSettings.AllowRecruitment)
        {
            return false;
        }

        // the party template will cause the troop to be converted into something useful anyway
        if (SubModule.PartySettingsManager.AllowTroopConversion && heroSettings.PartyTemplate != null)
        {
            return result;
        }

        if (!Recruitment.ShouldRecruit(comp, heroSettings, character, party))
        {
            result = false;
        }

        return result;
    }

    public override bool ShouldPartyRecruitPrisoners(PartyBase party)
    {
        return BaseModel.ShouldPartyRecruitPrisoners(party);
    }
}
