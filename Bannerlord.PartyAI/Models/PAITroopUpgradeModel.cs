using PartyAIControls.CampaignBehaviors;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace PartyAIControls.Models
{
  internal class PAITroopUpgradeModel : PartyTroopUpgradeModel
  {
    readonly PartyTroopUpgradeModel _previousModel;

    public PAITroopUpgradeModel(PartyTroopUpgradeModel previousModel)
    {
      _previousModel = previousModel;
      _previousModel ??= new DefaultPartyTroopUpgradeModel();
    }

    public override bool CanPartyUpgradeTroopToTarget(PartyBase party, CharacterObject character, CharacterObject target)
    {
      return _previousModel.CanPartyUpgradeTroopToTarget(party, character, target);
    }

    public override bool DoesPartyHaveRequiredItemsForUpgrade(PartyBase party, CharacterObject upgradeTarget)
    {
      if (party.Owner?.Equals(Hero.MainHero) ?? false)
      {
        return _previousModel.DoesPartyHaveRequiredItemsForUpgrade(party, upgradeTarget);
      }

      // let AI always upgrade regardless of items
      return true;
    }

    public override bool DoesPartyHaveRequiredPerksForUpgrade(PartyBase party, CharacterObject character, CharacterObject upgradeTarget, out PerkObject requiredPerk)
    {
      return _previousModel.DoesPartyHaveRequiredPerksForUpgrade(party, character, upgradeTarget, out requiredPerk);
    }

    public override ExplainedNumber GetGoldCostForUpgrade(PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget)
    {
      return _previousModel.GetGoldCostForUpgrade(party, characterObject, upgradeTarget);
    }

    public override int GetSkillXpFromUpgradingTroops(PartyBase party, CharacterObject troop, int numberOfTroops)
    {
      return _previousModel.GetSkillXpFromUpgradingTroops(party, troop, numberOfTroops);
    }

    public override float GetUpgradeChanceForTroopUpgrade(PartyBase party, CharacterObject troop, int upgradeTargetIndex)
    {
      if (party.MobileParty == null) return _previousModel.GetUpgradeChanceForTroopUpgrade(party, troop, upgradeTargetIndex);
      if (party.MobileParty.IsGarrison)
      {
        if (!SubModule.PartySettingsManager.IsGarrisonManageable(party.MobileParty.CurrentSettlement))
        {
          return _previousModel.GetUpgradeChanceForTroopUpgrade(party, troop, upgradeTargetIndex);
        }
      }
      else if (!SubModule.PartySettingsManager.IsManageable(party.LeaderHero))
      {
        return _previousModel.GetUpgradeChanceForTroopUpgrade(party, troop, upgradeTargetIndex);
      }

      if (upgradeTargetIndex < 0 || upgradeTargetIndex >= troop.UpgradeTargets.Length || troop.UpgradeTargets.Length == 0)
      {
        return 0.00001f;
      }

      PartyAIClanPartySettings heroSettings;
      if (party.MobileParty.IsGarrison)
      {
        heroSettings = SubModule.PartySettingsManager.Settings(party.MobileParty.CurrentSettlement);
      }
      else
      {
        heroSettings = SubModule.PartySettingsManager.Settings(party.LeaderHero);
      }
      PartyCompositionObect comp = SubModule.PartyTroopRecruiter.GetPartyComposition(party, heroSettings, troop);
      PartyAITroopRecruiter recruiter = SubModule.PartyTroopRecruiter;

      if (heroSettings.MaxTroopTier > 0 && troop.Tier >= heroSettings.MaxTroopTier) { return 0f; }

      if (SubModule.PartyTroopRecruiter.ShouldRecruit(comp, heroSettings, troop.UpgradeTargets[upgradeTargetIndex], party))
      {
        return 1f;
      }

      for (int i = 0; i < troop.UpgradeTargets.Length; i++)
      {
        if (i == upgradeTargetIndex)
        {
          continue;
        }

        if (SubModule.PartyTroopRecruiter.ShouldRecruit(comp, heroSettings, troop.UpgradeTargets[i], party))
        {
          return 0f;
        }
      }

      if (heroSettings.PartyTemplate?.Troops.Contains(troop.UpgradeTargets[upgradeTargetIndex]) ?? true)
      {
        IEnumerable<FormationClass> newTargets = recruiter.UpgradeTargets(troop.UpgradeTargets[upgradeTargetIndex], true, heroSettings.PartyTemplate).ConvertAll(c => FormationClassExtensions.FallbackClass(c.DefaultFormationClass)).Distinct();
        IEnumerable<FormationClass> currentTargets = recruiter.UpgradeTargets(troop, true, heroSettings.PartyTemplate).ConvertAll(c => FormationClassExtensions.FallbackClass(c.DefaultFormationClass)).Distinct();
        if (Enumerable.SequenceEqual(newTargets, currentTargets))
        {
          return 1f;
        }
      }

      return 0f;
    }

    public override int GetXpCostForUpgrade(PartyBase party, CharacterObject characterObject, CharacterObject upgradeTarget)
    {
      return _previousModel.GetXpCostForUpgrade(party, characterObject, upgradeTarget);
    }

    public override bool IsTroopUpgradeable(PartyBase party, CharacterObject character)
    {
      bool result = _previousModel.IsTroopUpgradeable(party, character);

      if (!result || party.MobileParty == null) return result;
      if (party.MobileParty.IsGarrison)
      {
        if (!SubModule.PartySettingsManager.IsGarrisonManageable(party.MobileParty.CurrentSettlement))
        {
          return result;
        }
      }
      else if (!SubModule.PartySettingsManager.IsManageable(party.LeaderHero))
      {
        return result;
      }

      for (int i = 0; i < character.UpgradeTargets.Length; i++)
      {
        if (GetUpgradeChanceForTroopUpgrade(party, character, i) > 0f)
        {
          return true;
        }
      }

      return false;
    }
  }
}
