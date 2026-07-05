using Bannerlord.PartyAI.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.PartyAI.CampaignBehaviors;

internal class PartyDestroyedClearOrdersBehavior : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
    }

    public override void SyncData(IDataStore dataStore)
    {
    }

    private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
    {
        var hero = destroyedParty?.LeaderHero;
        if (hero is not null && SubModule.PartySettingsManager.IsHeroManageable(hero))
        {
            PartyAiEntitySettings settings = SubModule.PartySettingsManager.Settings(hero);
            settings.ClearAllOrders();
        }

        foreach (PartyAiEntitySettings settings in SubModule.PartySettingsManager.HeroesWithOrders)
        {
            var order = settings.Order;
            if (order?.Target is MobileParty targetParty && targetParty == destroyedParty)
            {
                settings.ClearOrder();
            }
        }
    }
}
