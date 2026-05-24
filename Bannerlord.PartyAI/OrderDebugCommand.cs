/*using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace PartyAIControls
{
	internal class OrderDebugCommand
	{
		[CommandLineFunctionality.CommandLineArgumentFunction("order_debug", "partyai")]
		public static string Execute(List<string> strings)
		{
			string ErrorType = "";
			if (!CampaignCheats.CheckCheatUsage(ref ErrorType))
			{
				return ErrorType;
			}

			if (Campaign.Current == null)
			{
				return "No campaign found, are you in the right game mode?";
			}

			if (!CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckHelp(strings))
			{
				return "Format is \"partyai.order_debug\".";
			}

			List<MobileParty> parties = Clan.PlayerClan.Heroes.Where(h => h.IsPartyLeader && h.PartyBelongedTo != null && SubModule.PartySettingsManager.Settings(h).Order.AiBehavior != AiBehavior.Hold).ToList().ConvertAll(h => h.PartyBelongedTo);

			string result = "";
			foreach (MobileParty party in parties)
      {
				PartyThinkParams thinkParams = party.ThinkParamsCache;

				result += "ThinkParamsCache for " + party.LeaderHero.Name.ToString() + Environment.NewLine;
				foreach ((AIBehaviorTuple,float) score in thinkParams.AIBehaviorScores.ToList().OrderByDescending(s => s.Item2))
        {
					string name = score.Item1.Party.Name.ToString();
					result += score.Item1.AiBehavior.ToString() + " " + name + " - " + score.Item2 + Environment.NewLine;
        }
				result += Environment.NewLine;
			}

			return result;
		}
	}
}
*/