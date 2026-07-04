using Bannerlord.PartyAI.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.PartyAI.CampaignBehaviors;

internal class PrisonerClearOrdersBehavior : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnHeroPrisonerTaken);
    }

    public override void SyncData(IDataStore dataStore)
    {
    }

    private void OnHeroPrisonerTaken(PartyBase party, Hero prisoner)
    {
        if (SubModule.PartySettingsManager.IsHeroManageable(prisoner))
        {
            PartyAiEntitySettings settings = SubModule.PartySettingsManager.Settings(prisoner);
            settings.ClearAllOrders();
        }
    }
}
