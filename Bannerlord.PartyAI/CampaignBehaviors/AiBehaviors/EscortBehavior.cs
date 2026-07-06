using Bannerlord.PartyAI.Domain.Models;
using System.Diagnostics.CodeAnalysis;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.PartyAI.CampaignBehaviors.AiBehaviors;

public class EscortBehavior : PartyOrderBehaviorBase
{
    protected override PartyAiOrderType OrderType => PartyAiOrderType.EscortParty;

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

        if (!ShouldContinueExecutingOrder(party, order, out var targetParty))
        {
            settings.ClearOrder();
            return;
        }

        if (!TryNavigateToParty(party, targetParty, AiBehavior.EscortParty, thinkParams))
        {
            Message.OrderStoppedTargetUnreachable(party, order);
            settings.ClearOrder();
            return;
        }

        party.Ai.SetInitiative(0f, 0.33f, 2f);
    }

    private bool ShouldContinueExecutingOrder(
        MobileParty party,
        PartyAiOrder order,
        [NotNullWhen(true)] out MobileParty? targetParty)
    {
        var target = order.Target as MobileParty;

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

        targetParty = target;
        return canContinue;
    }
}
