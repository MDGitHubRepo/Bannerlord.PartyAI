using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Bannerlord.PartyAI;

public static class Message
{
    public static void OrderStoppedTargetUnreachable(MobileParty party, PAICustomOrder order) =>
        Display(
            new("{=PAI_order_stopped_unreachable}{PARTY} is no longer {ORDER} because their target is not reachable."),
            Colors.Magenta,
            ("PARTY", party.LeaderHero.Name.ToString()),
            ("ORDER", order.Text.ToString()));

    public static void OrderStoppedTargetInvalid(MobileParty party, PAICustomOrder order) =>
        Display(
            new("{=PAI_order_stopped_invalid_target}{PARTY} is no longer {ORDER} because their target is invalid."),
            Colors.Magenta,
            ("PARTY", party.LeaderHero.Name.ToString()),
            ("ORDER", order.Text.ToString()));

    public static void OrderStoppedTargetEnemy(MobileParty party, PAICustomOrder order) =>
        Display(
            new("{=PAI_order_stopped_war}{PARTY} is no longer {ORDER} because the target faction became an enemy."),
            Colors.Magenta,
            ("PARTY", party.LeaderHero.Name.ToString()),
            ("ORDER", order.Text.ToString()));

    public static void Display(TextObject message, Color color, params (string Tag, string Value)[] variables)
    {
        foreach (var variable in variables)
        {
            message.SetTextVariable(variable.Tag, variable.Value);
        }

        InformationManager.DisplayMessage(new InformationMessage(message.ToString(), color));
    }
}
