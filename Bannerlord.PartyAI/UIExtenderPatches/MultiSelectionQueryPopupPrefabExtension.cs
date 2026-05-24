using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace PartyAIControls.UIExtenderPatches
{
  internal class MultiSelectionQueryPopupPrefabExtension
  {
    [PrefabExtension("MultiSelectionQueryPopup", "descendant::ListPanel[@Id='MultiSelectionContentList']/Children")]
    internal class Popups : PrefabExtensionInsertPatch
    {
      public override InsertType Type => InsertType.Child;
      public override int Index => 2;

      [PrefabExtensionFileName]
      public string PatchFileName => "MultiSelectionQueryPopupInject";
    }

    [PrefabExtension("MultiSelectionQueryPopup", "descendant::ImageIdentifierWidget[@DataSource='{ImageIdentifier}']")]
    internal class BannerInMultiSelect : PrefabExtensionInsertPatch
    {
      public override InsertType Type => InsertType.Append;

      [PrefabExtensionFileName]
      public string PatchFileName => "InquiryElementInject";
    }
  }
}
