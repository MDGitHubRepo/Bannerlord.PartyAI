using Bannerlord.PartyAI.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.PartyAI.CampaignBehaviors;

internal class PreventIllegalMovesInArmyBehavior : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
    }

    private void OnHourlyTickParty(MobileParty party)
    {
        var army = party.Army;
        var hero = party.LeaderHero;
        if (army is null || !SubModule.PartySettingsManager.IsHeroManageable(hero))
        {
            return;
        }

        var settings = SubModule.PartySettingsManager.Settings(hero);

        if (ShouldLeaveArmy(army, settings))
        {
            LeaveArmy(party);
        }
    }

    public override void SyncData(IDataStore dataStore)
    {
    }

    private static bool ShouldLeaveArmy(Army army, PartyAiEntitySettings settings)
    {
        var illegalRaiding = !settings.AllowRaidVillages && IsArmyRaiding(army);
        var illegalSiege = !settings.AllowSieging && IsArmyBesieging(army);

        return settings.AllowJoinArmies || illegalRaiding || illegalSiege;
    }

    private static void LeaveArmy(MobileParty party)
    {
        RefundInfluence(party);

        party.Army = null;
    }

    private static void RefundInfluence(MobileParty party)
    {
        int influence = Campaign.Current.Models.ArmyManagementCalculationModel
            .CalculatePartyInfluenceCost(party.Army.LeaderParty, party);
        ChangeClanInfluenceAction.Apply(party.Army.LeaderParty.LeaderHero.Clan, influence);
    }

    private static bool IsArmyRaiding(Army army)
    {
        if (army == null)
        {
            return false;
        }

        return army.ArmyType == Army.ArmyTypes.Raider;
    }

    private static bool IsArmyBesieging(Army army)
    {
        if (army == null)
        {
            return false;
        }

        return army.ArmyType == Army.ArmyTypes.Besieger;
    }
}
