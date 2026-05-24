using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyAIControls.ViewModels.MenuItemVMs
{
  public class PartyAIControlsMenuCaravanVM : PartyAIControlsMenuPartyVM
  {
    private readonly PartyAIControlsMenuVM _menu;

    public PartyAIControlsMenuCaravanVM(Hero leader, PartyAIControlsMenuVM menu) : base(leader, menu)
    {
      _menu = menu;
    }
    [DataSourceProperty] public override bool IsCaravan => true;
    [DataSourceProperty] public override bool IsLordParty => false;
    [DataSourceProperty] public override string ActiveOrder => Party.MobileParty.PartyTradeGold.ToString();
    [DataSourceProperty] public override string LeaderName => Party.Name.ToString();

    public override void EditPartyOptions() => SubModule.InformationManager.ShowCaravanOptionsInquiry(Settings, RefreshValues);

    public override void RefreshValues()
    {
      base.RefreshValues();

      if (SubModule.PartySettingsManager.AllowTroopConversion || SubModule.PartySettingsManager.AllowCaravanConversion(Leader))
      {
        EditCompositionHint = new(new TextObject("{=PAIQNUqwt4C}Edit"));
        ChangeTemplateHint = new(new TextObject("{=PAIXIv9UgAt}Change"));
        AllowEditComposition = true;
        AllowEditTemplate = true;
      }
      else
      {
        EditCompositionHint = new(new TextObject("{=PAIIAZIrPw0}These values are not useful for caravans unless you enable troop conversion in the mod options."));
        ChangeTemplateHint = new(new TextObject("{=PAIQAHEYKZ2}Templates are not useful for caravans unless you enable troop conversion in the mod options."));
        AllowEditComposition = false;
        AllowEditTemplate = false;
      }
      OnPropertyChanged("EditCompositionHint");
      OnPropertyChanged("ChangeTemplateHint");
      OnPropertyChanged("AllowEditComposition");
      OnPropertyChanged("AllowEditTemplate");
    }
  }
}
