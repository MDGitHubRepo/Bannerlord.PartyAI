using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace PartyAIControls.HarmonyPatches
{
  [HarmonyPatch]
  internal class PartiesBuyHorseCampaignBehaviorPatch
  {

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PartiesBuyHorseCampaignBehavior), "OnSettlementEntered")]
    internal static bool Prefix(MobileParty mobileParty, Settlement settlement, Hero hero)
    {
      if (mobileParty?.LeaderHero == null || mobileParty == MobileParty.MainParty || mobileParty.IsDisbanding || settlement == null)
      {
        return true;
      }
      if (!settlement.IsTown && !settlement.IsVillage) { return true; }

      if (!SubModule.PartySettingsManager.IsHeroManageable(mobileParty.LeaderHero))
      {
        return true;
      }
      PartyAIClanPartySettings settings = SubModule.PartySettingsManager.Settings(mobileParty.LeaderHero);
      if (!settings.BuyHorses) { return true; }

      int toSell = mobileParty.Party.NumberOfMounts - mobileParty.Party.NumberOfMenWithoutHorse;

      if (toSell > 10)
      {
        for (int i = mobileParty.ItemRoster.Count - 1; i >= 0; i--)
        {
          ItemRosterElement item = mobileParty.ItemRoster[i];
          if (item.EquipmentElement.Item.IsMountable)
          {
            int count = Math.Min(toSell, item.Amount);
            SellItemsAction.Apply(mobileParty.Party, settlement.Party, item, count, settlement);
            toSell -= count;
            if (toSell <= 0) { break; }
          }
        }
      }
      else if (toSell < 0)
      {
        BuyHorses(mobileParty, settlement, settings.BuyHorsesBudgetToday, out int cost);
        settings.DeductHorseBudget(cost);
      }

      return false;
    }

    private static void BuyHorses(MobileParty mobileParty, Settlement settlement, int budget, out int cost)
    {
      cost = 0;
      if (!settlement.IsTown && !settlement.IsVillage) { return; }
      ItemRoster itemRoster = settlement.Party.ItemRoster;

      for (int i = 0; i < 2; i++)
      {
        int index = -1;
        int price = 100000;
        for (int j = 0; j < itemRoster.Count; j++)
        {
          EquipmentElement equipment = itemRoster.GetElementCopyAtIndex(j).EquipmentElement;
          if (equipment.Item.HasHorseComponent && equipment.Item.HorseComponent.IsMount && equipment.ItemModifier == null)
          {
            int itemPrice;
            if (settlement.IsTown)
            {
              itemPrice = settlement.Town.GetItemPrice(equipment, mobileParty);
            }
            else
            {
              itemPrice = settlement.Village.GetItemPrice(equipment, mobileParty);
            }

            if (itemPrice < price)
            {
              price = itemPrice;
              index = j;
            }
          }
        }
        if (index >= 0)
        {
          ItemRosterElement elementCopyAtIndex = itemRoster.GetElementCopyAtIndex(index);
          int amount = elementCopyAtIndex.Amount;
          if ((amount * price) > budget)
          {
            amount = MathF.Floor((float)budget / price);
          }
          int numberOfMounts = mobileParty.Party.NumberOfMounts;
          if (amount > mobileParty.Party.NumberOfMenWithoutHorse - numberOfMounts)
          {
            amount = mobileParty.Party.NumberOfMenWithoutHorse - numberOfMounts;
          }
          if (amount > 0)
          {
            SellItemsAction.Apply(settlement.Party, mobileParty.Party, elementCopyAtIndex, amount, settlement);
            cost = amount * price;
          }
        }
      }
    }
  }
}
