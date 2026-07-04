using Bannerlord.PartyAI.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Bannerlord.PartyAI.CampaignBehaviors;

internal class JoiningArmyClearOrdersBehavior : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        CampaignEvents.OnPartyJoinedArmyEvent.AddNonSerializedListener(this, OnPartyJoinedArmy);
    }

    public override void SyncData(IDataStore dataStore)
    {
    }

    private void OnPartyJoinedArmy(MobileParty mobileParty)
    {
        if (mobileParty is null)
        {
            return;
        }

        var leaderHero = mobileParty.LeaderHero;

        if (leaderHero is null
            || !SubModule.PartySettingsManager.IsHeroManageable(leaderHero)
            || !SubModule.PartySettingsManager.HasActiveOrder(leaderHero))
        {
            return;
        }

        var order = SubModule.PartySettingsManager.Settings(leaderHero).Order;

        var partyText = mobileParty.Name;
        var orderText = OrderVerbalizer.GetStatusText(order);
        var armyText = mobileParty.Army?.Name is null
            ? "an army"
            : mobileParty.Army.Name.ToString();

        TextObject text = new TextObject("{=PAIOEWao2aI}{PARTY} is no longer {ORDER} because they were called to {ARMY}")
          .SetTextVariable("PARTY", partyText)
          .SetTextVariable("ORDER", orderText)
          .SetTextVariable("ARMY", armyText);

        InformationManager.DisplayMessage(new InformationMessage(text.ToString(), Colors.Magenta));

        PartyAiEntitySettings settings = SubModule.PartySettingsManager.Settings(leaderHero);
        settings.ClearAllOrders();
    }
}
