using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;

namespace PartyAIControls.UIExtenderPatches
{

  internal sealed class ClanPartiesPrefabExtension
  {
    [PrefabExtension("ClanPartiesRightPanel", "descendant::Widget[@SuggestedWidth='465']")]
    internal class ControlsOverlay : PrefabExtensionInsertPatch
    {
      public override InsertType Type => InsertType.Append;

      [PrefabExtensionFileName]
      public string PatchFileName => "ClanPartiesInject";
    }
  }
}
