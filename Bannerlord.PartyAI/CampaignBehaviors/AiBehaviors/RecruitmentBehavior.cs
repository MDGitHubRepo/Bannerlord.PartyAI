using Bannerlord.PartyAI.Domain;
using Helpers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace Bannerlord.PartyAI.CampaignBehaviors.AiBehaviors;

internal class RecruitmentBehavior : PartyAiBehaviorBase
{
    private const PAICustomOrder.OrderType RecruitOrderType = PAICustomOrder.OrderType.RecruitFromTemplate;
    private const int RecruitmentSettlementCooldownDays = 10;

    private List<PAISettlementVisitLog> _recentlyRecruitedFromSettlements = new();

    public override void RegisterEvents()
    {
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, OnTroopRecruited);
        CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, OnAiHourlyTick);
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_recentlyRecruitedFromSettlements", ref _recentlyRecruitedFromSettlements);
    }

    private void OnTroopRecruited(Hero recruiter, Settlement settlement, Hero source, CharacterObject troop, int amount)
    {
        if (!IsPartyOrderRelevant(recruiter, RecruitOrderType, out var settings, out _))
        {
            return;
        }

        if (settlement is null
            || Recruitment.ComputeRecruitableVolunteersCount(recruiter.PartyBelongedTo, settlement, settings) > 0)
        {
            return;
        }

        _recentlyRecruitedFromSettlements.Add(new(settlement, CampaignTime.Now, recruiter.PartyBelongedTo));
    }

    private void OnDailyTick()
    {
        _recentlyRecruitedFromSettlements.RemoveAll(l => l.Visited.ElapsedDaysUntilNow > RecruitmentSettlementCooldownDays);
    }

    private void OnAiHourlyTick(MobileParty party, PartyThinkParams thinkParams)
    {
        if (!IsPartyOrderRelevant(party, RecruitOrderType, out var settings, out var order))
        {
            return;
        }

        int freeSlots = party.Party.PartySizeLimit - party.Party.NumberOfAllMembers;
        if (freeSlots < 1)
        {
            settings.ClearOrder();
            return;
        }

        party.Ai.SetInitiative(0, 1, 24);

        var targetSettlement = order.Target as Settlement;
        if (ShouldPickNewRecruitmentTarget(settings, party, targetSettlement))
        {
            var newTarget = Navigation.FindNearestSettlement(
                s => IsGoodTargetForRecruiting(s, party, settings),
                party);

            settings.Order?.Target = newTarget;
            targetSettlement = newTarget;
        }

        if (targetSettlement is null)
        {
            return; // fallback to default AI
        }

        if (!CalculateVisitSettlementScore(party, targetSettlement, order, thinkParams))
        {
            settings.ClearOrder();
        }
    }

    private bool ShouldPickNewRecruitmentTarget(
        PartyAIClanPartySettings settings,
        MobileParty party,
        [NotNullWhen(false)] Settlement? currentSettlement)
    {
        if (currentSettlement is null)
        {
            return true;
        }

        return _recentlyRecruitedFromSettlements.Any(l => l.Settlement == currentSettlement && l.Party == party)
            || Recruitment.ComputeRecruitableVolunteersCount(party, currentSettlement, settings) == 0
            || !CanVisitSettlement(party, currentSettlement);
    }

    private bool IsGoodTargetForRecruiting(
        Settlement settlement,
        MobileParty party,
        PartyAIClanPartySettings settings)
    {
        if (!settlement.IsVillage && !settlement.IsTown)
        {
            return false;
        }

        if (!CanVisitSettlement(party, settlement))
        {
            return false;
        }

        if (!settings.RecruitFromEnemySettlements
            && FactionManager.IsAtWarAgainstFaction(party.MapFaction, settlement.MapFaction))
        {
            return false;
        }

        var template = settings.PartyTemplate;
        if (template is not null && !template.TroopCultures.Contains(settlement.Culture))
        {
            return false;
        }

        if (_recentlyRecruitedFromSettlements.Any(l => l.Settlement == settlement && l.Party == party))
        {
            return false;
        }

        int count = Recruitment.ComputeRecruitableVolunteersCount(party, settlement, settings);
        if (count == 0)
        {
            return false;
        }

        return true;
    }

    private bool CalculateVisitSettlementScore(
        MobileParty mobileParty,
        Settlement target,
        PAICustomOrder order,
        PartyThinkParams partyThinkParams)
    {
        var isTargetingPort = target.HasPort && mobileParty.IsCurrentlyAtSea;

        AiHelper.GetBestNavigationTypeAndAdjustedDistanceOfSettlementForMobileParty(
            mobileParty,
            target,
            isTargetingPort,
            out var bestNavType,
            out var bestNavDistance,
            out var isFromPort);

        if (bestNavType == MobileParty.NavigationType.None)
        {
            Message.OrderStoppedTargetUnreachable(mobileParty, order);
            return false;
        }

        AIBehaviorData aibehaviorData = new AIBehaviorData(
            target,
            AiBehavior.GoToSettlement,
            bestNavType,
            false,
            isFromPort,
            isTargetingPort);

        //var partySizeRatio = MathF.Clamp(mobileParty.PartySizeRatio, 0, 1);
        //var partySizeScore = 5f * MathF.Pow(1f - partySizeRatio, 2f);
        AddBehaviorScore(aibehaviorData, 5f, partyThinkParams);
        return true;
    }

    private bool CanVisitSettlement(MobileParty mobileParty, Settlement settlement)
    {
        if (mobileParty.HasLandNavigationCapability)
        {
            return !settlement.IsUnderSiege && settlement.Party.MapEvent == null;
        }
        else
        {
            return settlement.SiegeEvent == null || !settlement.SiegeEvent.IsBlockadeActive;
        }
    }
}
