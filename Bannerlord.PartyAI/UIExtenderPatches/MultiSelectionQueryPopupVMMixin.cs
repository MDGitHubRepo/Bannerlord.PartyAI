using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using PartyAIControls.ViewModels;
using PartyAIControls.ViewModels.MenuOptionVMs;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Inquiries;

namespace PartyAIControls.UIExtenderPatches
{
  [ViewModelMixin(nameof(MultiSelectionQueryPopUpVM.SetData))]
  internal class MultiSelectionQueryPopupVMMixin : BaseViewModelMixin<MultiSelectionQueryPopUpVM>
  {
    private readonly MultiSelectionQueryPopUpVM _vm;
    private string _selectAllText;
    private bool _isSelectAllVisible;
    internal static bool AddClanBanners;

    public MultiSelectionQueryPopupVMMixin(MultiSelectionQueryPopUpVM vm) : base(vm)
    {
      _vm = vm;

      SelectAllText = new TextObject("{=PAIxKOXkgPU}Select All").ToString();

      OnRefresh();
    }

    [DataSourceProperty]
    public string SelectAllText
    {
      get
      {
        return _selectAllText;
      }
      set
      {
        if (value != _selectAllText)
        {
          _selectAllText = value;
          OnPropertyChangedWithValue(value, "SelectAllText");
        }
      }
    }

    [DataSourceProperty]
    public bool IsSelectAllVisible
    {
      get
      {
        return _isSelectAllVisible;
      }
      set
      {
        if (value != _isSelectAllVisible)
        {
          _isSelectAllVisible = value;
          OnPropertyChangedWithValue(value, "IsSelectAllVisible");
        }
      }
    }

    [DataSourceMethod]
    public void SelectAll()
    {
      foreach (InquiryElementVM e in _vm.InquiryElements)
      {
        if (e.IsEnabled)
        {
          e.IsSelected = true;
          e.RefreshValues();
        }
      }
    }

    public override void OnRefresh()
    {
      base.OnRefresh();

      if (PartyAIModOptionsVM.IsAutoCreatePartyLeaderRosterSelection && _vm?.InquiryElements?.FirstOrDefault()?.InquiryElement?.Identifier is Hero)
      {
        foreach (InquiryElementVM e in _vm.InquiryElements)
        {
          if (PartyAIModOptionsVM.ChosenPartyLeaders.Contains((Hero)e.InquiryElement.Identifier))
          {
            e.IsSelected = true;
            e.RefreshValues();
          }
        }
      }
      PartyAIModOptionsVM.IsAutoCreatePartyLeaderRosterSelection = false;

      if (PartyAICaravanOptionsVM.IsSelectFilteredSettlements && _vm?.InquiryElements?.FirstOrDefault()?.InquiryElement?.Identifier is Settlement)
      {
        foreach (InquiryElementVM e in _vm.InquiryElements)
        {
          if (PartyAICaravanOptionsVM.FilteredSettlements.Contains(e.InquiryElement.Identifier as Settlement))
          {
            e.IsSelected = true;
            e.RefreshValues();
          }
        }
      }
      PartyAICaravanOptionsVM.IsSelectFilteredSettlements = false;

      AddClanBanners = false;

      IsSelectAllVisible = _vm.InquiryElements.Count <= _vm.MaxSelectableOptionCount && _vm.InquiryElements.Count > 1 && _vm.MaxSelectableOptionCount - _vm.MinSelectableOptionCount > 1;

      _vm.SearchText = string.Empty;
    }
  }
}
