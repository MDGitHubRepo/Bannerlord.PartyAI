using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace PartyAIControls
{
  internal class OpenControlPanelCommand
  {
    [CommandLineFunctionality.CommandLineArgumentFunction("open", "partyai")]
    public static string Execute(List<string> strings)
    {
      if (Campaign.Current == null)
      {
        return "No campaign found, are you in the right game mode?";
      }

      if (!CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckHelp(strings))
      {
        return "Format is \"partyai.open\".";
      }

      if (Game.Current?.GameStateManager?.ActiveState == null 
                || Game.Current.GameStateManager.ActiveState is not MapState 
                || Game.Current.GameStateManager.ActiveState.IsMenuState)
      {
        return "You must be on the map screen to use this command.";
      }

      GameStateManager.Current.PushState(GameStateManager.Current.CreateState<PartyAIControlsMenuState>());
      return "Success";
    }
  }
}
