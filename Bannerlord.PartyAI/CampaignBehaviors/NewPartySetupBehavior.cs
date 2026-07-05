using Bannerlord.PartyAI.Domain.Models;
using Bannerlord.PartyAI.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.PartyAI.CampaignBehaviors;

internal class NewPartySetupBehavior : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnMobilePartyCreated);
    }

    public override void SyncData(IDataStore dataStore)
    {
    }

    private void OnMobilePartyCreated(MobileParty mobileParty)
    {
        var hero = mobileParty?.LeaderHero;
        if (hero is null || !SubModule.PartySettingsManager.IsHeroManageable(hero))
        {
            return;
        }

        PartyAiEntitySettings settings = SubModule.PartySettingsManager.Settings(hero);

        settings.ClearAllOrders();
        settings.ResetBudgets();

        if (settings.FallbackOrder != null && settings.FallbackOrder.Behavior != PartyAiOrderType.None)
        {
            settings.SetOrder(settings.FallbackOrder.Behavior, settings.FallbackOrder.Target);
        }
    }
}
