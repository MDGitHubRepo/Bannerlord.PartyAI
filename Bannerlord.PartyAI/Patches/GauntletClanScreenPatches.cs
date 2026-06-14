using Bannerlord.PartyAI.Mixins;
using HarmonyLib;
using HarmonyLib.PatchBuilder;
using SandBox.GauntletUI;
using TaleWorlds.CampaignSystem.GameState;

namespace Bannerlord.PartyAI.Patches;

internal class GauntletClanScreenPatches
{
    public static void Apply(Harmony harmony)
    {
        harmony.Patch<GauntletClanScreen>()
            .Method("OnActivate")
                .Postfix(OnActivatePrefix);
    }

    private static void OnActivatePrefix(GauntletClanScreen __instance)
    {
        if (ClanPartyItemVMMixin.SelectedParty != null)
        {
            ClanState state = (ClanState)AccessTools.Field(typeof(GauntletClanScreen), "_clanState").GetValue(__instance);
            AccessTools.Property(state.GetType(), "InitialSelectedParty").SetValue(state, ClanPartyItemVMMixin.SelectedParty);
            ClanPartyItemVMMixin.SelectedParty = null;
        }
    }
}
