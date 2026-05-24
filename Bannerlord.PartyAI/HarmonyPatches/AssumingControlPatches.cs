using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static PartyAIControls.PAICustomOrder;

namespace PartyAIControls.HarmonyPatches
{
    [HarmonyPatch]
    internal class AssumingControlPatches
    {
        // === GO TO POINT (CLICK ON MAP) ===
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MobileParty), "SetMoveGoToPoint")]
        private static void SetMoveGoToPoint(
            MobileParty __instance,
            CampaignVec2 point,
            MobileParty.NavigationType navigationType
        )
        {
            // Only react when:
            //  - Command key is held
            //  - This order is coming from the MAIN PARTY
            if (!Input.IsKeyDown(SubModule.PartySettingsManager.CommandPartiesKey) ||
                __instance != MobileParty.MainParty)
            {
                return;
            }

            foreach (MobileParty controlling in SubModule.PartyThinker.AssumingDirectControl)
            {
                if (controlling?.LeaderHero == null)
                    continue;
                if (!SubModule.PartySettingsManager.IsHeroManageable(controlling.LeaderHero))
                    continue;
                if (controlling.MapEvent != null)
                    continue;

                if (controlling.GetPosition2D.Distance(MobileParty.MainParty.GetPosition2D) >
                    MobileParty.MainParty.SeeingRange)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage(
                            new TextObject("{=PAIc1pTxSOA}{NAME} is out of range to be commanded directly")
                                .SetTextVariable("NAME", controlling.Name)
                                .ToString(),
                            Colors.Magenta
                        )
                    );
                    continue;
                }

                // Follow / escort the main party to that point
                SetPartyAiAction.GetActionForEscortingParty(
                    controlling,
                    MobileParty.MainParty,
                    controlling.DesiredAiNavigationType,
                    false,
                    false
                );

                if (controlling.Ai != null)
                    controlling.Ai.SetDoNotMakeNewDecisions(true);

                PartyAIClanPartySettings settings =
                    SubModule.PartySettingsManager.Settings(controlling.LeaderHero);
                settings.OrderQueue.Clear();
                settings.ClearOrder();
                settings.SetOrder(new(MobileParty.MainParty, OrderType.EscortParty));
            }
        }

        // === ENGAGE PARTY (ATTACK ORDER) ===
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MobileParty), "SetMoveEngageParty")]
        private static void SetMoveEngageParty(
            MobileParty __instance,
            MobileParty party
        )
        {
            if (!Input.IsKeyDown(SubModule.PartySettingsManager.CommandPartiesKey) ||
                __instance != MobileParty.MainParty)
            {
                return;
            }

            foreach (MobileParty controlling in SubModule.PartyThinker.AssumingDirectControl)
            {
                if (controlling?.LeaderHero == null)
                    continue;
                if (!SubModule.PartySettingsManager.IsHeroManageable(controlling.LeaderHero))
                    continue;
                if (controlling.MapEvent != null)
                    continue;

                if (controlling.GetPosition2D.Distance(MobileParty.MainParty.GetPosition2D) >
                    MobileParty.MainParty.SeeingRange)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage(
                            new TextObject("{=PAIc1pTxSOA}{NAME} is out of range to be commanded directly")
                                .SetTextVariable("NAME", controlling.Name)
                                .ToString(),
                            Colors.Magenta
                        )
                    );
                    continue;
                }

                if (controlling.Ai != null)
                    controlling.Ai.SetDoNotMakeNewDecisions(true);

                PartyAIClanPartySettings settings =
                    SubModule.PartySettingsManager.Settings(controlling.LeaderHero);
                settings.OrderQueue.Clear();
                settings.ClearOrder();

                if (FactionManager.IsAtWarAgainstFaction(party.MapFaction, controlling.MapFaction))
                {
                    // Attack enemy party
                    SetPartyAiAction.GetActionForEngagingParty(
                        controlling,
                        party,
                        controlling.DesiredAiNavigationType,
                        false
                    );
                    settings.SetOrder(new(party, OrderType.AttackParty));
                }
                else
                {
                    // Escort non-hostile party
                    SetPartyAiAction.GetActionForEscortingParty(
                        controlling,
                        party,
                        controlling.DesiredAiNavigationType,
                        false,
                        false
                    );
                    settings.SetOrder(new(party, OrderType.EscortParty));
                }
            }
        }

        // === ESCORT PARTY (VANILLA FOLLOW ORDER) ===
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MobileParty), "SetMoveEscortParty")]
        private static void SetMoveEscortParty(
            MobileParty __instance,
            MobileParty mobileParty
        )
        {
            // Reuse the same logic as engaging / escorting
            SetMoveEngageParty(__instance, mobileParty);
        }

        // === GO TO SETTLEMENT ===
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MobileParty), "SetMoveGoToSettlement")]
        private static void SetMoveGoToSettlement(
            MobileParty __instance,
            Settlement settlement
        )
        {
            if (!Input.IsKeyDown(SubModule.PartySettingsManager.CommandPartiesKey) ||
                __instance != MobileParty.MainParty)
            {
                return;
            }

            foreach (MobileParty controlling in SubModule.PartyThinker.AssumingDirectControl)
            {
                if (controlling?.LeaderHero == null)
                    continue;
                if (!SubModule.PartySettingsManager.IsHeroManageable(controlling.LeaderHero))
                    continue;
                if (controlling.MapEvent != null)
                    continue;

                if (controlling.GetPosition2D.Distance(MobileParty.MainParty.GetPosition2D) >
                    MobileParty.MainParty.SeeingRange)
                {
                    InformationManager.DisplayMessage(
                        new InformationMessage(
                            new TextObject("{=PAIc1pTxSOA}{NAME} is out of range to be commanded directly")
                                .SetTextVariable("NAME", controlling.Name)
                                .ToString(),
                            Colors.Magenta
                        )
                    );
                    continue;
                }

                if (controlling.Ai != null)
                    controlling.Ai.SetDoNotMakeNewDecisions(true);

                PartyAIClanPartySettings settings =
                    SubModule.PartySettingsManager.Settings(controlling.LeaderHero);
                settings.OrderQueue.Clear();
                settings.ClearOrder();

                if (FactionManager.IsAtWarAgainstFaction(settlement.MapFaction, controlling.MapFaction))
                {
                    // Enemy settlement → besiege
                    SetPartyAiAction.GetActionForBesiegingSettlement(
                        controlling,
                        settlement,
                        controlling.DesiredAiNavigationType,
                        false
                    );
                    settings.SetOrder(new(settlement, OrderType.BesiegeSettlement));
                }
                else
                {
                    if (settlement.IsUnderSiege)
                    {
                        // Friendly settlement under siege → defend
                        SetPartyAiAction.GetActionForDefendingSettlement(
                            controlling,
                            settlement,
                            controlling.DesiredAiNavigationType,
                            false,
                            false
                        );
                        settings.SetOrder(new(settlement, OrderType.DefendSettlement));
                    }
                    else
                    {
                        // Normal case → just visit
                        SetPartyAiAction.GetActionForVisitingSettlement(
                            controlling,
                            settlement,
                            controlling.DesiredAiNavigationType,
                            false,
                            false
                        );

                        // If your OrderType enum has a VisitSettlement value,
                        // swap this to that to perfectly match original behavior.
                        settings.SetOrder(new(settlement, OrderType.DefendSettlement));
                    }
                }
            }
        }
    }
}