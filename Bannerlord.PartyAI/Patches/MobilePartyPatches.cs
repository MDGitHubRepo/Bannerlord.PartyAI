using Bannerlord.PartyAI.Domain;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using static Bannerlord.PartyAI.PAICustomOrder;

namespace Bannerlord.PartyAI.Patches;

internal class MobilePartyPatches
{
    public static void Apply(Harmony harmony)
    {
        harmony.Patch<MobileParty>()
            .Method(x => x.SetMoveGoToPoint(default, default))
                .Postfix(SetMoveGoToPointPostfix)
            .Method(x => x.SetMoveEngageParty(default, default))
                .Postfix(SetMoveEngagePartyPostfix)
            .Method(x => x.SetMoveEscortParty(default, default, default))
                .Postfix(SetMoveEscortPartyPostfix)
            .Method(x => x.SetMoveGoToSettlement(default, default, default))
                .Postfix(SetMoveGoToSettlementPostfix);
    }

    // === GO TO POINT (CLICK ON MAP) ===
    private static void SetMoveGoToPointPostfix(
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

        foreach (MobileParty controlling in ControlAssumption.AssumingDirectControl)
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
            settings.SetOrder(OrderType.EscortParty, MobileParty.MainParty);
        }
    }

    // === ENGAGE PARTY (ATTACK ORDER) ===
    private static void SetMoveEngagePartyPostfix(
        MobileParty __instance,
        MobileParty party
    )
    {
        if (!Input.IsKeyDown(SubModule.PartySettingsManager.CommandPartiesKey) ||
            __instance != MobileParty.MainParty)
        {
            return;
        }

        foreach (MobileParty controlling in ControlAssumption.AssumingDirectControl)
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
                settings.SetOrder(OrderType.AttackParty, party);
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
                settings.SetOrder(OrderType.EscortParty, party);
            }
        }
    }

    // === ESCORT PARTY (VANILLA FOLLOW ORDER) ===
    private static void SetMoveEscortPartyPostfix(
        MobileParty __instance,
        MobileParty mobileParty
    )
    {
        // Reuse the same logic as engaging / escorting
        SetMoveEngagePartyPostfix(__instance, mobileParty);
    }

    // === GO TO SETTLEMENT ===
    private static void SetMoveGoToSettlementPostfix(
        MobileParty __instance,
        Settlement settlement
    )
    {
        if (!Input.IsKeyDown(SubModule.PartySettingsManager.CommandPartiesKey) ||
            __instance != MobileParty.MainParty)
        {
            return;
        }

        foreach (MobileParty controlling in ControlAssumption.AssumingDirectControl)
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
                settings.SetOrder(OrderType.BesiegeSettlement, settlement);
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
                    settings.SetOrder(OrderType.DefendSettlement, settlement);
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
                    settings.SetOrder(OrderType.DefendSettlement, settlement);
                }
            }
        }
    }
}