using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace PartyAIControls.Models
{
  internal class PAISettlementGarrisonModel : SettlementGarrisonModel
  {
    readonly SettlementGarrisonModel _previousModel;

    public PAISettlementGarrisonModel(SettlementGarrisonModel previousModel)
    {
      _previousModel = previousModel;
      _previousModel ??= new DefaultSettlementGarrisonModel();
    }

    public override ExplainedNumber CalculateBaseGarrisonChange(Settlement settlement, bool includeDescriptions = false)
    {
      return _previousModel.CalculateBaseGarrisonChange(settlement, includeDescriptions);
    }

    public override int GetMaximumDailyAutoRecruitmentCount(Town town)
    {
      return _previousModel.GetMaximumDailyAutoRecruitmentCount(town);
    }

    public override float GetMaximumDailyRepairAmount(Settlement settlement)
    {
      return _previousModel.GetMaximumDailyRepairAmount(settlement);
    }

    public override int FindNumberOfTroopsToLeaveToGarrison(MobileParty mobileParty, Settlement settlement)
    {
      int result = _previousModel.FindNumberOfTroopsToLeaveToGarrison(mobileParty, settlement);

      if (!SubModule.PartySettingsManager.IsHeroManageable(mobileParty.LeaderHero))
      {
        return result;
      }

      PartyAIClanPartySettings heroSettings = SubModule.PartySettingsManager.Settings(mobileParty.LeaderHero);

      if (!heroSettings.AllowDonateTroops)
      {
        result = 0;
      }

      return result;
    }

    public override int FindNumberOfTroopsToTakeFromGarrison(MobileParty mobileParty, Settlement settlement, float idealGarrisonStrengthPerWalledCenter = 0)
    {
      int result = _previousModel.FindNumberOfTroopsToTakeFromGarrison(mobileParty, settlement, idealGarrisonStrengthPerWalledCenter);

      if (!SubModule.PartySettingsManager.IsHeroManageable(mobileParty.LeaderHero))
      {
        return result;
      }

      PartyAIClanPartySettings heroSettings = SubModule.PartySettingsManager.Settings(mobileParty.LeaderHero);

      if (!heroSettings.AllowTakeTroopsFromSettlement)
      {
        result = 0;
      }

      if (!heroSettings.AllowRecruitment)
      {
        result = 0;
      }

      return result;
    }
  }
}
