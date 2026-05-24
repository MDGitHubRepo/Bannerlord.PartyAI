using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using HarmonyLib;
using System.Linq;
using System.Reflection;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.Inventory;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyAIControls.UIExtenderPatches
{
  [ViewModelMixin("RefreshInformationValues")]
  internal class SPInventoryVMMixin : BaseViewModelMixin<SPInventoryVM>
  {
    private readonly SPInventoryVM _vm;
    private readonly InventoryLogic _inventoryLogic;
    private BasicTooltipViewModel _otherSideEquipmentMaxCountHint;
    private static readonly FieldInfo _inventoryLogicField = AccessTools.Field(typeof(SPInventoryVM), "_inventoryLogic");

    public SPInventoryVMMixin(SPInventoryVM vm) : base(vm)
    {
      _vm = vm;
      _inventoryLogic = (InventoryLogic)_inventoryLogicField?.GetValue(_vm);
    }

    public override void OnRefresh()
    {
      base.OnRefresh();

      if (_vm.OtherSideHasCapacity && _inventoryLogic?.OtherSideCapacityData != null && !_vm.IsTrading)
      {
        int weight;
        if (_inventoryLogic?.OtherParty?.MobileParty != null)
        {
          OtherSideEquipmentMaxCountHint = new BasicTooltipViewModel(() => CampaignUIHelper.GetPartyInventoryCapacityTooltip(_inventoryLogic?.OtherParty?.MobileParty));
          weight = MathF.Ceiling(_vm.LeftItemListVM.Where(i => !i.ItemRosterElement.EquipmentElement.Item.IsMountable && !i.ItemRosterElement.EquipmentElement.Item.IsAnimal).Sum((SPItemVM x) => x.ItemRosterElement.GetRosterElementWeight()));
        }
        else
        {
          weight = MathF.Ceiling(_vm.LeftItemListVM.Sum((SPItemVM x) => x.ItemRosterElement.GetRosterElementWeight()));
        }

        TextObject textObject = GameTexts.FindText("str_LEFT_over_RIGHT");
        int capacity = _inventoryLogic.OtherSideCapacityData.GetCapacity();
        textObject.SetTextVariable("LEFT", weight);
        textObject.SetTextVariable("RIGHT", capacity);
        _vm.OtherEquipmentCountText = textObject.ToString();
        _vm.OtherEquipmentCountWarned = weight > capacity;
        OtherSideEquipmentMaxCountHint ??= new BasicTooltipViewModel();

        _vm.IsDoneDisabled = weight > capacity;
      }
    }

    [DataSourceProperty]
    public BasicTooltipViewModel OtherSideEquipmentMaxCountHint
    {
      get
      {
        return _otherSideEquipmentMaxCountHint;
      }
      set
      {
        if (value != _otherSideEquipmentMaxCountHint)
        {
          _otherSideEquipmentMaxCountHint = value;
          OnPropertyChangedWithValue(value, "OtherSideEquipmentMaxCountHint");
        }
      }
    }
  }
}
