using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Map.MapBar;
using TaleWorlds.Localization;

namespace PartyAIControls.HarmonyPatches
{
  [HarmonyPatch(typeof(MapBarVM), "UpdateCanGatherArmyAndReason")]
  internal class MapBarVMPatches
  {
    private static readonly bool _bannerKingsLoaded = AccessTools.TypeByName("BannerKings.Main") != null;

    private static void Postfix(MapBarVM __instance)
    {
      if (_bannerKingsLoaded)
      {
        if (Clan.PlayerClan.Kingdom == null || Clan.PlayerClan.IsUnderMercenaryService)
        {
          __instance.GatherArmyHint = new(new("{=PAIoLtvzpKU}PartyAIControls: Feature to enable armies without being a Vassal is not compatible with BannerKings."));
        }
        return;
      }

      IFaction mapFaction = Hero.MainHero.MapFaction;

      if (mapFaction != null && !mapFaction.IsKingdomFaction)
      {
        __instance.CanGatherArmy = true;
        __instance.GatherArmyHint.HintText = new TextObject("");
      }
      else if (Clan.PlayerClan.IsUnderMercenaryService)
      {
        __instance.CanGatherArmy = true;
        __instance.GatherArmyHint.HintText = new TextObject("");
      }
    }
  }
}
