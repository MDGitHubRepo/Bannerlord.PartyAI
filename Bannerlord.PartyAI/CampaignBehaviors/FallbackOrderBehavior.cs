using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.PartyAI.CampaignBehaviors;

internal class FallbackOrderBehavior : CampaignBehaviorBase
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
        var fallbackOrder = settings.FallbackOrder;
        if (fallbackOrder is null
            || fallbackOrder.Behavior == PAICustomOrder.OrderType.None
            || party!.Army is null)
        {
            return;
        }

        settings.SetOrder(fallbackOrder.Behavior, fallbackOrder.Target);
    }
}
