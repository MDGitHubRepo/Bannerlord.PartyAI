using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace Bannerlord.PartyAI.Models;

internal class PAIPartyFoodBuyingModel : PartyFoodBuyingModel
{
    public override float MinimumDaysFoodToLastWhileBuyingFoodFromTown => BaseModel.MinimumDaysFoodToLastWhileBuyingFoodFromTown > 40 ? BaseModel.MinimumDaysFoodToLastWhileBuyingFoodFromTown : 40;

    public override float MinimumDaysFoodToLastWhileBuyingFoodFromVillage => BaseModel.MinimumDaysFoodToLastWhileBuyingFoodFromVillage > 15 ? BaseModel.MinimumDaysFoodToLastWhileBuyingFoodFromVillage : 15;

    public override float LowCostFoodPriceAverage => BaseModel.LowCostFoodPriceAverage;

    public override void FindItemToBuy(MobileParty mobileParty, Settlement settlement, out ItemRosterElement itemRosterElement, out float itemElementsPrice)
    {
        BaseModel.FindItemToBuy(mobileParty, settlement, out itemRosterElement, out itemElementsPrice);
    }
}
