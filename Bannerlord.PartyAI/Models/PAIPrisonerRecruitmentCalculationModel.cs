using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace PartyAIControls.Models
{
  internal class PAIPrisonerRecruitmentCalculationModel : PrisonerRecruitmentCalculationModel
  {
    readonly PrisonerRecruitmentCalculationModel _previousModel;

    public PAIPrisonerRecruitmentCalculationModel(PrisonerRecruitmentCalculationModel previousModel)
    {
      _previousModel = previousModel;
      _previousModel ??= new DefaultPrisonerRecruitmentCalculationModel();
    }

    public override int CalculateRecruitableNumber(PartyBase party, CharacterObject character)
    {
      return _previousModel.CalculateRecruitableNumber(party, character);
    }

    public override ExplainedNumber GetConformityChangePerHour(PartyBase party, CharacterObject character)
    {
      return _previousModel.GetConformityChangePerHour(party, character);
    }

    public override int GetConformityNeededToRecruitPrisoner(CharacterObject character)
    {
      return _previousModel.GetConformityNeededToRecruitPrisoner(character);
    }

    public override int GetPrisonerRecruitmentMoraleEffect(PartyBase party, CharacterObject character, int num)
    {
      return _previousModel.GetPrisonerRecruitmentMoraleEffect(party, character, num);
    }

    public override bool IsPrisonerRecruitable(PartyBase party, CharacterObject character, out int conformityNeeded)
    {
      bool result = _previousModel.IsPrisonerRecruitable(party, character, out conformityNeeded);

      if (!SubModule.PartySettingsManager.IsHeroManageable(party.LeaderHero))
      {
        return result;
      }

      PartyAIClanPartySettings heroSettings = SubModule.PartySettingsManager.Settings(party.LeaderHero);
      PartyCompositionObect comp = SubModule.PartyTroopRecruiter.GetPartyComposition(party, heroSettings);

      if (!heroSettings.AllowRecruitment)
      {
        return false;
      }

      // the party template will cause the troop to be converted into something useful anyway
      if (SubModule.PartySettingsManager.AllowTroopConversion && heroSettings.PartyTemplate != null)
      {
        return result;
      }

      if (!SubModule.PartyTroopRecruiter.ShouldRecruit(comp, heroSettings, character, party))
      {
        result = false;
      }

      return result;
    }

    public override bool ShouldPartyRecruitPrisoners(PartyBase party)
    {
      return _previousModel.ShouldPartyRecruitPrisoners(party);
    }
  }
}
