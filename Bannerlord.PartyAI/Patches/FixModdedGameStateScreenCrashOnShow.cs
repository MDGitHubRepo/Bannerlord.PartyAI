using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace Bannerlord.PartyAI.Patches;

internal class FixModdedGameStateScreenCrashOnShow
{
    public static void Apply(Harmony harmony)
    {
        harmony.Patch()
            .Method<GameStateScreenManager>(x => x.CreateScreen(null))
                .Prefix(CreateScreenPrefix)
            .Method<PartyAIControlsMenuState>(x => x.RegisterListener(null))
                .Prefix(RegisterListenerPrefix);
    }

    private static bool CreateScreenPrefix(GameStateScreenManager __instance, ref ScreenBase __result, GameState state)
    {
        if (state is not PartyAIControlsMenuState partyAIControlsMenuState)
        {
            return true;
        }

        // When the game does assembly scanning looking for GameStateScreens,
        // the mod modules are omitted (for whatever reason)
        __result = new PartyAIControlsMenuScreen(partyAIControlsMenuState);

        return false;
    }

    private static bool RegisterListenerPrefix(PartyAIControlsMenuState __instance, ref bool __result, IGameStateListener listener)
    {
        // Do not register null listeners (causes null exception later down the line)
        // What's funny is that the vanilla code DOES check for null, but it still proceeds to add it.
        if (listener is null)
        {
            __result = false;
            return false;
        }

        return true;
    }
}
