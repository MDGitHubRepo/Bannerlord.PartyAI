using Bannerlord.PartyAI.Domain;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Bannerlord.PartyAI.Models;

internal class PAIArmyManagementCalculationModel : ArmyManagementCalculationModel
{
    private readonly ArmyManagementCalculationModel _previousModel;

    public PAIArmyManagementCalculationModel(ArmyManagementCalculationModel previousModel)
    {
        _previousModel = previousModel ?? new DefaultArmyManagementCalculationModel();
    }

    public override bool CanLordCreateArmy(MobileParty leaderParty, out MBList<MobileParty> possibleArmyMembers)
    {
        var result = _previousModel.CanLordCreateArmy(leaderParty, out possibleArmyMembers);

        CallingToArmy.RemoveForbiddenPartiesFromArmyCall(leaderParty, possibleArmyMembers);

        return result;
    }

    #region Pass-through overrides
    public override float AIMobilePartySizeRatioToCallToArmy => _previousModel.AIMobilePartySizeRatioToCallToArmy;

    public override float PlayerMobilePartySizeRatioToCallToArmy => _previousModel.PlayerMobilePartySizeRatioToCallToArmy;

    public override float MinimumNeededFoodInDaysToCallToArmy => _previousModel.MinimumNeededFoodInDaysToCallToArmy;

    public override float MaximumDistanceToCallToArmy => _previousModel.MaximumDistanceToCallToArmy;

    public override int InfluenceValuePerGold => _previousModel.InfluenceValuePerGold;

    public override int AverageCallToArmyCost => _previousModel.AverageCallToArmyCost;

    public override int CohesionThresholdForDispersion => _previousModel.CohesionThresholdForDispersion;

    public override float MaximumWaitTime => _previousModel.MaximumWaitTime;

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

    public override bool CanPlayerCreateArmy(out TextObject disabledReason)
    {
        return _previousModel.CanPlayerCreateArmy(out disabledReason);
    }

    public override bool CheckPartyEligibility(MobileParty party, out TextObject explanation)
    {
        return _previousModel.CheckPartyEligibility(party, out explanation);
    }

    public override float DailyBeingAtArmyInfluenceAward(MobileParty armyMemberParty)
    {
        return _previousModel.DailyBeingAtArmyInfluenceAward(armyMemberParty);
    }

    public override int GetCohesionBoostInfluenceCost(Army army, int percentageToBoost = 100)
    {
        return _previousModel.GetCohesionBoostInfluenceCost(army, percentageToBoost);
    }

    public override int GetPartyRelation(Hero hero)
    {
        return _previousModel.GetPartyRelation(hero);
    }

    public override float GetPartySizeScore(MobileParty party)
    {
        return _previousModel.GetPartySizeScore(party);
    }
    #endregion
}
