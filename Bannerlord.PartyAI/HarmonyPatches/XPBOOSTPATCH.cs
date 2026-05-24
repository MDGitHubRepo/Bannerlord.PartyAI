namespace PartyAIControls.HarmonyPatches
{
  /*internal class PartyUpgraderCampaignBehaviorPatches
{

[HarmonyPatch(typeof(PartyUpgraderCampaignBehavior), "GetPossibleUpgradeTargets")]
internal class GetPossibleUpgradeTargets
{
	private static Type TroopArgsType = AccessTools.TypeByName("TroopUpgradeArgs");
	private static Type TroopArgsListType = typeof(List<>).MakeGenericType(new[] { TroopArgsType });

	private static void Postfix(ref IList __result, PartyBase party, TroopRosterElement element)
	{
		if (party?.MobileParty?.ActualClan == null || !Clan.PlayerClan.Equals(party.MobileParty.ActualClan))
		{
			return;
		}

		PartyWageModel partyWageModel = Campaign.Current.Models.PartyWageModel;
		IList list = (IList)Activator.CreateInstance(TroopArgsListType);
		CharacterObject character = element.Character;
		int num = element.Number - element.WoundedNumber;
		if (num > 0)
		{
			PartyTroopUpgradeModel partyTroopUpgradeModel = Campaign.Current.Models.PartyTroopUpgradeModel;
			for (int i = 0; i < character.UpgradeTargets.Length; i++)
			{
				CharacterObject characterObject = character.UpgradeTargets[i];
				int upgradeXpCost = character.GetUpgradeXpCost(party, i);
				if (upgradeXpCost > 0)
				{
					num = MathF.Min(num, element.Xp / upgradeXpCost);
					if (num == 0)
					{
						continue;
					}
				}
				if (characterObject.Tier > character.Tier && party.MobileParty.HasLimitedWage() && party.MobileParty.TotalWage + num * (partyWageModel.GetCharacterWage(characterObject) - partyWageModel.GetCharacterWage(character)) > party.MobileParty.PaymentLimit)
				{
					num = MathF.Max(0, MathF.Min(num, (party.MobileParty.PaymentLimit - party.MobileParty.TotalWage) / (partyWageModel.GetCharacterWage(characterObject) - partyWageModel.GetCharacterWage(character))));
					if (num == 0)
					{
						continue;
					}
				}
				int upgradeGoldCost = character.GetUpgradeGoldCost(party, i);
				if (party.LeaderHero != null && upgradeGoldCost != 0 && num * upgradeGoldCost > party.LeaderHero.Gold)
				{
					num = party.LeaderHero.Gold / upgradeGoldCost;
					if (num == 0)
					{
						continue;
					}
				}
				if ((!party.Culture.IsBandit || characterObject.Culture.IsBandit) && (character.Occupation != Occupation.Bandit || partyTroopUpgradeModel.CanPartyUpgradeTroopToTarget(party, character, characterObject)))
				{
					float upgradeChanceForTroopUpgrade = GetUpgradeChanceForTroopUpgrade(party, character, i);
					if (upgradeChanceForTroopUpgrade > 0f || character.UpgradeTargets.Length == 1)
					{
						list.Add(Activator.CreateInstance(TroopArgsType, new object[] { character, characterObject, num, upgradeGoldCost, upgradeXpCost, upgradeChanceForTroopUpgrade }));
					}
				}
			}
		}
		__result = list;
		return;
	}
}*/
  /*
	[HarmonyPatch(typeof(DefaultPartyTrainingModel), "GetEffectiveDailyExperience")]
  internal class tempxpbuff
  {
    private static void Postfix(ref ExplainedNumber __result, MobileParty mobileParty, TroopRosterElement troop)
    {
      if (mobileParty?.ActualClan == null || !Clan.PlayerClan.Equals(mobileParty.ActualClan) || mobileParty.Owner == null || Hero.MainHero.Equals(mobileParty.Owner))
      {
        return;
      }

      __result.Add(100f,new TaleWorlds.Localization.TextObject("FREE XP"));
    }
  }*/
}
