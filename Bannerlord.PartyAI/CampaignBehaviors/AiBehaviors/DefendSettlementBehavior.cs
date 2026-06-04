using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Bannerlord.PartyAI.CampaignBehaviors.AiBehaviors;

public class DefendSettlementBehavior : PartyAiBehaviorBase
{
    private readonly StayInSettlementBehavior _stayInSettlementBehavior;

    public DefendSettlementBehavior(StayInSettlementBehavior stayInSettlementBehavior)
    {
        _stayInSettlementBehavior = stayInSettlementBehavior;
    }

    public override void RegisterEvents()
    {
        CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, OnAiHourlyTick);
    }

    private void OnAiHourlyTick(MobileParty party, PartyThinkParams thinkParams)
    {
        if (!IsPartyOrderRelevant(party, PAICustomOrder.OrderType.DefendSettlement, out var settings, out var order))
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

        var shouldDefendPort = ShouldDefendPort(party, targetSettlement);

        if (!targetSettlement.IsUnderSiege && !shouldDefendPort)
        {
            _stayInSettlementBehavior.HandleStayInSettlement(party, settings, order, thinkParams);
            return;
        }

        AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(
            party,
            targetSettlement,
            shouldDefendPort,
            out var navigationType,
            out _,
            out var isFromPort);

        var behaviorData = new AIBehaviorData(
            targetSettlement,
            AiBehavior.DefendSettlement,
            navigationType,
            false,
            isFromPort,
            shouldDefendPort);

        AddBehaviorScore(behaviorData, 5f, thinkParams);
    }

    private bool ShouldDefendPort(MobileParty party, Settlement targetSettlement)
    {
        // I don't really get this, I've copied and refactored it from
        // AiMilitaryBehavior.GetDistanceScoreForDefending
        // I really hope TW knows what they're doing (won't be surprised if they don't) :)
        var canUsePort = targetSettlement.HasPort && party.HasNavalNavigationCapability;

        if (!canUsePort)
        {
            return false;
        }

        var siegeEvent = targetSettlement.SiegeEvent;
        if (siegeEvent == null)
        {
            return false;
        }

        if (!siegeEvent.IsBlockadeActive)
        {
            return false;
        }

        var mapEvent = siegeEvent.BesiegerCamp.LeaderParty.MapEvent;
        return mapEvent != null
            && (mapEvent.IsBlockade || mapEvent.IsBlockadeSallyOut);
    }
}