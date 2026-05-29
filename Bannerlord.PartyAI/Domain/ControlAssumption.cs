using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;

namespace Bannerlord.PartyAI.Domain;

public static class ControlAssumption
{
    private static readonly TextObject TitleText = new("{=PAIFHytp3D7}Choose which parties to directly command");
    private static readonly TextObject DescriptionText = new("{=PAIRzSgh49H}Parties must be manageable and in visual range to appear here.");

    public static List<MobileParty> AssumingDirectControl = new();

    private static bool IsPopupOpen = false;

    public static bool IsUnderControlAssumption(MobileParty? party)
        => party is not null && AssumingDirectControl.Contains(party);

    public static bool IsKeyCombinationDown()
    {
        var manager = SubModule.PartySettingsManager;
        var modifierKey = manager.CommandedPartiesModiferKey;
        var mainKey = manager.CommandedPartiesKey;

        return (modifierKey == InputKey.Invalid
            || Input.IsKeyDown(modifierKey))
            && Input.IsKeyDown(mainKey);
    }

    public static void OpenPopup()
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
                AssumingDirectControl.Clear();
                foreach (InquiryElement e in results)
                {
                    if (e.Identifier is MobileParty m)
                    {
                        AssumingDirectControl.Add(m);
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
