using System;
using System.Linq;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace Bannerlord.PartyAI.Domain;

public static class PartyHorseTrading
{
    private const int SaleThreshold = 10;

    public static bool TryBuyAndSellHorses(MobileParty mobileParty, Settlement settlement)
    {
        if (mobileParty?.LeaderHero is null || settlement is null)
        {
            return false;
        }

        PartyAIClanPartySettings settings = SubModule.PartySettingsManager.Settings(mobileParty.LeaderHero);
        if (!CanTradeHorses(mobileParty, settlement, settings))
        {
            return false;
        }

        int mountOverhead = mobileParty.Party.NumberOfMounts - mobileParty.Party.NumberOfMenWithoutHorse;
        if (mountOverhead > SaleThreshold)
        {
            SellHorses(mobileParty, settlement, mountOverhead);
        }
        else if (mountOverhead < 0)
        {
            BuyHorses(mobileParty, settlement, settings);
        }

        return true;
    }

    private static void SellHorses(MobileParty mobileParty, Settlement settlement, int totalAmountToSell)
    {
        var horses = mobileParty.ItemRoster
            .Where(element => element.EquipmentElement.Item.IsMountable)
            .OrderByDescending(element => GetBuyPrice(element, mobileParty, settlement));

        var amountToSell = totalAmountToSell;
        foreach (var horse in horses)
        {
            int count = Math.Min(amountToSell, horse.Amount);
            SellItemsAction.Apply(mobileParty.Party, settlement.Party, horse, count, settlement);
            amountToSell -= count;

            if (amountToSell <= 0)
            {
                break;
            }
        }
    }

    private static void BuyHorses(MobileParty mobileParty, Settlement settlement, PartyAIClanPartySettings settings)
    {
        var horses = settlement.Party.ItemRoster
            .Where(IsHorseToBuy)
            .OrderBy(element => GetBuyPrice(element, mobileParty, settlement));

        var totalCosts = 0;
        var oldGold = mobileParty.LeaderHero.Gold;
        var budget = Math.Min(settings.BuyHorsesBudgetToday, oldGold);

        foreach (var element in horses)
        {
            var price = GetBuyPrice(element, mobileParty, settlement); // We've got the buy price already, could cache it
            int amount = element.Amount;
            var projectedPrice = amount * price * 1.05f;
            if (projectedPrice > budget)
            {
                amount = MathF.Floor((float)budget / price);
            }

            int mountDeficit = mobileParty.Party.NumberOfMenWithoutHorse
                - mobileParty.Party.NumberOfMounts;

            amount = Math.Min(amount, mountDeficit);

            if (amount <= 0)
            {
                break;
            }

            SellItemsAction.Apply(settlement.Party, mobileParty.Party, element, amount, settlement);

            var newGold = mobileParty.LeaderHero.Gold;
            var actualPrice = oldGold - newGold;
            oldGold = newGold;

            totalCosts += actualPrice;
            budget -= totalCosts;
        }

        settings.DeductHorseBudget(totalCosts);
    }

    private static bool CanTradeHorses(
        MobileParty mobileParty,
        Settlement settlement,
        PartyAIClanPartySettings settings)
    {
        return mobileParty != MobileParty.MainParty
            && !mobileParty.IsDisbanding
            && (settlement.IsTown || settlement.IsVillage)
            && SubModule.PartySettingsManager.IsHeroManageable(mobileParty.LeaderHero)
            && settings.BuyHorses;
    }

    private static bool IsHorseToBuy(ItemRosterElement element)
    {
        var equipment = element.EquipmentElement;

        return equipment.Item.HasHorseComponent
            && equipment.Item.HorseComponent.IsMount
            && equipment.ItemModifier is null;
    }

    private static int GetBuyPrice(ItemRosterElement element, MobileParty mobileParty, Settlement settlement)
    {
        var equipment = element.EquipmentElement;

        var itemPrice = settlement.IsTown
            ? settlement.Town.GetItemPrice(equipment, mobileParty)
            : settlement.Village.GetItemPrice(equipment, mobileParty);

        return itemPrice;
    }
}
