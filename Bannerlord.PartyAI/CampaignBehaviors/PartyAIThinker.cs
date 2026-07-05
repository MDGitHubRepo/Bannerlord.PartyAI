using Bannerlord.PartyAI.Domain.Models;
using Bannerlord.PartyAI.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.PartyAI.CampaignBehaviors;

internal class PartyAIThinker : CampaignBehaviorBase
{
    private readonly ControlAssumptionBehavior _controlAssumptionBehavior;

    public PartyAIThinker(ControlAssumptionBehavior controlAssumptionBehavior)
    {
        _controlAssumptionBehavior = controlAssumptionBehavior;
    }

    public override void SyncData(IDataStore dataStore)
    {
    }

    public override void RegisterEvents()
    {
        CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
        CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnMobilePartyCreated);
    }

    private void OnMobilePartyDestroyed(MobileParty mobileParty, PartyBase destroyerParty)
    {
        if (mobileParty?.LeaderHero != null && SubModule.PartySettingsManager.IsHeroManageable(mobileParty.LeaderHero))
        {
            PartyAiEntitySettings settings = SubModule.PartySettingsManager.Settings(mobileParty.LeaderHero);
            settings.ClearAllOrders();
        }

        foreach (PartyAiEntitySettings settings in SubModule.PartySettingsManager.HeroesWithOrders)
        {
            PartyAiOrder order = settings.Order;
            switch (order.Behavior)
            {
                case PartyAiOrderType.AttackParty:
                case PartyAiOrderType.EscortParty:
                    if (order.Target is not MobileParty m || m != mobileParty)
                    {
                        continue;
                    }
                    settings.ClearOrder();
                    if (_controlAssumptionBehavior.IsUnderControlAssumption(settings.Hero?.PartyBelongedTo))
                    {
                        settings.SetOrder(PartyAiOrderType.EscortParty, MobileParty.MainParty);
                        MobileParty escortingParty = settings.Hero?.PartyBelongedTo;
                        if (escortingParty != null)
                        {
                            SetPartyAiAction.GetActionForEscortingParty(
                                escortingParty,
                                MobileParty.MainParty,
                                MobileParty.MainParty.DesiredAiNavigationType,
                                false,
                                false);
                            escortingParty.Ai.SetDoNotMakeNewDecisions(true);
                        }
                        else
                        {
                            settings.ClearOrder();
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private void OnMobilePartyCreated(MobileParty mobileParty)
    {
        if (mobileParty?.LeaderHero == null) return;
        if (SubModule.PartySettingsManager.IsHeroManageable(mobileParty.LeaderHero))
        {
            PartyAiEntitySettings settings = SubModule.PartySettingsManager.Settings(mobileParty.LeaderHero);

            settings.ClearAllOrders();
            settings.ResetBudgets();

            if (settings.FallbackOrder != null && settings.FallbackOrder.Behavior != PartyAiOrderType.None)
            {
                settings.SetOrder(settings.FallbackOrder.Behavior, settings.FallbackOrder.Target);
            }
        }
    }

}
