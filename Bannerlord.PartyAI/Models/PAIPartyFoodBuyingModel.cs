using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace PartyAIControls.Models
{
  internal class PAIPartyFoodBuyingModel : PartyFoodBuyingModel
  {
    private readonly PartyFoodBuyingModel _previousModel;

    public PAIPartyFoodBuyingModel(PartyFoodBuyingModel previousModel)
    {
      _previousModel = previousModel;
      _previousModel ??= new DefaultPartyFoodBuyingModel();
    }

    public override float MinimumDaysFoodToLastWhileBuyingFoodFromTown => _previousModel.MinimumDaysFoodToLastWhileBuyingFoodFromTown > 40 ? _previousModel.MinimumDaysFoodToLastWhileBuyingFoodFromTown : 40;

    public override float MinimumDaysFoodToLastWhileBuyingFoodFromVillage => _previousModel.MinimumDaysFoodToLastWhileBuyingFoodFromVillage > 15 ? _previousModel.MinimumDaysFoodToLastWhileBuyingFoodFromVillage : 15;

    public override float LowCostFoodPriceAverage => _previousModel.LowCostFoodPriceAverage;

    public override void FindItemToBuy(MobileParty mobileParty, Settlement settlement, out ItemRosterElement itemRosterElement, out float itemElementsPrice)
    {
      _previousModel.FindItemToBuy(mobileParty, settlement, out itemRosterElement, out itemElementsPrice);
    }
  }
}
