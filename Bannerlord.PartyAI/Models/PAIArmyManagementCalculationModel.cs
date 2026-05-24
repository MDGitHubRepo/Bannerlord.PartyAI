using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static PartyAIControls.PAICustomOrder;

namespace PartyAIControls.Models
{
    internal class PAIArmyManagementCalculationModel : ArmyManagementCalculationModel
    {
        private readonly ArmyManagementCalculationModel _previousModel;

        public PAIArmyManagementCalculationModel(ArmyManagementCalculationModel previousModel)
        {
            _previousModel = previousModel ?? new DefaultArmyManagementCalculationModel();
        }

        public override int InfluenceValuePerGold => _previousModel.InfluenceValuePerGold;

        public override int AverageCallToArmyCost => _previousModel.AverageCallToArmyCost;

        public override int CohesionThresholdForDispersion => _previousModel.CohesionThresholdForDispersion;

        // New abstract properties in newer versions, just forward to previous model
        public override float AIMobilePartySizeRatioToCallToArmy =>
            _previousModel.AIMobilePartySizeRatioToCallToArmy;

        public override float PlayerMobilePartySizeRatioToCallToArmy =>
            _previousModel.PlayerMobilePartySizeRatioToCallToArmy;

        public override float MinimumNeededFoodInDaysToCallToArmy =>
            _previousModel.MinimumNeededFoodInDaysToCallToArmy;

        public override float MaximumDistanceToCallToArmy =>
            _previousModel.MaximumDistanceToCallToArmy;

        public override float MaximumWaitTime =>
            _previousModel.MaximumWaitTime;

        public override ExplainedNumber CalculateDailyCohesionChange(Army army, bool includeDescriptions = false)
        {
            return _previousModel.CalculateDailyCohesionChange(army, includeDescriptions);
        }

        public override int CalculateNewCohesion(Army army, PartyBase newParty, int calculatedCohesion, int sign)
        {
            return _previousModel.CalculateNewCohesion(army, newParty, calculatedCohesion, sign);
        }

        public override int CalculatePartyInfluenceCost(MobileParty armyLeaderParty, MobileParty party)
        {
            return _previousModel.CalculatePartyInfluenceCost(armyLeaderParty, party);
        }

        public override int CalculateTotalInfluenceCost(Army army, float percentage)
        {
            return _previousModel.CalculateTotalInfluenceCost(army, percentage);
        }

        // Newer API: needs an explanation TextObject
        public override bool CheckPartyEligibility(MobileParty party, out TextObject explanation)
        {
            return _previousModel.CheckPartyEligibility(party, out explanation);
        }

        public override bool CanPlayerCreateArmy(out TextObject explanation)
        {
            return _previousModel.CanPlayerCreateArmy(out explanation);
        }

        public override float DailyBeingAtArmyInfluenceAward(MobileParty armyMemberParty)
        {
            return _previousModel.DailyBeingAtArmyInfluenceAward(armyMemberParty);
        }

        public override int GetCohesionBoostInfluenceCost(Army army, int percentageToBoost = 100)
        {
            return _previousModel.GetCohesionBoostInfluenceCost(army, percentageToBoost);
        }

        public override List<MobileParty> GetMobilePartiesToCallToArmy(MobileParty leaderParty)
        {
            List<MobileParty> result = _previousModel.GetMobilePartiesToCallToArmy(leaderParty);

            for (int index = result.Count - 1; index >= 0; index--)
            {
                MobileParty candidate = result[index];

                if (candidate?.LeaderHero == null ||
                    (candidate.LeaderHero?.Equals(leaderParty?.LeaderHero) ?? true))
                {
                    continue;
                }

                if (!SubModule.PartySettingsManager.IsHeroManageable(candidate.LeaderHero))
                {
                    continue;
                }

                PartyAIClanPartySettings settings = SubModule.PartySettingsManager.Settings(candidate.LeaderHero);

                if (!settings.AllowAllowJoinArmies)
                {
                    result.RemoveAt(index);
                    continue;
                }

                if (!settings.AllowSieging && IsArmyBesieging(leaderParty.Army))
                {
                    result.RemoveAt(index);
                    continue;
                }

                if (!settings.AllowRaidVillages && IsArmyRaiding(leaderParty.Army))
                {
                    result.RemoveAt(index);
                    continue;
                }

                if (settings.HasActiveOrder && settings.Order.Behavior == OrderType.RecruitFromTemplate)
                {
                    result.RemoveAt(index);
                    continue;
                }
            }

            return result;
        }

        public override int GetPartyRelation(Hero hero)
        {
            return _previousModel.GetPartyRelation(hero);
        }

        public override float GetPartySizeScore(MobileParty party)
        {
            return _previousModel.GetPartySizeScore(party);
        }

        public override bool CanLordCreateArmy(MobileParty leaderParty, out MBList<MobileParty> possibleArmyMembers)
        {
            return _previousModel.CanLordCreateArmy(leaderParty, out possibleArmyMembers);
        }

        internal static bool IsArmyBesieging(Army army)
        {
            if (army == null)
                return false;

            // In newer versions AIBehavior / AIBehaviorFlags are gone; ArmyType is enough here.
            return army.ArmyType == Army.ArmyTypes.Besieger;
        }

        internal static bool IsArmyRaiding(Army army)
        {
            if (army == null)
                return false;

            return army.ArmyType == Army.ArmyTypes.Raider;
        }
    }
}