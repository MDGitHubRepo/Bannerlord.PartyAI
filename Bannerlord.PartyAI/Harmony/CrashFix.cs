//using HarmonyLib;
//using TaleWorlds.CampaignSystem.Party;
//using TaleWorlds.CampaignSystem.ViewModelCollection.Party;

//namespace PartyAIControls.HarmonyPatches
//{
//    // Prevent PartyCharacterVM.InitializeUpgrades from crashing when there is no RightOwnerParty?
//    [HarmonyPatch(typeof(PartyCharacterVM))]
//    internal static class PartyCharacterVMCrashFix
//    {
//        [HarmonyPrefix]
//        [HarmonyPatch("InitializeUpgrades")]
//        private static bool InitializeUpgradesPrefix(
//            PartyCharacterVM __instance,
//            PartyScreenLogic ____partyScreenLogic)
//        {
//            // If it isn't even upgradable, let vanilla handle (which will just bail out)
//            if (!__instance.IsUpgradableTroop)
//                return true;

//            if (__instance.Character == null ||
//                __instance.Character.UpgradeTargets == null ||
//                __instance.Character.UpgradeTargets.Length == 0)
//            {
//                __instance.NumOfUpgradeableTroops = 0;
//                __instance.NumOfReadyToUpgradeTroops = 0;
//                __instance.IsTroopUpgradable = false;
//                __instance.IsUpgradableTroop = false;
//                __instance.StrNumOfUpgradableTroop = string.Empty;
//                return false; // skip vanilla InitializeUpgrades
//            }

//            if (____partyScreenLogic == null || ____partyScreenLogic.RightOwnerParty == null)
//            {
//                __instance.NumOfUpgradeableTroops = 0;
//                __instance.NumOfReadyToUpgradeTroops = 0;
//                __instance.IsTroopUpgradable = false;
//                __instance.IsUpgradableTroop = false;
//                __instance.StrNumOfUpgradableTroop = string.Empty;
//                return false; 
//            }

//            // In normal party screen (with a real owner party), run vanilla logic
//            return true;
//        }
//    }
//}