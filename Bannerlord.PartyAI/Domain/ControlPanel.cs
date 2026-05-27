using TaleWorlds.Core;
using TaleWorlds.InputSystem;

namespace Bannerlord.PartyAI.Domain;

public static class ControlPanel
{
    public static bool IsKeyCombinationDown()
    {
        var manager = SubModule.PartySettingsManager;
        var modifierKey = manager.ControlPanelModiferKey;
        var mainKey = manager.ControlPanelKey;

        return (modifierKey == InputKey.Invalid
            || Input.IsKeyDown(modifierKey))
            && Input.IsKeyDown(mainKey);
    }

    public static void Open()
    {
        GameStateManager.Current.PushState(GameStateManager.Current.CreateState<PartyAIControlsMenuState>());
    }
}
