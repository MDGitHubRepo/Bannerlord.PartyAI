using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Bannerlord.PartyAI;

public static class Message
{
    public static void Display(TextObject message, Color color, params (string Tag, string Value)[] variables)
    {
        foreach (var variable in variables)
        {
            message.SetTextVariable(variable.Tag, variable.Value);
        }

        InformationManager.DisplayMessage(new InformationMessage(message.ToString(), color));
    }
}
