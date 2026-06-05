using Bannerlord.PartyAI.Domain;
using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static Bannerlord.PartyAI.PAICustomOrder;

namespace Bannerlord.PartyAI.CampaignBehaviors;

internal class PartyAIThinker : CampaignBehaviorBase
{
    private readonly ControlAssumptionBehavior _controlAssumptionBehavior;

    public PartyAIThinker(ControlAssumptionBehavior controlAssumptionBehavior)
    {
        _controlAssumptionBehavior = controlAssumptionBehavior;
    }

    public override void SyncData(IDataStore dataStore)
    {
    }

    public override void RegisterEvents()
    {
        CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, OnAiHourlyTick);
        CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
        CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
        CampaignEvents.OnPartyJoinedArmyEvent.AddNonSerializedListener(this, OnPartyJoinedArmy);
        CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
        CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnMobilePartyCreated);
        CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
    }

    private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
    {
        if (party?.LeaderHero == null || settlement == null)
        {
            return;
        }
        if (!SubModule.PartySettingsManager.IsHeroManageable(party.LeaderHero))
        {
            return;
        }

        PartyAIClanPartySettings settings = SubModule.PartySettingsManager.Settings(party.LeaderHero);
        if (settings.Order?.Behavior == OrderType.VisitSettlement
            && settings.Order.Target == settlement)
        {
            settings.ClearOrder();
        }
    }

    private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
    {
        foreach (PartyAIClanPartySettings settings in SubModule.PartySettingsManager.HeroesWithOrders)
        {
            if (settings.Order.Behavior == OrderType.BesiegeSettlement && settings.Order.Target == settlement)
            {
                if (!FactionManager.IsAtWarAgainstFaction(settings.Hero.MapFaction, settlement.MapFaction))
                {
                    settings.ClearOrder();
                }
            }
        }
    }

    private void OnHeroPrisonerTaken(PartyBase party, Hero prisoner)
    {
        if (SubModule.PartySettingsManager.IsHeroManageable(prisoner))
        {
            PartyAIClanPartySettings settings = SubModule.PartySettingsManager.Settings(prisoner);
            settings.ClearAllOrders();
        }
    }

    private void OnPartyJoinedArmy(MobileParty mobileParty)
    {
        if (mobileParty is null)
        {
            return;
        }

        var leaderHero = mobileParty.LeaderHero;

        if (leaderHero is null
            || !SubModule.PartySettingsManager.IsHeroManageable(leaderHero)
            || !SubModule.PartySettingsManager.HasActiveOrder(leaderHero))
        {
            return;
        }

        var partyText = mobileParty.Name;
        var orderText = SubModule.PartySettingsManager.GetOrderText(leaderHero);
        var armyText = mobileParty.Army?.Name is null
            ? "an army"
            : mobileParty.Army.Name.ToString();
        
        TextObject text = new TextObject("{=PAIOEWao2aI}{PARTY} is no longer {ORDER} because they were called to {ARMY}")
          .SetTextVariable("PARTY", partyText)
          .SetTextVariable("ORDER", orderText)
          .SetTextVariable("ARMY", armyText);
        
        InformationManager.DisplayMessage(new InformationMessage(text.ToString(), Colors.Magenta));

        PartyAIClanPartySettings settings = SubModule.PartySettingsManager.Settings(leaderHero);
        settings.ClearAllOrders();
    }

    private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
    {
        if (mobileParty?.LeaderHero != null && SubModule.PartySettingsManager.IsHeroManageable(mobileParty.LeaderHero))
        {
            PartyAIClanPartySettings settings = SubModule.PartySettingsManager.Settings(mobileParty.LeaderHero);
            settings.ClearAllOrders();
        }

        foreach (PartyAIClanPartySettings settings in SubModule.PartySettingsManager.HeroesWithOrders)
        {
            PAICustomOrder order = settings.Order;
            switch (order.Behavior)
            {
                case OrderType.AttackParty:
                case OrderType.EscortParty:
                    if (order.Target is not MobileParty m || m != mobileParty)
                    {
                        continue;
                    }
                    settings.ClearOrder();
                    if (_controlAssumptionBehavior.IsUnderControlAssumption(settings.Hero?.PartyBelongedTo))
                    {
                        settings.SetOrder(OrderType.EscortParty, MobileParty.MainParty);
                        MobileParty escortingParty = settings.Hero?.PartyBelongedTo;
                        if (escortingParty != null)
                        {
                            SetPartyAiAction.GetActionForEscortingParty(
                                escortingParty,
                                MobileParty.MainParty,
                                MobileParty.MainParty.DesiredAiNavigationType,
                                false,
                                false);
                            escortingParty.Ai.SetDoNotMakeNewDecisions(true);
                        }
                        else
                        {
                            settings.ClearOrder();
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private void OnMobilePartyCreated(MobileParty mobileParty)
    {
        if (mobileParty?.LeaderHero == null) return;
        if (SubModule.PartySettingsManager.IsHeroManageable(mobileParty.LeaderHero))
        {
            PartyAIClanPartySettings settings = SubModule.PartySettingsManager.Settings(mobileParty.LeaderHero);

            settings.ClearAllOrders();
            settings.ResetBudgets();

            if (settings.FallbackOrder != null && settings.FallbackOrder.Behavior != OrderType.None)
            {
                settings.SetOrder(settings.FallbackOrder.Behavior, settings.FallbackOrder.Target);
            }
        }
    }

    internal void OnAiHourlyTick(MobileParty party, PartyThinkParams thinkParams)
    {
        if (party?.LeaderHero is null
            || !SubModule.PartySettingsManager.IsHeroManageable(party.LeaderHero))
        {
            return;
        }

        PartyAIClanPartySettings settings = SubModule.PartySettingsManager.Settings(party.LeaderHero);
        if (settings is null)
        {
            return;
        }

        // These three should probably be in the "doing" part instead of "thinking"
        ImplementAllowRaidingVillages(party, thinkParams, settings);
        ImplementAllowJoiningArmies(party, thinkParams, settings);
        ImplementAllowBesieging(party, thinkParams, settings);

        if (!settings.HasActiveOrder)
        {
            return;
        }

        IMapPoint target = settings.Order.Target;
        MobileParty.PartyObjective existingObjective = party.Objective;
        List<(AIBehaviorData, float)> newParams;
        switch (settings.Order.Behavior)
        {
            case OrderType.PatrolAroundPoint:
                ImplementPatrolAroundSettlement(settings, party, target, thinkParams, out newParams, distanceFactor: settings.PatrolRadius);
                break;
            case OrderType.PatrolClanLands:
                ImplementPatrolClanLands(settings.Hero, party, target, thinkParams, out newParams);
                break;
            default:
                return;
        }

        SwapParams(thinkParams, party, newParams);

        if (existingObjective != party.Objective)
        {
            settings.CachedPartyObjective = existingObjective;
        }
    }

    private void SwapParams(PartyThinkParams thinkParams, MobileParty party, List<(AIBehaviorData, float)> newParams)
    {
        thinkParams.Reset(party);
        float threshold = 0.3f;
        bool aboveThreshold = newParams.Any(p => p.Item2 > threshold);
        for (int i = 0; i < newParams.Count; i++)
        {
            (AIBehaviorData, float) param = newParams[i];
            if (!aboveThreshold)
            {
                param.Item2 += threshold;
            }
            thinkParams.AddBehaviorScore(param);
        }
    }

    private void ImplementPatrolClanLands(Hero hero, MobileParty party, IMapPoint target, in PartyThinkParams thinkParams, out List<(AIBehaviorData, float)> newParams, float distanceFactor = 1.0f, bool useQuickDistance = false)
    {
        newParams = new List<(AIBehaviorData, float)>();

        var safeNavType = Navigation.SanitizeNavigationType(party.DesiredAiNavigationType);
        float range = Navigation.GetSafeDistanceBetweenClosestTwoTowns(safeNavType) * 0.9f * distanceFactor;

        if (hero?.Clan?.Settlements?.Count == 0)
        {
            newParams = thinkParams.AIBehaviorScores.ConvertAll(s => (s.Item1, s.Item2));
            return;
        }

        if (hero?.Clan == null)
            return;

        // Find nearest clan settlement to patrol around
        Settlement? nearestClanSettlement = Navigation.FindNearestSettlement(s => s.OwnerClan == hero.Clan, party);

        if (nearestClanSettlement == null)
        {
            return;
        }

        // 5% chance to switch to a random clan settlement (variety in patrol)
        if (MBRandom.RandomFloat < 0.05f && hero.Clan.Settlements.Count > 0)
        {
            nearestClanSettlement = hero.Clan.Settlements.GetRandomElementInefficiently();
        }

        // === PRIORITY: React to clan settlements in danger ===
        foreach (Settlement clanSettlement in hero.Clan.Settlements)
        {
            float distToClanSettlement = party.GetPosition2D.Distance(clanSettlement.GetPosition2D);

            if (distToClanSettlement > range * 8)
                continue;

            if (clanSettlement.IsFortification && clanSettlement.IsUnderSiege)
            {
                if (Navigation.TryGetBestNavigationDataForSettlement(party, clanSettlement, out MobileParty.NavigationType navType, out bool isFromPort, out bool isTargetingPort))
                {
                    newParams.Add((
                        new AIBehaviorData(clanSettlement, AiBehavior.DefendSettlement, navType, false, isFromPort, isTargetingPort),
                        8f
                    ));
                }

                if (party.Objective != MobileParty.PartyObjective.Defensive)
                {
                    party.SetPartyObjective(MobileParty.PartyObjective.Defensive);
                }
                return;
            }

            if (clanSettlement.IsVillage && clanSettlement.Village?.VillageState == Village.VillageStates.BeingRaided)
            {
                if (Navigation.TryGetBestNavigationDataForSettlement(party, clanSettlement, out MobileParty.NavigationType navType, out bool isFromPort, out bool isTargetingPort))
                {
                    newParams.Add((
                        new AIBehaviorData(clanSettlement, AiBehavior.DefendSettlement, navType, false, isFromPort, isTargetingPort),
                        8f
                    ));
                }

                if (party.Objective != MobileParty.PartyObjective.Defensive)
                {
                    party.SetPartyObjective(MobileParty.PartyObjective.Defensive);
                }
                return;
            }
        }

        // === If too far from clan lands, issue command to walk there ===
        var distance = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(
            party,
            nearestClanSettlement,
            safeNavType);
        if (distance > range * 4)
        {
            if (Navigation.TryGetBestNavigationDataForSettlement(party, nearestClanSettlement, out MobileParty.NavigationType navType, out bool isFromPort, out bool isTargetingPort))
            {
                newParams.Add((
                    new AIBehaviorData(nearestClanSettlement, AiBehavior.GoToSettlement, navType, false, isFromPort, isTargetingPort),
                    5f
                ));
            }
        }

        // === ALWAYS filter vanilla AI behaviors by distance ===
        foreach ((AIBehaviorData behavior, float weight) in thinkParams.AIBehaviorScores)
        {
            var behaviorTarget = ExtractPositionFromBehavior(behavior);

            if (behaviorTarget == CampaignVec2.Zero)
            {
                continue;
            }

            float distToTarget = DistanceHelper.FindClosestDistanceFromSettlementToPoint(
                    nearestClanSettlement,
                    behaviorTarget,
                    safeNavType,
                    isFromPort: out bool _);

            if (distToTarget < range)
            {
                newParams.Add((behavior, weight));
            }
        }

        if (party.Objective != MobileParty.PartyObjective.Aggressive)
        {
            party.SetPartyObjective(MobileParty.PartyObjective.Aggressive);
        }
    }

    private void ImplementPatrolAroundSettlement(PartyAIClanPartySettings settings, MobileParty party, IMapPoint target, in PartyThinkParams thinkParams, out List<(AIBehaviorData, float)> newParams, float distanceFactor = 1.0f)
    {
        newParams = new List<(AIBehaviorData, float)>();

        var safeNavType = Navigation.SanitizeNavigationType(party.DesiredAiNavigationType);

        Settlement centerSettlement = (Settlement)target;

        // Range is a filtering/"too far" heuristic; navigation routing uses vanilla-derived data.
        float range = Navigation.GetSafeDistanceBetweenClosestTwoTowns(MobileParty.NavigationType.Default) * 0.9f * distanceFactor;

        // Compute best navigation and port flags for reaching the patrol center.
        if (!Navigation.TryGetBestNavigationDataForSettlement(party, centerSettlement, out MobileParty.NavigationType centerNavType, out bool centerIsFromPort, out bool centerIsTargetingPort))
        {
            newParams = thinkParams.AIBehaviorScores.ConvertAll(s => (s.Item1, s.Item2));
            return;
        }

        // === PRIORITY: Defend nearby same-faction settlements under attack ===
        foreach (Settlement settlement in Settlement.All)
        {
            if (settlement.MapFaction != party.MapFaction)
                continue;

            float distToSettlement = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(
                party,
                settlement,
                safeNavType);

            if (distToSettlement > range)
                continue;

            if (IsSettlementUnderAttack(settlement))
            {
                if (Navigation.TryGetBestNavigationDataForSettlement(party, settlement, out MobileParty.NavigationType defendNavType, out bool defendIsFromPort, out bool defendIsTargetingPort))
                {
                    SetPartyAiAction.GetActionForDefendingSettlement(
                        party,
                        settlement,
                        defendNavType,
                        defendIsFromPort,
                        defendIsTargetingPort
                    );
                }
                return;
            }
        }

        var distanceToCenter = DistanceHelper.FindClosestDistanceFromMobilePartyToSettlement(
            party,
            centerSettlement,
            safeNavType);

        // If too far from patrol center: walk there
        if (distanceToCenter > range * 4)
        {
            SetPartyAiAction.GetActionForVisitingSettlement(
                party,
                centerSettlement,
                centerNavType,
                centerIsFromPort,
                centerIsTargetingPort
            );

            newParams.Add((
                new AIBehaviorData(centerSettlement, AiBehavior.GoToSettlement, centerNavType, false, centerIsFromPort, centerIsTargetingPort),
                5f
            ));

            return;
        }

        // Anchor patrol on the intended settlement.
        newParams.Add((
            new AIBehaviorData(centerSettlement, AiBehavior.PatrolAroundPoint, centerNavType, false, centerIsFromPort, centerIsTargetingPort),
            6f
        ));

        // In range: filter vanilla behavior scores by distance to center
        foreach ((AIBehaviorData behavior, float weight) in thinkParams.AIBehaviorScores)
        {
            var behaviorTarget = ExtractPositionFromBehavior(behavior);

            if (behaviorTarget == CampaignVec2.Zero)
            {
                continue;
            }

            float distToTarget = DistanceHelper.FindClosestDistanceFromSettlementToPoint(
                    centerSettlement,
                    behaviorTarget,
                    safeNavType,
                    isFromPort: out bool _);

            if (distToTarget < range)
            {
                newParams.Add((behavior, weight));
            }
        }

        if (party.Objective != MobileParty.PartyObjective.Aggressive)
        {
            party.SetPartyObjective(MobileParty.PartyObjective.Aggressive);
        }
    }

    private void ImplementAllowRaidingVillages(MobileParty party, PartyThinkParams thinkParams, PartyAIClanPartySettings settings)
    {
        if (settings.AllowRaidVillages)
        {
            return;
        }

        // prevent raiding in army (leave if they raid)
        // The other half of this is in HarmonyPatches.AiMilitaryBehaviorPatches
        if (party.Army != null && !party.Army.LeaderParty.LeaderHero.Equals(party.LeaderHero))
        {
            if (IsArmyRaiding(party.Army))
            {
                // refund influence
                int influence = Campaign.Current.Models.ArmyManagementCalculationModel.CalculatePartyInfluenceCost(party.Army.LeaderParty, party);
                ChangeClanInfluenceAction.Apply(party.Army.LeaderParty.LeaderHero.Clan, influence);

                LeaveArmy(party, thinkParams);
            }
        }
    }

    private void ImplementAllowJoiningArmies(MobileParty party, PartyThinkParams thinkParams, PartyAIClanPartySettings settings)
    {
        var army = party.Army;
        if (army is null)
        {
            return;
        }

        var armyLeaderHero = army.LeaderParty?.LeaderHero;
        if (!settings.AllowJoinArmies
            && armyLeaderHero != party.LeaderHero
            && armyLeaderHero != Hero.MainHero)
        {
            LeaveArmy(party, thinkParams);
        }
    }

    private void ImplementAllowBesieging(MobileParty party, PartyThinkParams thinkParams, PartyAIClanPartySettings settings)
    {
        if (settings.AllowSieging)
        {
            return;
        }

        // prevent besieging in army (leave if they besiege)
        // The other half of this is in HarmonyPatches.AiMilitaryBehaviorPatches
        if (party.Army != null && !party.Army.LeaderParty.LeaderHero.Equals(party.LeaderHero))
        {
            if (IsArmyBesieging(party.Army))
            {
                LeaveArmy(party, thinkParams);
            }
        }
    }

    private void LeaveArmy(MobileParty party, PartyThinkParams thinkParams)
    {
        // refund influence
        int influence = Campaign.Current.Models.ArmyManagementCalculationModel
            .CalculatePartyInfluenceCost(party.Army.LeaderParty, party);
        ChangeClanInfluenceAction.Apply(party.Army.LeaderParty.LeaderHero.Clan, influence);

        party.Army = null;

        // Find nearest friendly/neutral fortification to send the party to
        Settlement? nearestFort = Navigation.FindNearestSettlement(
            s => s.IsFortification
                && (s.MapFaction == party.MapFaction
                ||  FactionManager.IsNeutralWithFaction(party.MapFaction, s.MapFaction)),
            party
        );

        if (nearestFort != null && Navigation.TryGetBestNavigationDataForSettlement(party, nearestFort, out MobileParty.NavigationType navType, out bool isFromPort, out bool isTargetingPort))
        {
            SetPartyAiAction.GetActionForVisitingSettlement(
                party,
                nearestFort,
                navType,
                isFromPort,
                isTargetingPort
            );
        }

        ResetPartyAi(party);
        thinkParams.Reset(party);
    }

    private void ResetPartyAi(MobileParty party)
    {
        party.Ai.RethinkAtNextHourlyTick = true;
        party.Ai.SetDoNotMakeNewDecisions(false);
    }

    private static CampaignVec2 ExtractPositionFromBehavior(AIBehaviorData behavior)
    {
        CampaignVec2 position;
        if (behavior.Position != CampaignVec2.Zero)
        {
            position = behavior.Position;
        }
        else if (behavior.Party is not null && behavior.Party.Position != CampaignVec2.Zero)
        {
            position = behavior.Party.Position;
        }
        else
        {
            position = CampaignVec2.Zero;
        }

        return position;
    }

    private static bool IsSettlementUnderAttack(Settlement settlement)
    {
        if (settlement.IsFortification)
        {
            return settlement.IsUnderSiege;
        }

        if (settlement.IsVillage)
        {
            return settlement.Village?.VillageState == Village.VillageStates.BeingRaided;
        }

        return false;
    }

    private static bool IsArmyRaiding(Army army)
    {
        if (army == null)
            return false;

        return army.ArmyType == Army.ArmyTypes.Raider;
    }

    private static bool IsArmyBesieging(Army army)
    {
        if (army == null)
            return false;

        // In newer versions AIBehavior / AIBehaviorFlags are gone; ArmyType is enough here.
        return army.ArmyType == Army.ArmyTypes.Besieger;
    }
}
