using HarmonyLib;
using Helpers;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace PartyAIControls.CampaignBehaviors
{
  internal class PartyAIFoodBuyer : CampaignBehaviorBase
  {
    private PartiesBuyFoodCampaignBehavior _foodCampaignBehavior = null;
    private readonly MethodInfo CalculateFoodCountToBuyMethod = AccessTools.Method(typeof(PartiesBuyFoodCampaignBehavior), "CalculateFoodCountToBuy");

    public override void RegisterEvents()
    {
      CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
      CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, HourlyTickParty);
    }

    public void HourlyTickParty(MobileParty mobileParty)
    {
      if (!SubModule.DetatchmentManager.IsDetatchment(mobileParty)) { return; }
      Settlement currentSettlementOfMobilePartyForAICalculation = MobilePartyHelper.GetCurrentSettlementOfMobilePartyForAICalculation(mobileParty);
      if (currentSettlementOfMobilePartyForAICalculation != null)
      {
        TryBuyingFood(mobileParty, currentSettlementOfMobilePartyForAICalculation);
      }
    }

    public void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
    {
      if (!SubModule.DetatchmentManager.IsDetatchment(mobileParty)) { return; }
      TryBuyingFood(mobileParty, settlement);
    }

    private void TryBuyingFood(MobileParty mobileParty, Settlement settlement)
    {
      if (Campaign.Current.GameStarted && (settlement.IsTown || settlement.IsVillage) && Campaign.Current.Models.MobilePartyFoodConsumptionModel.DoesPartyConsumeFood(mobileParty) && (settlement.IsVillage || (mobileParty.MapFaction != null && !mobileParty.MapFaction.IsAtWarWith(settlement.MapFaction))) && settlement.ItemRoster.TotalFood > 0)
      {
        PartyFoodBuyingModel partyFoodBuyingModel = Campaign.Current.Models.PartyFoodBuyingModel;
        float minDays = settlement.IsVillage ? partyFoodBuyingModel.MinimumDaysFoodToLastWhileBuyingFoodFromVillage : partyFoodBuyingModel.MinimumDaysFoodToLastWhileBuyingFoodFromTown;
        _foodCampaignBehavior ??= Campaign.Current.GetCampaignBehavior<PartiesBuyFoodCampaignBehavior>();
        int foodCount = (int)CalculateFoodCountToBuyMethod.Invoke(_foodCampaignBehavior, new object[] { mobileParty, minDays });
        BuyFoodInternal(mobileParty, settlement, foodCount);
      }
    }

    private void BuyFoodInternal(MobileParty mobileParty, Settlement settlement, int numberOfFoodItemsNeededToBuy)
    {
      if (mobileParty.IsMainParty)
      {
        return;
      }
      for (int i = 0; i < numberOfFoodItemsNeededToBuy; i++)
      {
        FindItemToBuy(mobileParty, settlement, out var itemRosterElement, out var itemElementsPrice);
        if (itemRosterElement.EquipmentElement.Item != null)
        {
          if (itemElementsPrice <= mobileParty.Owner.Gold)
          {
            SellItemsAction.Apply(settlement.Party, mobileParty.Party, itemRosterElement, 1);
          }
          if (itemRosterElement.EquipmentElement.Item.HasHorseComponent && itemRosterElement.EquipmentElement.Item.HorseComponent.IsLiveStock)
          {
            i += itemRosterElement.EquipmentElement.Item.HorseComponent.MeatCount - 1;
          }
          continue;
        }
        break;
      }
    }

    public void FindItemToBuy(MobileParty mobileParty, Settlement settlement, out ItemRosterElement itemElement, out float itemElementsPrice)
    {
      itemElement = ItemRosterElement.Invalid;
      itemElementsPrice = 0f;
      float num = 0f;
      SettlementComponent settlementComponent = settlement.SettlementComponent;
      int num2 = -1;
      for (int i = 0; i < settlement.ItemRoster.Count; i++)
      {
        ItemRosterElement elementCopyAtIndex = settlement.ItemRoster.GetElementCopyAtIndex(i);
        if (elementCopyAtIndex.Amount <= 0)
        {
          continue;
        }
        bool flag = elementCopyAtIndex.EquipmentElement.Item.HasHorseComponent && elementCopyAtIndex.EquipmentElement.Item.HorseComponent.IsLiveStock;
        if (!(elementCopyAtIndex.EquipmentElement.Item.IsFood || flag))
        {
          continue;
        }
        int itemPrice = settlementComponent.GetItemPrice(elementCopyAtIndex.EquipmentElement, mobileParty);
        int itemValue = elementCopyAtIndex.EquipmentElement.ItemValue;
        if (!(itemPrice < 120 || flag) || mobileParty.Owner.Gold < itemPrice)
        {
          continue;
        }
        float num3 = (flag ? ((120f - itemPrice / elementCopyAtIndex.EquipmentElement.Item.HorseComponent.MeatCount) * 0.0083f) : ((120 - itemPrice) * 0.0083f));
        float num4 = (flag ? ((100f - itemValue / elementCopyAtIndex.EquipmentElement.Item.HorseComponent.MeatCount) * 0.01f) : ((100 - itemValue) * 0.01f));
        float num5 = num3 * num3 * num4 * num4;
        if (num5 > 0f)
        {
          if (MBRandom.RandomFloat * (num + num5) >= num)
          {
            num2 = i;
            itemElementsPrice = itemPrice;
          }
          num += num5;
        }
      }
      if (num2 != -1)
      {
        itemElement = settlement.ItemRoster.GetElementCopyAtIndex(num2);
      }
    }

    public override void SyncData(IDataStore dataStore)
    {
    }
  }
}
