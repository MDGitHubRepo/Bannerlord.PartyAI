using Bannerlord.PartyAI.Domain.Models;
using Bannerlord.PartyAI.Models;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.PartyAI.CampaignBehaviors.AiBehaviors;

internal class BesiegeSettlementBehavior : PartyOrderBehaviorBase
{
    protected override PartyAiOrderType OrderType => PartyAiOrderType.BesiegeSettlement;

    public override void RegisterEvents()
    {
        CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, OnAiHourlyTick);
        CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
    }

    private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
    {
        foreach (PartyAiEntitySettings settings in SubModule.PartySettingsManager.HeroesWithOrders)
        {
            var order = settings.Order;

            if (order is null || order.Behavior != OrderType || order.Target != settlement)
            {
                continue;
            }

            if (!FactionManager.IsAtWarAgainstFaction(settings.Hero.MapFaction, settlement.MapFaction))
            {
                Message.OrderStoppedTargetFriendly(settings.Hero.PartyBelongedTo, order);
                settings.ClearOrder();
            }
        }
    }

    private void OnAiHourlyTick(MobileParty party, PartyThinkParams thinkParams)
    {
        if (!IsPartyOrderRelevant(party, out var settings, out var order))
        {
            return;
        }

        var target = order.Target as Settlement;

        if (target is null)
        {
            Message.OrderStoppedTargetInvalid(party, order);
            settings.ClearOrder();
            return;
        }

        if (!FactionManager.IsAtWarAgainstFaction(party.MapFaction, target.MapFaction))
        {
            Message.OrderStoppedTargetFriendly(party, order);
            settings.ClearOrder();
            return;
        }

        AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(
            party,
            target,
            isTargetingPort: false, // TODO: Implement a separate blockade order
            out var navigationType,
            out var bestDistance,
            out var isFromPort);

        if (navigationType == MobileParty.NavigationType.None)
        {
            Message.OrderStoppedTargetUnreachable(party, order);
            settings.ClearOrder();
            return;
        }

        party.Ai.SetInitiative(0, 1, 6);

        AIBehaviorData aibehaviorData = new AIBehaviorData(
            target,
            AiBehavior.BesiegeSettlement,
            navigationType,
            willGatherArmy: false,
            isFromPort: false,
            isTargetingPort: false);
        AddBehaviorScore(aibehaviorData, Constants.BehaviorScore, thinkParams);
    }
}