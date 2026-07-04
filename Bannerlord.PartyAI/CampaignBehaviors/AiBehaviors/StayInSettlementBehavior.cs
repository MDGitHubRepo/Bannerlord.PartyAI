using Bannerlord.PartyAI.Domain.Models;
using Bannerlord.PartyAI.Models;
using System.Diagnostics.CodeAnalysis;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using static TaleWorlds.CampaignSystem.Party.MobileParty;

namespace Bannerlord.PartyAI.CampaignBehaviors.AiBehaviors;

public class StayInSettlementBehavior : PartyOrderBehaviorBase
{
    private readonly VisitSettlementBehavior _visitSettlementBehavior;

    public StayInSettlementBehavior(VisitSettlementBehavior visitSettlementBehavior)
    {
        _visitSettlementBehavior = visitSettlementBehavior;
    }

    protected override PartyAiOrderType OrderType => PartyAiOrderType.StayInSettlement;

    public override void RegisterEvents()
    {
        CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, OnAiHourlyTick);
    }

    public void HandleStayInSettlement(
        MobileParty party,
        PartyAiEntitySettings settings,
        PartyAiOrder order,
        PartyThinkParams thinkParams)
    {
        if (!ShouldContinueExecutingOrder(party, order, out var targetSettlement))
        {
            settings.ClearOrder();
            return;
        }

        if (party.CurrentSettlement != targetSettlement)
        {
            _visitSettlementBehavior.HandleVisitSettlement(
                party,
                targetSettlement,
                settings,
                order,
                thinkParams);
            return;
        }

        party.Ai.SetInitiative(0, 0, 6);

        var behaviorData = new AIBehaviorData(
            targetSettlement,
            AiBehavior.Hold,
            NavigationType.None,
            false,
            false,
            false);

        AddBehaviorScore(behaviorData, Constants.BehaviorScore, thinkParams);
    }

    private void OnAiHourlyTick(MobileParty party, PartyThinkParams thinkParams)
    {
        if (!IsPartyOrderRelevant(party, out var settings, out var order))
        {
            return;
        }

        HandleStayInSettlement(party, settings, order, thinkParams);
    }

    private bool ShouldContinueExecutingOrder(
        MobileParty party,
        PartyAiOrder order,
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
