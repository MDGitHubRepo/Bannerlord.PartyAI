using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;

namespace Bannerlord.PartyAI.HarmonyPatches;

internal class InventoryLogicPatches
{
    public static void Apply(Harmony harmony)
    {
        harmony.Patch<InventoryLogic>()
            .Postfix(x => x.CanGainXpFromDiscarding, CanGainXpFromDiscardingPostfix);
    }

    // PreventDonationExploit
    private static void CanGainXpFromDiscardingPostfix(InventoryLogic __instance, ref bool __result)
    {
        if (__instance?.LeftMemberRoster == null)
        {
            return;
        }

        // TODO cache this
        PartyBase owner = (PartyBase)AccessTools
            .Property(typeof(TroopRoster), "OwnerParty")
            .GetValue(__instance.LeftMemberRoster);

        if (owner?.MobileParty?.ActualClan == Clan.PlayerClan)
        {
            __result = false;
        }
    }
}
