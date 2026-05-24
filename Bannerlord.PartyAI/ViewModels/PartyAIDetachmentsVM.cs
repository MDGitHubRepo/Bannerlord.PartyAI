using PartyAIControls.ViewModels.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyAIControls.ViewModels
{
  internal class PartyAIDetachmentsVM : ViewModel
  {
    private readonly Action _onClose;
    private bool _isVisible;

    public PartyAIDetachmentsVM(Action callback)
    {
      TitleText = new TextObject("{=PAIgAsElRcK}Detatchment Management").ToString();
      IsVisible = true;

      _onClose = callback;

      RefreshValues();
    }

    [DataSourceProperty] public string AcceptText => GameTexts.FindText("str_done").ToString();
    [DataSourceProperty] public string CreateDetachmentText => new TextObject("{=PAYjAC3mQzN}Create").ToString();
    [DataSourceProperty] public string TitleText { get; private set; }
    [DataSourceProperty]
    public bool IsVisible
    {
      get
      {
        return _isVisible;
      }
      set
      {
        if (value != _isVisible)
        {
          _isVisible = value;
          OnPropertyChangedWithValue(value, "IsVisible");
        }
      }
    }

    public void CreateDetachment()
    {
      string title = new TextObject("{=PAIdmeoGhXk}Choose Parent Party").ToString();
      string desc = new TextObject("{=PAIALZhTNO9}Choose which party the new detachment will detach from").ToString();
      List<InquiryElement> partyList = PartyAIControlsMenuVM.Instance.PartyList.Where(p => !p.IsCaravan).ToList().ConvertAll(p =>
      {
        if (p.Party.MobileParty.IsGarrison)
        {
          return new InquiryElement(p.Party.MobileParty, p.Party.Name.ToString(), new BannerImageIdentifier(p.Party.Owner.ClanBanner, false));
        }
          CharacterCode code = CharacterCode.CreateFrom(p.Party.Owner.CharacterObject);
          return new InquiryElement(p.Party.MobileParty, p.Party.Name.ToString(), new CharacterImageIdentifier(code));
      });
      IsVisible = false;

      MBInformationManager.ShowMultiSelectionInquiry(new(title, desc, partyList, true, 1, 1, GameTexts.FindText("str_next").ToString(), GameTexts.FindText("str_cancel").ToString(), (List<InquiryElement> results) =>
      {
        if (results.FirstOrDefault()?.Identifier is not MobileParty party) { return; }

        List<InquiryElement> typeList = new();
        IMapPoint target = party.IsGarrison ? party.CurrentSettlement : party;
        typeList.Add(new(new PAIDetatchmentConfig(PAIDetatchmentConfig.DetatchmentType.Recruiter, target), new TextObject("{=PAINxQlJwm0}Recruiter").ToString(), null));

        MBInformationManager.ShowMultiSelectionInquiry(new(new TextObject("{=PAIlhut21a1}Choose Detatchment Type").ToString(),
          string.Empty, typeList, true, 1, 1, GameTexts.FindText("str_next").ToString(), GameTexts.FindText("str_cancel").ToString(), (List<InquiryElement> config) =>
          {
            VMUtilities.OpenPartyScreen(TroopRoster.CreateDummyTroopRoster(), party.MemberRoster,
              leftPartyName: new TextObject("{=PAIduQc8nb6}Select troops for detachment"),
              header: new TextObject("{=PAIUP5cqkl4}Create Detachment"),
              rightParty: party.Party,
              doneCondition: (TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum) =>
              {
                return new(true, new TextObject(""));
              },
              doneDelegate: (TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty, PartyBase rightParty) =>
              {
                SubModule.DetatchmentManager.CreateNewDetatchment(leftMemberRoster, config.First().Identifier as PAIDetatchmentConfig);
                IsVisible = true;
                return true;
              },
              cancelDelegate: () =>
              {
                IsVisible = true;
              });
          }, (_) => IsVisible = true));
      }, (_) => IsVisible = true, isSeachAvailable: true));
    }

    public void Accept() => _onClose?.Invoke();
  }
}
