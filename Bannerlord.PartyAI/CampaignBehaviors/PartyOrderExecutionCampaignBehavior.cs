using Bannerlord.PartyAI.Domain;
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
    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_assumingDirectControl", ref ControlAssumption.AssumingDirectControl);
    }

    public override void RegisterEvents()
    {
        CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, OnHourlyTickParty);
    }

    private void OnHourlyTickParty(MobileParty party)
    {
        if (party?.LeaderHero == null
            || !SubModule.PartySettingsManager.IsHeroManageable(party.LeaderHero))
        {
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

        
        if (settings.HasActiveOrder)
        {
            switch (settings.Order.Behavior)
            {
                default:
                    ResetPartyAi(party);
                    return;
            }
        }
    }

    private void ResetPartyAi(MobileParty party)
    {
        party.Ai.RethinkAtNextHourlyTick = true;
        party.Ai.SetDoNotMakeNewDecisions(false);
    }
}
