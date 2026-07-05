using Bannerlord.PartyAI.Domain.Models;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.PartyAI.CampaignBehaviors.AiBehaviors;

internal class PatrolAroundSettlementBehavior : PartyOrderBehaviorBase
{
    protected override PartyAiOrderType OrderType => PartyAiOrderType.PatrolAroundPoint;

    public override void RegisterEvents()
    {
        CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, OnAiHourlyTick);
    }

    private void OnAiHourlyTick(MobileParty party, PartyThinkParams thinkParams)
    {
        if (!IsPartyOrderRelevant(party, out var settings, out var order))
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

        if (!ShouldContinueExecutingOrder(party, order))
        {
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

        if (party.Objective != MobileParty.PartyObjective.Defensive)
        {
            settings.CachedPartyObjective = party.Objective;
            party.SetPartyObjective(MobileParty.PartyObjective.Defensive);
        }

        var behaviorData = new AIBehaviorData(
            targetSettlement,
            AiBehavior.PatrolAroundPoint,
            navigationType,
            false,
            isFromPort,
            isTargetingPort);

        AddBehaviorScore(behaviorData, Constants.BehaviorScore, thinkParams);
    }

    private bool ShouldContinueExecutingOrder(
        MobileParty party,
        PartyAiOrder order)
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
