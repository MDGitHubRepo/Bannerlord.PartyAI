using Bannerlord.PartyAI.Domain.Models;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Bannerlord.PartyAI;

public static class Message
{
    public static void OrderStoppedTargetUnreachable(MobileParty party, PartyAiOrder order) =>
        DisplayOrderMessage(
            "{=PAI_order_stopped_unreachable}{PARTY} is no longer {ORDER} because their target is not reachable.",
            party,
            order);

    public static void OrderStoppedTargetInvalid(MobileParty party, PartyAiOrder order) =>
        DisplayOrderMessage(
            "{=PAI_order_stopped_invalid_target}{PARTY} is no longer {ORDER} because their target is invalid.",
            party,
            order);

    public static void OrderStoppedNoValidTargets(MobileParty party, PartyAiOrder order) =>
        DisplayOrderMessage(
            "{=PAI_order_stopped_no_valid_targets}{PARTY} is no longer {ORDER} because no suitable target could be found.",
            party,
            order);

    public static void OrderStoppedTargetEnemy(MobileParty party, PartyAiOrder order) =>
        DisplayOrderMessage(
            "{=PAI_order_stopped_war}{PARTY} is no longer {ORDER} because the target's faction became an enemy.",
            party,
            order);

    public static void OrderStoppedTargetFriendly(MobileParty party, PartyAiOrder order) =>
        DisplayOrderMessage(
            "{=PAI_order_stopped_peace}{PARTY} is no longer {ORDER} because the target's faction is no longer an enemy.",
            party,
            order);

    public static void OrderStoppedTargetSieged(MobileParty party, PartyAiOrder order) =>
        DisplayOrderMessage(
            "{=PAI_order_stopped_siege}{PARTY} is no longer {ORDER} because the target is under siege.",
            party,
            order);

    private static void DisplayOrderMessage(string message, MobileParty party, PartyAiOrder order) =>
        Display(
            new(message),
            Colors.Magenta,
            ("PARTY", party.LeaderHero.Name),
            ("ORDER", OrderVerbalizer.GetStatusText(order)));

    public static void Display(TextObject message, Color color, params (string Tag, TextObject Value)[] variables)
    {
        foreach (var variable in variables)
        {
            message.SetTextVariable(variable.Tag, variable.Value);
        }

        InformationManager.DisplayMessage(new InformationMessage(message.ToString(), color));
    }
}
