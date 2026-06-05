using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Bannerlord.PartyAI.CampaignBehaviors;

internal class ControlAssumptionBehavior : CampaignBehaviorBase
{
    private static readonly TextObject TitleText = new("{=PAIFHytp3D7}Choose which parties to directly command");
    private static readonly TextObject DescriptionText = new("{=PAIRzSgh49H}Parties must be manageable and in visual range to appear here.");

    private bool IsPopupOpen = false;

    private List<MobileParty> _assumingDirectControl = new();

    public override void RegisterEvents()
    {
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_assumingDirectControl", ref _assumingDirectControl);
    }

    public bool IsUnderControlAssumption(MobileParty? party)
        => party is not null && _assumingDirectControl.Contains(party);

    public bool IsKeyCombinationDown()
    {
        var manager = SubModule.PartySettingsManager;
        var modifierKey = manager.CommandedPartiesModiferKey;
        var mainKey = manager.CommandedPartiesKey;

        return (modifierKey == InputKey.Invalid
            || Input.IsKeyDown(modifierKey))
            && Input.IsKeyDown(mainKey);
    }

    public void OpenPopup()
    {
        if (IsPopupOpen)
        {
            return;
        }

        CampaignTimeControlMode mode = Campaign.Current.TimeControlMode;
        Campaign.Current.TimeControlMode = CampaignTimeControlMode.FastForwardStop;

        string title = TitleText.ToString();
        string desc = DescriptionText.ToString();

        List<InquiryElement> inquiryElements = MobileParty.AllLordParties
            .Where(m => SubModule.PartySettingsManager.IsHeroManageable(m.LeaderHero)
                && IsWithinSeeingRange(m)
                && !IsInAnothersArmy(m))
            .OrderByDescending(m => m.ActualClan.Equals(Clan.PlayerClan))
            .ThenBy(m => m.Name?.ToString())
            .Select(ConvertToInquiryElement)
            .ToList();

        MBInformationManager.ShowMultiSelectionInquiry(new(
            title,
            desc,
            inquiryElements,
            isExitShown: true,
            minSelectableOptionCount: 0,
            maxSelectableOptionCount: inquiryElements.Count,
            GameTexts.FindText("str_done").ToString(),
            GameTexts.FindText("str_cancel").ToString(),
            affirmativeAction: results =>
            {
                _assumingDirectControl.Clear();
                foreach (InquiryElement e in results)
                {
                    if (e.Identifier is MobileParty m)
                    {
                        _assumingDirectControl.Add(m);
                    }
                }
                IsPopupOpen = false;
                Campaign.Current.TimeControlMode = mode;
            },
            negativeAction: results =>
            {
                IsPopupOpen = false;
                Campaign.Current.TimeControlMode = mode;
            },
            isSeachAvailable: true)
        );

        IsPopupOpen = true;
    }

