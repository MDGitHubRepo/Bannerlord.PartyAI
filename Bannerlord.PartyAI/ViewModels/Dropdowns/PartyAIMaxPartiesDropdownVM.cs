using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyAIControls.ViewModels.Dropdowns
{
  public class PartyAIMaxPartiesDropdownVM : ViewModel
  {
    public class PartyAIMaxPartiesSelectorItemVM : SelectorItemVM
    {
      public int Max { get; private set; }

      public PartyAIMaxPartiesSelectorItemVM(TextObject s, int max)
        : base(s)
      {
        Max = max;
      }
    }

    private readonly Action<int> _onSelection;

    private SelectorVM<PartyAIMaxPartiesSelectorItemVM> _sortOptions;

    [DataSourceProperty]
    public SelectorVM<PartyAIMaxPartiesSelectorItemVM> SortOptions
    {
      get
      {
        return _sortOptions;
      }
      set
      {
        if (value != _sortOptions)
        {
          _sortOptions = value;
          OnPropertyChangedWithValue(value, "SortOptions");
        }
      }
    }
    public PartyAIMaxPartiesDropdownVM(Action<int> onSort)
    {
      _onSelection = onSort;

      SortOptions = new SelectorVM<PartyAIMaxPartiesSelectorItemVM>(-1, OnMaxPartiesSelected);

      SortOptions.AddItem(new PartyAIMaxPartiesSelectorItemVM(new TextObject("{=PAIIqVpFFAi}Max"), 0));

      for (int i = 1; i <= Clan.PlayerClan.CommanderLimit || i <= SubModule.PartySettingsManager.AutoCreateClanPartiesMax; i++)
      {
        SortOptions.AddItem(new PartyAIMaxPartiesSelectorItemVM(new TextObject("{=!}" + i.ToString()), i));
      }
      SortOptions.SelectedIndex = SubModule.PartySettingsManager.AutoCreateClanPartiesMax;
    }

    private void OnMaxPartiesSelected(SelectorVM<PartyAIMaxPartiesSelectorItemVM> selector)
    {
      _onSelection?.Invoke(selector.SelectedItem.Max);
    }
  }
}
