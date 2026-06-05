using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.PartyAI.CampaignBehaviors;

internal class ResetPartyAiBehavior : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
    }

    public override void SyncData(IDataStore dataStore)
    {
    }

    private void OnHourlyTickParty(MobileParty party)
    {
        var hero = party?.LeaderHero;
        if (!SubModule.PartySettingsManager.IsHeroManageable(hero))
        {
            return;
        }

        var settings = SubModule.PartySettingsManager.Settings(hero);
        if (settings.HasActiveOrder)
        {
            // It's most likely unncessary
            party.Ai.RethinkAtNextHourlyTick = true;
            party.Ai.SetDoNotMakeNewDecisions(false);
        }
    }
}
