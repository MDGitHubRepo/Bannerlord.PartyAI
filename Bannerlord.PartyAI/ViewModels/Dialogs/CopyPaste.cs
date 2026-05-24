using PartyAIControls.UIExtenderPatches;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Localization;
using static PartyAIControls.PAICustomOrder;

namespace PartyAIControls.ViewModels.Dialogs
{
  internal static class CopyPaste
  {
    private static PartyAIClanPartySettings _source;
    private static List<InquiryElement> _copySources;
    private static Action _callback;
    private static bool _showAll;
    private static bool _clanOnly;

    public static void CopyTo(Hero hero, Action callback = null)
    {
      _callback = callback;
      _showAll = false;
      _clanOnly = true;

      if (hero == null) { return; }

      CopyCallback(SubModule.PartySettingsManager.Settings(hero));
    }

    public static void CopyGarrisonTo(Settlement settlement, Action callback = null)
    {
      _callback = callback;

      if (settlement == null) { return; }

      CopyCallback(SubModule.PartySettingsManager.Settings(settlement));
    }

    internal static void CopyCallback(PartyAIClanPartySettings source, Action<List<InquiryElement>> callback = null)
    {
      if (source == null) { return; }
      callback ??= ChooseCopyTypeCallback;

      _source = source;

      string title = new TextObject("{=PAIEv0gLuYi}Select which settings you would like to copy.").ToString();
      string description = new TextObject("{=PAIZZEi6e9F}You may select more than one option.").ToString();

      List<InquiryElement> newList = new();
      string CompositionText = new TextObject("{=PAI42PrfM04}Party Composition").ToString();
      string TemplateText = new TextObject("{=PAIrkbpwijb}Template").ToString();
      string OrderText = new TextObject("{=PAI6XKZojTt}Order").ToString();
      string OptionsText = new TextObject("{=PAIQnwbXcqc}Options").ToString();
      TextObject hint = new TextObject("{=!}{HERO}'s {OPTION}").SetTextVariable("HERO", _source.Hero != null ? _source.Hero.Name : _source.Settlement.Name);

      newList.Add(new InquiryElement(source.Composition, CompositionText, null, true, hint.SetTextVariable("OPTION", CompositionText).ToString()));
      newList.Add(new InquiryElement(source.PartyTemplate, TemplateText, null, true, hint.SetTextVariable("OPTION", TemplateText).ToString()));
      if (_source.Settlement == null)
      {
        if (!SubModule.PartySettingsManager.IsCaravanManageable(_source.Hero))
        {
          newList.Add(new InquiryElement(source.Order ?? new PAICustomOrder(null, OrderType.None), OrderText, null, true, hint.SetTextVariable("OPTION", OrderText).ToString()));
        }
        newList.Add(new InquiryElement(source, OptionsText, null, true, hint.SetTextVariable("OPTION", OptionsText).ToString()));
      }

      MultiSelectionQueryPopupVMMixin.AddClanBanners = true;
      MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title, description, newList, isExitShown: true, 1, newList.Count, GameTexts.FindText("str_next").ToString(), GameTexts.FindText("str_cancel").ToString(), callback, null));
    }

    private static void ChooseCopyTypeCallback(List<InquiryElement> list)
    {
      if (list.Count == 0) { return; }

      _copySources = list;

      string title = new TextObject("{=PAIdpg5Dset}Select which parties to copy settings to").ToString();

      List<Hero> heroList = new();
      List<InquiryElement> newList;

      if (SubModule.PartySettingsManager.IsCaravanManageable(_source.Hero))
      {
        heroList = Clan.PlayerClan.Heroes.Where(h => SubModule.PartySettingsManager.IsCaravanManageable(h) && h != _source.Hero).ToList();
      }
      else if (_source.Settlement != null)
      {
        newList = Clan.PlayerClan.Settlements
                    .Where(s => SubModule.PartySettingsManager.IsGarrisonManageable(s) && s != _source.Settlement)
                    .ToList()
                    .ConvertAll(s =>
          new InquiryElement(
              s, 
              s.Name.ToString(), 
              new BannerImageIdentifier(s.OwnerClan.Banner, false))
        );
        goto done;
      }
      else if (_clanOnly)
      {
        PartyAIControlsMenuVM.GetManageableHeroes(heroList, _clanOnly, _showAll);
        heroList = heroList.Where(h => h != _source.Hero && !(h.PartyBelongedTo?.IsCaravan ?? false)).ToList();
      }
      else
      {
        heroList = PartyAIControlsMenuVM.Instance.PartyList.ToList().ConvertAll(p => p.Leader).Where(h => h != _source.Hero).ToList();
      }

      newList = heroList.ConvertAll(p =>
        new InquiryElement(
            p, 
            p.Name.ToString(), 
            new CharacterImageIdentifier(CharacterCode.CreateFrom(p.CharacterObject)), 
            true, 
            p.Clan.Name.ToString())
      );

    done:
      MultiSelectionQueryPopupVMMixin.AddClanBanners = true;
      MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title, string.Empty, newList, isExitShown: true, 1, 5000, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), ChooseDestinationPartiesCallback, null, "", true));
    }

    private static void ChooseDestinationPartiesCallback(List<InquiryElement> list)
    {
      foreach (InquiryElement p in list)
      {
        PartyAIClanPartySettings settings = p.Identifier is Settlement ? SubModule.PartySettingsManager.Settings((Settlement)p.Identifier) : SubModule.PartySettingsManager.Settings((Hero)p.Identifier);
        foreach (InquiryElement source in _copySources)
        {
          CopySettings(settings, source);
        }
      }

      _callback?.Invoke();
    }

    internal static void CopySettings(PartyAIClanPartySettings settings, InquiryElement source)
    {
      if (source.Identifier is PartyCompositionObect)
      {
        settings.Composition = ((PartyCompositionObect)source.Identifier).Clone();
      }

      if (source.Identifier is PAICustomTemplate)
      {
        PAICustomTemplate template = (PAICustomTemplate)source.Identifier;
        settings.SetPartyTemplate(template);
      }

      if (source.Identifier is PAICustomOrder)
      {
        PAICustomOrder order = (PAICustomOrder)source.Identifier;
        if (order.Behavior == OrderType.None)
        {
          settings.ClearOrder();
        }
        settings.SetOrder(order);
      }

      if (source.Identifier is PartyAIClanPartySettings)
      {
        settings.CopyOptionsFrom((PartyAIClanPartySettings)source.Identifier);
      }

      if (source.Identifier == null)
      {
        settings.SetPartyTemplate(null);
      }
    }
  }
}
