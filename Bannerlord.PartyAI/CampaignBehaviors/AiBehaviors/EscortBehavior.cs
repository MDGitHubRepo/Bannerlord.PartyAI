using Helpers;
using System.Diagnostics.CodeAnalysis;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace Bannerlord.PartyAI.CampaignBehaviors.AiBehaviors;

public class EscortBehavior : PartyAiBehaviorBase
{
    private const int FarDistance = 250;
    private const int CloseDistance = 5;
    private const int LowPriorityScore = 5;
    private const int HighPriorityScore = 10;

    public override void RegisterEvents()
    {
        CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, OnAiHourlyTick);
    }

    private void OnAiHourlyTick(MobileParty party, PartyThinkParams thinkParams)
    {
        if (!IsPartyOrderRelevant(party, PAICustomOrder.OrderType.EscortParty, out var settings, out var order))
        {
            return;
        }

        if (!ShouldContinueExecutingOrder(party, order, out var targetParty))
        {
            settings.ClearOrder();
            return;
        }

        party.Ai.SetInitiative(0, 0.33f, 24);

        if (!CalculateEscortPartyScore(party, targetParty, order, thinkParams))
        {
            settings.ClearOrder();
        }
    }

    private bool CalculateEscortPartyScore(
        MobileParty mobileParty,
        MobileParty targetParty,
        PAICustomOrder order,
        PartyThinkParams partyThinkParams)
    {
        float navDistance = 0f;
        bool isFromPort = false;
        bool isTargetingPort = false;
        MobileParty.NavigationType bestNavType = MobileParty.NavigationType.None;
        if (targetParty.CurrentSettlement is not null)
        {
            isTargetingPort = targetParty.CurrentSettlement.HasPort && mobileParty.IsCurrentlyAtSea;

            AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(
                mobileParty,
                targetParty.CurrentSettlement,
                isTargetingPort,
                out bestNavType,
                out navDistance,
                out isFromPort);
        }
        else
        {
            AiHelper.GetBestNavigationTypeAndDistanceOfMobilePartyForMobileParty(
                mobileParty,
                targetParty,
                out bestNavType,
                out navDistance);
        }

        if (bestNavType == MobileParty.NavigationType.None)
        {
            Message.OrderStoppedTargetUnreachable(mobileParty, order);
            return false;
        }

        AIBehaviorData aibehaviorData = new AIBehaviorData(
            targetParty,
            AiBehavior.EscortParty,
            bestNavType,
            false,
            isFromPort,
            isTargetingPort);

        float t = MBMath.InverseLerp(FarDistance, CloseDistance, navDistance);
        float score = MBMath.Lerp(LowPriorityScore, HighPriorityScore, t * t);
        AddBehaviorScore(aibehaviorData, score, partyThinkParams);
        return true;
    }

    private bool ShouldContinueExecutingOrder(
        MobileParty party,
        PAICustomOrder order,
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
