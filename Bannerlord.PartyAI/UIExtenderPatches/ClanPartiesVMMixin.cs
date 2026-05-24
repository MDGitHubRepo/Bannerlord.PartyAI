using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;

namespace PartyAIControls.UIExtenderPatches
{
  [ViewModelMixin(nameof(ClanPartiesVM.RefreshValues))]
  internal class ClanPartiesVMMixin : BaseViewModelMixin<ClanPartiesVM>
  {
    internal ClanPartiesVM _vm;
    internal static ClanPartiesVMMixin Instance;

    public ClanPartiesVMMixin(ClanPartiesVM vm) : base(vm)
    {
      _vm = vm;
      Instance = this;
    }
  }
}
