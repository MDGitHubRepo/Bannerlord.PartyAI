using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyAIControls.ViewModels.Dropdowns
{
  public class PartyAIMaxTroopTierDropdownVM : ViewModel
  {
    public class PartyAIMaxTroopTierSelectorItemVM : SelectorItemVM
    {
      public int Max { get; private set; }

      public PartyAIMaxTroopTierSelectorItemVM(TextObject s, int max)
        : base(s)
      {
        Max = max;
      }
    }

    private readonly Action<int> _onSelection;

    private SelectorVM<PartyAIMaxTroopTierSelectorItemVM> _sortOptions;

    [DataSourceProperty]
    public SelectorVM<PartyAIMaxTroopTierSelectorItemVM> SortOptions
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
    public PartyAIMaxTroopTierDropdownVM(int selectedIndex, Action<int> onSort)
    {
      _onSelection = onSort;

      SortOptions = new SelectorVM<PartyAIMaxTroopTierSelectorItemVM>(-1, OnMaxTroopTierSelected);

      SortOptions.AddItem(new PartyAIMaxTroopTierSelectorItemVM(new TextObject("{=PAIIqVpFFAi}Max"), 0));

      for (int i = 1; i <= Campaign.Current.Models.CharacterStatsModel.MaxCharacterTier; i++)
      {
        SortOptions.AddItem(new PartyAIMaxTroopTierSelectorItemVM(new TextObject("{=!}" + i.ToString()), i));
      }
      SortOptions.SelectedIndex = selectedIndex;
    }

    private void OnMaxTroopTierSelected(SelectorVM<PartyAIMaxTroopTierSelectorItemVM> selector)
    {
      _onSelection?.Invoke(selector.SelectedItem.Max);
    }
  }
}
