using Bannerlord.PartyAI.Domain;
using Bannerlord.PartyAI.Domain.Models;
using Bannerlord.PartyAI.Models;
using Helpers;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

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
        CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnMobilePartyCreated);
    }

    private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
    {
        if (mobileParty?.LeaderHero != null && SubModule.PartySettingsManager.IsHeroManageable(mobileParty.LeaderHero))
        {
            PartyAiEntitySettings settings = SubModule.PartySettingsManager.Settings(mobileParty.LeaderHero);
            settings.ClearAllOrders();
        }

        foreach (PartyAiEntitySettings settings in SubModule.PartySettingsManager.HeroesWithOrders)
        {
            PartyAiOrder order = settings.Order;
            switch (order.Behavior)
            {
                case PartyAiOrderType.AttackParty:
                case PartyAiOrderType.EscortParty:
                    if (order.Target is not MobileParty m || m != mobileParty)
                    {
                        continue;
                    }
                    settings.ClearOrder();
                    if (_controlAssumptionBehavior.IsUnderControlAssumption(settings.Hero?.PartyBelongedTo))
                    {
                        settings.SetOrder(PartyAiOrderType.EscortParty, MobileParty.MainParty);
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
            PartyAiEntitySettings settings = SubModule.PartySettingsManager.Settings(mobileParty.LeaderHero);

            settings.ClearAllOrders();
            settings.ResetBudgets();

            if (settings.FallbackOrder != null && settings.FallbackOrder.Behavior != PartyAiOrderType.None)
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

        PartyAiEntitySettings settings = SubModule.PartySettingsManager.Settings(party.LeaderHero);
        if (settings is null)
        {
            return;
        }

        if (!settings.HasActiveOrder)
        {
            return;
        }

        IMapPoint target = settings.Order.Target;
        MobileParty.PartyObjective existingObjective = party.Objective;
        List<(AIBehaviorData, float)> newParams;
        switch (settings.Order.Behavior)
        {
            case PartyAiOrderType.PatrolClanLands:
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
}
