using Bannerlord.PartyAI.Domain;
using Helpers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using static Bannerlord.PartyAI.PAICustomOrder;

namespace Bannerlord.PartyAI.CampaignBehaviors;

internal class PartyOrderExecutionCampaignBehavior : CampaignBehaviorBase
{
    private const int MinimumDaysOfFood = 3;
    private const int RecruitmentSettlementCooldownDays = 10;

    private List<PAISettlementVisitLog> _recentlyRecruitedFromSettlements = new();

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_assumingDirectControl", ref ControlAssumption.AssumingDirectControl);
        dataStore.SyncData("_recentlyRecruitedFromSettlements", ref _recentlyRecruitedFromSettlements);
    }

    public override void RegisterEvents()
    {
        CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
        CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, OnTroopRecruited);
    }

    private void OnTroopRecruited(Hero recruiter, Settlement settlement, Hero source, CharacterObject troop, int amount)
    {
        if (!SubModule.PartySettingsManager.IsHeroManageable(recruiter) || settlement is null)
        {
            return;
        }

        var settings = SubModule.PartySettingsManager.Settings(recruiter);
        if (settings.Order?.Behavior != OrderType.RecruitFromTemplate
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

    private void OnHourlyTickParty(MobileParty party)
    {
        if (party?.LeaderHero == null
            || !SubModule.PartySettingsManager.IsHeroManageable(party.LeaderHero))
        {
            return;
        }

        if (ShouldGetFood(party))
        {
            SetActionVisitNearestTownForFood(party);

            return;
        }

        // buy horses while waiting in settlements
        if (party.CurrentSettlement != null)
        {
            PartyHorseTrading.TryBuyAndSellHorses(party, party.CurrentSettlement);
        }

        var settings = SubModule.PartySettingsManager.Settings(party.LeaderHero);
        if (settings == null)
        {
            return;
        }

        if (settings.AutoRecruitment
            && party.PartySizeRatio < settings.AutoRecruitmentPercentage
            && !ControlAssumption.IsUnderControlAssumption(party)
            && party.Army == null)
        {
            if (settings.HasActiveOrder)
            {
                if (settings.Order.Behavior != OrderType.RecruitFromTemplate
                    && !settings.OrderQueue.Any(o => o.Behavior == OrderType.RecruitFromTemplate))
                {
                    settings.SetOrder(new(null, OrderType.RecruitFromTemplate));
                }
            }
            else
            {
                settings.SetOrder(new(null, OrderType.RecruitFromTemplate));
            }
        }
        if (settings.DismissUnwantedTroops && party.PartySizeRatio > settings.DismissUnwantedTroopsPercentage)
        {
            int max = (int)((party.PartySizeRatio - settings.DismissUnwantedTroopsPercentage) * party.Party.PartySizeLimit);
            if (max > 0)
            {
                SubModule.PartyTroopRecruiter.DismissUnwantedTroops(settings, party, max);
            }
        }
        if (settings.HasActiveOrder)
        {
            switch (settings.Order.Behavior)
            {
                case OrderType.BesiegeSettlement:
                    ImplementBesiegeSettlement(settings, party);
                    return;

                case OrderType.DefendSettlement:
                    ImplementDefendSettlement(settings, party);
                    return;

                case OrderType.StayInSettlement:
                    ImplementStayInSettlement(settings, party);
                    return;

                case OrderType.VisitSettlement:
                    ImplementVisitSettlement(settings, party);
                    return;

                case OrderType.AttackParty:
                    ImplementAttackParty(settings, party, settings.Order.Target);
                    return;

                case OrderType.EscortParty:
                    ImplementEscortParty(settings, party, settings.Order.Target);
                    return;

                case OrderType.RecruitFromTemplate:
                    ImplementRecruitFromTemplate(settings, party);
                    return;
            }
        }
        else if (settings.FallbackOrder != null && settings.FallbackOrder.Behavior != OrderType.None && party.Army == null)
        {
            settings.SetOrder(settings.FallbackOrder);
        }
    }

    private static void SetActionVisitNearestTownForFood(MobileParty party)
    {
        Settlement? town = Navigation.FindNearestSettlement(
            s => s.IsTown
                && (s.MapFaction == party.MapFaction
                || FactionManager.IsNeutralWithFaction(party.MapFaction, s.MapFaction)),
            party);

        if (town != null
            && Navigation.TryGetBestNavigationDataForSettlement(
                party,
                town,
                out MobileParty.NavigationType navType,
                out bool isFromPort,
                out bool isTargetingPort))
        {
            SetPartyAiAction.GetActionForVisitingSettlement(
                party,
                town,
                navType,
                isFromPort,
                isTargetingPort
            );
        } // TODO: else?
    }

    private void ImplementBesiegeSettlement(PartyAIClanPartySettings settings, MobileParty party)
    {
        var target = settings.Order.Target;

        if (!FactionManager.IsAtWarAgainstFaction(party.MapFaction, target.MapFaction))
        {
            settings.ClearOrder();
            ResetPartyAi(party);
            return;
        }

        if (!party.Ai.DoNotMakeNewDecisions || party.DefaultBehavior == AiBehavior.Hold)
        {
            if (target is Settlement targetSettlement)
            {
                SetPartyAiAction.GetActionForBesiegingSettlement(
                    party,
                    targetSettlement,
                    party.DesiredAiNavigationType,
                    false // isFromPort
                );

                party.Ai.SetDoNotMakeNewDecisions(true);
            }
            else
            {
                // Safety fallback if target somehow isn't a settlement
                settings.ClearOrder();
                ResetPartyAi(party);
            }
        }
    }

    private void ImplementDefendSettlement(PartyAIClanPartySettings settings, MobileParty party)
    {
        var target = settings.Order.Target;
        Settlement settlement = (Settlement)target;

        if (target.MapFaction != party.MapFaction)
        {
            settings.ClearOrder();
            return;
        }

        party.Ai.SetDoNotMakeNewDecisions(true);

        // Not in the target settlement yet -> move there
        if (party.CurrentSettlement != settlement)
        {
            if (Navigation.TryGetBestNavigationDataForSettlement(
                party,
                settlement,
                out MobileParty.NavigationType navType,
                out bool isFromPort,
                out bool isTargetingPort))
            {
                if (settlement.IsUnderSiege)
                {
                    SetPartyAiAction.GetActionForDefendingSettlement(
                        party,
                        settlement,
                        navType,
                        isFromPort,
                        isTargetingPort);
                }
                else
                {
                    SetPartyAiAction.GetActionForVisitingSettlement(
                        party,
                        settlement,
                        navType,
                        isFromPort,
                        isTargetingPort);
                }
            }
        }
    }

    private void ImplementStayInSettlement(PartyAIClanPartySettings settings, MobileParty party)
    {
        IMapPoint target = settings.Order.Target;
        Settlement settlement = (Settlement)target;

        if (FactionManager.IsAtWarAgainstFaction(target.MapFaction, party.MapFaction))
        {
            settings.ClearOrder();
            return;
        }

        party.Ai.SetDoNotMakeNewDecisions(true);

        if (party.CurrentSettlement != target)
        {
            if (settlement.IsUnderSiege)
            {
                settings.ClearOrder();
            }
            else if (target is Settlement targetSettlement)
            {
                if (Navigation.TryGetBestNavigationDataForSettlement(
                    party,
                    targetSettlement,
                    out MobileParty.NavigationType navType,
                    out bool isFromPort,
                    out bool isTargetingPort))
                {
                    SetPartyAiAction.GetActionForVisitingSettlement(
                        party,
                        targetSettlement,
                        navType,
                        isFromPort,
                        isTargetingPort
                    );
                }
            }
        }
    }

    private void ImplementVisitSettlement(PartyAIClanPartySettings settings, MobileParty party)
    {
        IMapPoint target = settings.Order.Target;
        Settlement settlement = (Settlement)target;

        if (FactionManager.IsAtWarAgainstFaction(target.MapFaction, party.MapFaction))
        {
            settings.ClearOrder();
            return;
        }

        party.Ai.SetDoNotMakeNewDecisions(true);

        if (party.CurrentSettlement != target)
        {
            if (settlement.IsUnderSiege)
            {
                settings.ClearOrder();
            }
            else if (target is Settlement targetSettlement)
            {
                if (Navigation.TryGetBestNavigationDataForSettlement(
                    party,
                    targetSettlement,
                    out MobileParty.NavigationType navType,
                    out bool isFromPort,
                    out bool isTargetingPort))
                {
                    SetPartyAiAction.GetActionForVisitingSettlement(
                        party,
                        targetSettlement,
                        navType,
                        isFromPort,
                        isTargetingPort
                    );
                }
            }
        }
    }

    private void ImplementAttackParty(PartyAIClanPartySettings settings, MobileParty party, IMapPoint target)
    {
        if (target is not MobileParty targetParty
            || targetParty == null
            || !FactionManager.IsAtWarAgainstFaction(party.MapFaction, targetParty.MapFaction))
        {
            settings.ClearOrder();
            ResetPartyAi(party);
            return;
        }

        bool navMismatch = party.DesiredAiNavigationType != targetParty.DesiredAiNavigationType;

        // Allow issuing the engage action when the AI is unlocked OR default hold OR navigation mode changed
        if (!party.Ai.DoNotMakeNewDecisions
            || party.DefaultBehavior == AiBehavior.Hold
            || navMismatch)
        {
            SetPartyAiAction.GetActionForEngagingParty(
                party,
                targetParty,
                targetParty.DesiredAiNavigationType, // use target's navigation type
                false // isFromPort
            );

            party.Ai.SetDoNotMakeNewDecisions(true);
        }
    }

    private void ImplementEscortParty(PartyAIClanPartySettings settings, MobileParty party, IMapPoint target)
    {
        if (target is not MobileParty targetParty
            || targetParty == null
            || FactionManager.IsAtWarAgainstFaction(party.MapFaction, targetParty.MapFaction))
        {
            settings.ClearOrder();
            ResetPartyAi(party);
            return;
        }

        bool navMismatch = party.DesiredAiNavigationType != targetParty.DesiredAiNavigationType;

        // Allow issuing the escort action when the AI is unlocked OR default hold OR navigation mode changed
        if (!party.Ai.DoNotMakeNewDecisions
            || party.DefaultBehavior == AiBehavior.Hold
            || navMismatch)
        {
            SetPartyAiAction.GetActionForEscortingParty(
                party,
                targetParty,
                targetParty.DesiredAiNavigationType, // use target's navigation type
                false, // isFromPort
                false  // isTargetingPort
            );

            party.Ai.SetDoNotMakeNewDecisions(true);
        }
    }

    private void ImplementRecruitFromTemplate(PartyAIClanPartySettings settings, MobileParty party)
    {
        int freeSlots = (int)((1f - party.PartySizeRatio) * party.Party.PartySizeLimit);
        if (freeSlots < 1)
        {
            settings.ClearOrder();
            return;
        }

        // === THREAT CHECK: Yield to vanilla AI if enemies nearby ===
        // Uses vanilla detection radius (3x encounter joining radius) and strength comparison
        float encounterRadius = Campaign.Current.Models.EncounterModel.GetEncounterJoiningRadius;
        float scanRadius = encounterRadius * 3f;

        try
        {
            LocatableSearchData<MobileParty> scan = MobileParty.StartFindingLocatablesAroundPosition(
                party.Position.ToVec2(),
                scanRadius);

            for (MobileParty enemy = MobileParty.FindNextLocatable(ref scan);
                 enemy != null;
                 enemy = MobileParty.FindNextLocatable(ref scan))
            {
                // Vanilla filters: skip self, inactive, in settlements (except garrisons), non-enemies
                if (enemy == party || !enemy.IsActive || enemy.IsDisbanding)
                    continue;

                if (enemy.CurrentSettlement != null && !enemy.IsGarrison)
                    continue;

                if (!FactionManager.IsAtWarAgainstFaction(party.MapFaction, enemy.MapFaction))
                    continue;

                if (enemy.Army != null && enemy.Army.LeaderParty != enemy && enemy.AttachedTo != null)
                    continue;

                if (party.IsCurrentlyAtSea != enemy.IsCurrentlyAtSea)
                    continue;

                // Vanilla strength comparison: flee if enemy is stronger
                float myStrength = (party.Army == null || party.AttachedTo == null && party.Army.LeaderParty != party)
                    ? party.Party.EstimatedStrength
                    : party.Army.EstimatedStrength;

                float enemyStrength = (enemy.Army == null || enemy.AttachedTo == null && enemy.Army.LeaderParty != enemy)
                    ? enemy.Party.EstimatedStrength
                    : enemy.Army.EstimatedStrength;

                // Vanilla flee condition: enemy is stronger AND aggressive/garrison
                if (myStrength < enemyStrength && (enemy.Aggressiveness > 0.01f || enemy.IsGarrison))
                {
                    // Unlock AI - vanilla will handle flee behavior
                    if (party.Ai.DoNotMakeNewDecisions)
                    {
                        party.Ai.SetDoNotMakeNewDecisions(false);
                    }
                    // Keep order active - party will resume recruiting once safe
                    return;
                }
            }
        }
        catch (KeyNotFoundException)
        {
            // LocatorGrid error - skip threat check this tick
        }

        var targetSettlement = settings.Order?.Target as Settlement;
        if (ShouldPickNewRecruitmentTarget(settings, party, targetSettlement))
        {
            var newTarget = Navigation.FindNearestSettlement(
                s => IsGoodTargetForRecruiting(s, party, settings),
                party);

            settings.Order?.Target = newTarget;
            targetSettlement = newTarget;

            if (targetSettlement is null)
            {
                ResetPartyAi(party);
                return;
            }
        }

        var currentSettlement = MobilePartyHelper.GetCurrentSettlementOfMobilePartyForAICalculation(party);
        if (currentSettlement != targetSettlement)
        {
            SetVisitSettlement(party, targetSettlement);
        }
    }

    private static void SetVisitSettlement(MobileParty party, Settlement settlement)
    {
        // Safe to lock AI and navigate
        if (!party.Ai.DoNotMakeNewDecisions)
        {
            party.Ai.SetDoNotMakeNewDecisions(true);
        }

        var navType = party.HasNavalNavigationCapability
            ? MobileParty.NavigationType.All
            : party.DesiredAiNavigationType;

        party.DesiredAiNavigationType = navType;

        SetPartyAiAction.GetActionForVisitingSettlement(
            party,
            settlement,
            navType,
            false,
            false
        );
    }

    private bool ShouldPickNewRecruitmentTarget(
        PartyAIClanPartySettings settings,
        MobileParty party,
        [NotNullWhen(false)]Settlement? currentSettlement)
    {
        if (currentSettlement is null)
        {
            return true;
        }

        return _recentlyRecruitedFromSettlements.Any(l => l.Settlement == currentSettlement && l.Party == party)
            || Recruitment.ComputeRecruitableVolunteersCount(party, currentSettlement, settings) == 0;
    }

    private bool IsGoodTargetForRecruiting(Settlement settlement, MobileParty party, PartyAIClanPartySettings settings)
    {
        if (!settlement.IsVillage && !settlement.IsTown)
        {
            return false;
        }

        if (!settings.RecruitFromEnemySettlements && FactionManager.IsAtWarAgainstFaction(party.MapFaction, settlement.MapFaction))
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

    private void ResetPartyAi(MobileParty party)
    {
        party.Ai.RethinkAtNextHourlyTick = true;
        party.Ai.SetDoNotMakeNewDecisions(false);
    }

    private bool ShouldGetFood(MobileParty party)
    {
        return party.GetNumDaysForFoodToLast() < MinimumDaysOfFood;
    }
}