    public void EscortMainParty(
        MobileParty party,
        CampaignVec2 point,
        MobileParty.NavigationType navigationType)
    {
        // Only react when:
        //  - Command key is held
        //  - This order is coming from the MAIN PARTY
        if (!Input.IsKeyDown(SubModule.PartySettingsManager.CommandPartiesKey)
            || party != MobileParty.MainParty)
        {
            return;
        }

        foreach (MobileParty controlling in _assumingDirectControl)
        {
            if (controlling?.LeaderHero == null)
                continue;
            if (!SubModule.PartySettingsManager.IsHeroManageable(controlling.LeaderHero))
                continue;
            if (controlling.MapEvent != null)
                continue;

            if (controlling.GetPosition2D.Distance(MobileParty.MainParty.GetPosition2D) >
                MobileParty.MainParty.SeeingRange)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        new TextObject("{=PAIc1pTxSOA}{NAME} is out of range to be commanded directly")
                            .SetTextVariable("NAME", controlling.Name)
                            .ToString(),
                        Colors.Magenta
                    )
                );
                continue;
            }

            // Follow / escort the main party to that point
            SetPartyAiAction.GetActionForEscortingParty(
                controlling,
                MobileParty.MainParty,
                controlling.DesiredAiNavigationType,
                false,
                false
            );

            if (controlling.Ai != null)
                controlling.Ai.SetDoNotMakeNewDecisions(true);

            PartyAIClanPartySettings settings =
                SubModule.PartySettingsManager.Settings(controlling.LeaderHero);
            settings.OrderQueue.Clear();
            settings.ClearOrder();
            settings.SetOrder(PAICustomOrder.OrderType.EscortParty, MobileParty.MainParty);
        }
    }

    public void AttackOrEscortParty(
        MobileParty party,
        MobileParty target)
    {
        if (!Input.IsKeyDown(SubModule.PartySettingsManager.CommandPartiesKey)
            || party != MobileParty.MainParty)
        {
            return;
        }

        foreach (MobileParty controlling in _assumingDirectControl)
        {
            if (controlling?.LeaderHero == null)
                continue;
            if (!SubModule.PartySettingsManager.IsHeroManageable(controlling.LeaderHero))
                continue;
            if (controlling.MapEvent != null)
                continue;

            if (controlling.GetPosition2D.Distance(MobileParty.MainParty.GetPosition2D) >
                MobileParty.MainParty.SeeingRange)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        new TextObject("{=PAIc1pTxSOA}{NAME} is out of range to be commanded directly")
                            .SetTextVariable("NAME", controlling.Name)
                            .ToString(),
                        Colors.Magenta
                    )
                );
                continue;
            }

            if (controlling.Ai != null)
                controlling.Ai.SetDoNotMakeNewDecisions(true);

            PartyAIClanPartySettings settings =
                SubModule.PartySettingsManager.Settings(controlling.LeaderHero);
            settings.OrderQueue.Clear();
            settings.ClearOrder();

            if (FactionManager.IsAtWarAgainstFaction(target.MapFaction, controlling.MapFaction))
            {
                // Attack enemy party
                SetPartyAiAction.GetActionForEngagingParty(
                    controlling,
                    target,
                    controlling.DesiredAiNavigationType,
                    false
                );
                settings.SetOrder(PAICustomOrder.OrderType.AttackParty, target);
            }
            else
            {
                // Escort non-hostile party
                SetPartyAiAction.GetActionForEscortingParty(
                    controlling,
                    target,
                    controlling.DesiredAiNavigationType,
                    false,
                    false
                );
                settings.SetOrder(PAICustomOrder.OrderType.EscortParty, target);
            }
        }
    }


    public void TargetSettlement(
        MobileParty party,
        Settlement settlement)
    {
        if (!Input.IsKeyDown(SubModule.PartySettingsManager.CommandPartiesKey) ||
            party != MobileParty.MainParty)
        {
            return;
        }

        foreach (MobileParty controlling in _assumingDirectControl)
        {
            if (controlling?.LeaderHero == null)
                continue;
            if (!SubModule.PartySettingsManager.IsHeroManageable(controlling.LeaderHero))
                continue;
            if (controlling.MapEvent != null)
                continue;

            if (controlling.GetPosition2D.Distance(MobileParty.MainParty.GetPosition2D) >
                MobileParty.MainParty.SeeingRange)
            {
                InformationManager.DisplayMessage(
                    new InformationMessage(
                        new TextObject("{=PAIc1pTxSOA}{NAME} is out of range to be commanded directly")
                            .SetTextVariable("NAME", controlling.Name)
                            .ToString(),
                        Colors.Magenta
                    )
                );
                continue;
            }

            if (controlling.Ai != null)
                controlling.Ai.SetDoNotMakeNewDecisions(true);

            PartyAIClanPartySettings settings =
                SubModule.PartySettingsManager.Settings(controlling.LeaderHero);
            settings.OrderQueue.Clear();
            settings.ClearOrder();

            if (FactionManager.IsAtWarAgainstFaction(settlement.MapFaction, controlling.MapFaction))
            {
                // Enemy settlement → besiege
                SetPartyAiAction.GetActionForBesiegingSettlement(
                    controlling,
                    settlement,
                    controlling.DesiredAiNavigationType,
                    false
                );
                settings.SetOrder(PAICustomOrder.OrderType.BesiegeSettlement, settlement);
            }
            else
            {
                if (settlement.IsUnderSiege)
                {
                    // Friendly settlement under siege → defend
                    SetPartyAiAction.GetActionForDefendingSettlement(
                        controlling,
                        settlement,
                        controlling.DesiredAiNavigationType,
                        false,
                        false
                    );
                    settings.SetOrder(PAICustomOrder.OrderType.DefendSettlement, settlement);
                }
                else
                {
                    // Normal case → just visit
                    SetPartyAiAction.GetActionForVisitingSettlement(
                        controlling,
                        settlement,
                        controlling.DesiredAiNavigationType,
                        false,
                        false
                    );

                    // If your OrderType enum has a VisitSettlement value,
                    // swap this to that to perfectly match original behavior.
                    settings.SetOrder(PAICustomOrder.OrderType.DefendSettlement, settlement);
                }
            }
        }
    }

    private static InquiryElement ConvertToInquiryElement(MobileParty mobileParty)
    {
        var characterCode = CharacterCode.CreateFrom(mobileParty.LeaderHero?.CharacterObject);
        var imageIdentifier = new CharacterImageIdentifier(characterCode);

        return new InquiryElement(
            identifier: mobileParty,
            title: mobileParty.Name.ToString(),
            imageIdentifier: imageIdentifier);
    }

    private static bool IsWithinSeeingRange(MobileParty mobileParty)
    {
        var distance = mobileParty.GetPosition2D.Distance(MobileParty.MainParty.GetPosition2D);
        return distance <= MobileParty.MainParty.SeeingRange;
    }

    private static bool IsInAnothersArmy(MobileParty mobileParty)
    {
        return mobileParty.Army is not null && mobileParty.Army.LeaderParty != mobileParty;
    }
}
