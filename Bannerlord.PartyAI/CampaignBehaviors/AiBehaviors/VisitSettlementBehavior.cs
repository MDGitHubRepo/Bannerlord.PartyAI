using Helpers;
using System.Diagnostics.CodeAnalysis;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.PartyAI.CampaignBehaviors.AiBehaviors;

internal class VisitSettlementBehavior : PartyAiBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, OnAiHourlyTick);
    }

    private void OnAiHourlyTick(MobileParty party, PartyThinkParams thinkParams)
    {
        if (!IsPartyOrderRelevant(party, PAICustomOrder.OrderType.VisitSettlement, out var settings, out var order))
        {
            return;
        }

        if (!ShouldContinueExecutingOrder(party, order, out var targetSettlement))
        {
            settings.ClearOrder();
            return;
        }

        if (party.CurrentSettlement == targetSettlement)
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

    private bool ShouldContinueExecutingOrder(
        MobileParty party,
        PAICustomOrder order,
        [NotNullWhen(true)] out Settlement? targetSettlement)
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

        targetSettlement = target;
        return canContinue;
    }
}
