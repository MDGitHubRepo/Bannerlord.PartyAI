using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.PartyAI.CampaignBehaviors.AiBehaviors;

public class VisitSettlementBehavior : PartyAiBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, OnAiHourlyTick);
    }

    public void HandleVisitSettlement(
        MobileParty party,
        Settlement targetSettlement,
        PartyAIClanPartySettings settings,
        PAICustomOrder order,
        PartyThinkParams thinkParams)
    {
        if (!ShouldContinueExecutingOrder(party, order))
        {
            settings.ClearOrder();
            return;
        }

        if (targetSettlement.IsUnderSiege)
        {
            Message.OrderStoppedTargetSieged(party, order);
            settings.ClearOrder();
            return;
        }

        var isTargetingPort = targetSettlement.HasPort && party.IsCurrentlyAtSea;
        AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(
            party,
            targetSettlement,
            isTargetingPort,
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

        var behaviorData = new AIBehaviorData(
            targetSettlement,
            AiBehavior.GoToSettlement,
            navigationType,
            false,
            isFromPort,
            isTargetingPort);

        AddBehaviorScore(behaviorData, 5f, thinkParams);
    }

    private void OnAiHourlyTick(MobileParty party, PartyThinkParams thinkParams)
    {
        if (!IsPartyOrderRelevant(party, PAICustomOrder.OrderType.VisitSettlement, out var settings, out var order))
        {
            return;
        }

        var targetSettlement = order.Target as Settlement;
        if (targetSettlement is null)
        {
            Message.OrderStoppedTargetInvalid(party, order);
            settings.ClearOrder();
            return;
        }

        if (party.CurrentSettlement == targetSettlement)
        {
            settings.ClearOrder();
            return;
        }

        HandleVisitSettlement(party, targetSettlement, settings, order, thinkParams);
    }

    private bool ShouldContinueExecutingOrder(
        MobileParty party,
        PAICustomOrder order)
    {
        var target = order.Target as Settlement;

        var canContinue = true;
        if (target is null)
        {
            Message.OrderStoppedTargetInvalid(party, order);
            canContinue = false;
        }
        else if (FactionManager.IsAtWarAgainstFaction(party.MapFaction, target.MapFaction))
        {
            Message.OrderStoppedTargetEnemy(party, order);
            canContinue = false;
        }

        return canContinue;
    }
}
