using Bannerlord.PartyAI.Domain.Models;
using Bannerlord.PartyAI.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.PartyAI.CampaignBehaviors.AiBehaviors;

public class VisitSettlementBehavior : PartyOrderBehaviorBase
{
    protected override PartyAiOrderType OrderType => PartyAiOrderType.VisitSettlement;

    public override void RegisterEvents()
    {
        CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, OnAiHourlyTick);
        CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
    }

    public void HandleVisitSettlement(
        MobileParty party,
        Settlement targetSettlement,
        PartyAiEntitySettings settings,
        PartyAiOrder order,
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

        if (!TryNavigateToSettlement(party, targetSettlement, AiBehavior.GoToSettlement, thinkParams))
        {
            Message.OrderStoppedTargetUnreachable(party, order);
            settings.ClearOrder();
            return;
        }

        party.Ai.SetInitiative(0f, 1f, 2f);
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

        if (party.CurrentSettlement == targetSettlement)
        {
            settings.ClearOrder();
            return;
        }

        HandleVisitSettlement(party, targetSettlement, settings, order, thinkParams);
    }

    private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
    {
        if (!IsPartyOrderRelevant(party, out var settings, out var order))
        {
            return;
        }

        if (order.Target == settlement)
        {
            settings.ClearOrder();
        }
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
