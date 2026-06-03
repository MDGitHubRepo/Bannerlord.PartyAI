using System.Diagnostics.CodeAnalysis;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.PartyAI.CampaignBehaviors.AiBehaviors;

public abstract class PartyAiBehaviorBase : CampaignBehaviorBase
{
    public override void RegisterEvents()
    {
        // It is unnecessary to force the consumers to override.
        // They will when they need to.
    }

    public override void SyncData(IDataStore dataStore)
    {
        // It is unnecessary to force the consumers to override
        // They will when they need to.
    }

    protected bool IsPartyOrderRelevant(
        Hero? hero,
        PAICustomOrder.OrderType orderType,
        [NotNullWhen(true)]out PartyAIClanPartySettings? settings,
        [NotNullWhen(true)]out PAICustomOrder? order)
    {
        order = null;
        settings = null;

        if (!SubModule.PartySettingsManager.IsHeroManageable(hero))
        {
            return false;
        }

        settings = SubModule.PartySettingsManager.Settings(hero);
        if (!settings.HasActiveOrder)
        {
            return false;
        }

        if (settings.Order.Behavior != orderType)
        {
            return false;
        }

        order = settings.Order;
        return true;
    }

    protected bool IsPartyOrderRelevant(
        MobileParty party,
        PAICustomOrder.OrderType orderType,
        [NotNullWhen(true)] out PartyAIClanPartySettings? settings,
        [NotNullWhen(true)] out PAICustomOrder? order)
    {
        return IsPartyOrderRelevant(party?.LeaderHero, orderType, out settings, out order);
    }

    protected void AddBehaviorScore(AIBehaviorData behaviorData, float score, PartyThinkParams thinkParams)
    {
        if (thinkParams.TryGetBehaviorScore(in behaviorData, out float previousScore))
        {
            thinkParams.SetBehaviorScore(in behaviorData, score + previousScore);
            return;
        }

        thinkParams.AddBehaviorScore((behaviorData, score));
    }
}
