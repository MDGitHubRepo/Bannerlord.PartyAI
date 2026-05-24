using PartyAIControls.ViewModels.Misc;
using System;
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
  internal static class FineTune
  {
    internal static void Tune()
    {
      string title = new TextObject("{=PAITCqVHXaz}Select which template to fine tune").ToString();

      List<InquiryElement> list = SubModule.PartySettingsManager.AllTemplates.OrderBy(t => t.Name).ToList().ConvertAll(t =>
        new InquiryElement(t, t.Name, new CharacterImageIdentifier(CampaignUIHelper.GetCharacterCode(t.Troops.First())))
      );
      MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(title, string.Empty, list, isExitShown: true, 1, 1, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(), delegate (List<InquiryElement> list)
      {
        if (list.Count == 0) { return; }

        if (list.First().Identifier is PAICustomTemplate template)
        {
          TroopRoster selectedTroops = TroopRoster.CreateDummyTroopRoster();
          foreach (CharacterObject c in template.Troops)
          {
            selectedTroops.AddToCounts(c, 1);
          }

          TroopRoster left = TroopRoster.CreateDummyTroopRoster();
          template.ResolveTroops().Except(template.Troops).ToList().ForEach(t =>
          {
            left.AddToCounts(t, 1);
          });

          TextObject rightPartyName = new TextObject("{=PABrUnTTy9r}Troops in Template '{TEMPLATE}'").SetTextVariable("TEMPLATE", template.Name);
          VMUtilities.OpenPartyScreen(left, selectedTroops, null, rightPartyName, new TextObject("{=PAIxE5LIta2}Fine Tune Template"), delegate (TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, FlattenedTroopRoster takenPrisonerRoster, FlattenedTroopRoster releasedPrisonerRoster, bool isForced, PartyBase leftParty, PartyBase rightParty)
          {
            template.Troops = rightMemberRoster.ToFlattenedRoster().ToList().ConvertAll(c => c.Troop).Distinct().ToList();
            return true;
          }, FineTuneRosterCheck);
        }
      }, null, "", true));
    }

    private static Tuple<bool, TextObject> FineTuneRosterCheck(TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, TroopRoster rightMemberRoster, TroopRoster rightPrisonRoster, int leftLimitNum, int rightLimitNum)
    {
      Tuple<bool, TextObject> result = VMUtilities.IsTemplateRosterValid(leftMemberRoster, leftPrisonRoster, rightMemberRoster, rightPrisonRoster, leftLimitNum, rightLimitNum);
      if (!result.Item1) { return result; }

      string errors = string.Empty;
      List<CharacterObject> selected = rightMemberRoster.GetTroopRoster().ConvertAll(t => t.Character);
      List<CharacterObject> notSelected = leftMemberRoster.GetTroopRoster().ConvertAll(t => t.Character);

      foreach (CharacterObject character in selected)
      {
        if (character.UpgradeTargets?.Length > 0)
        {
          foreach (CharacterObject target in selected)
          {
            if (character == target) { continue; }
            if (!PAICustomTemplate.UpgradesTo(character, target)) { continue; }
            if (selected.Any(c => c.UpgradeTargets.Contains(target))) { continue; }
            if (character.UpgradeTargets.Any(c => selected.Contains(c) && PAICustomTemplate.UpgradesTo(c, target))) { continue; }

            if (!string.IsNullOrEmpty(errors)) { errors += Environment.NewLine; }
            errors += new TextObject("{=PAI5D5Ofcoo}No upgrade path between {CHARACTER} and {TARGET}").SetTextVariable("CHARACTER", character.Name).SetTextVariable("TARGET", target.Name);
          }
        }

        if (notSelected.Any(c => c.UpgradeTargets.Contains(character)) && !selected.Any(c => c.UpgradeTargets.Contains(character)) && !selected.Any(c => c != character && PAICustomTemplate.UpgradesTo(c, character)))
        {
          if (!string.IsNullOrEmpty(errors)) { errors += Environment.NewLine; }
          errors += new TextObject("{=PAI91kcf3yB}Must select at least one troop that upgrades to {CHARACTER}").SetTextVariable("CHARACTER", character.Name);
        }
      }

      if (!string.IsNullOrEmpty(errors))
      {
        result = new(false, new TextObject(errors));
      }

      return result;
    }
  }
}