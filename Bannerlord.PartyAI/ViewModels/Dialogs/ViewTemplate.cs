using PartyAIControls.ViewModels.Misc;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Localization;

namespace PartyAIControls.ViewModels.Dialogs
{
  internal static class ViewTemplate
  {
    public static void View()
    {
      string title = new TextObject("{=PAIlfXfoNTy}Select which template to view").ToString();

      List<InquiryElement> list = SubModule.PartySettingsManager.AllTemplates.OrderBy(t => t.Name).ToList().ConvertAll(t =>
        new InquiryElement(t, t.Name, new CharacterImageIdentifier(CampaignUIHelper.GetCharacterCode(t.Troops.First())))
      );
      MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title, string.Empty, list, isExitShown: true, 1, 1, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), ViewPartyTemplateCallback, null, "", true));
    }

    private static void ViewPartyTemplateCallback(List<InquiryElement> list)
    {
      if (list.Count == 0)
      {
        return;
      }

      PAICustomTemplate template = (PAICustomTemplate)list.First().Identifier;
      if (template == null)
      {
        return;
      }

      TroopRoster selectedTroops = TroopRoster.CreateDummyTroopRoster();
      foreach (CharacterObject c in template.Troops)
      {
        selectedTroops.AddToCounts(c, 1);
      }

      TextObject rightPartyName = new TextObject("{=PABrUnTTy9r}Troops in Template '{TEMPLATE}'").SetTextVariable("TEMPLATE", template.Name);
      VMUtilities.OpenPartyScreen(null, selectedTroops, null, rightPartyName, new TextObject("{=PAoNJx6fAYq}View Template"), null, transferableDelegate: (CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase LeftOwnerParty) => false);
    }
  }
}
