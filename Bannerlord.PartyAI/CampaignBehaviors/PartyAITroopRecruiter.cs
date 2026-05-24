using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace PartyAIControls.CampaignBehaviors
{
  internal class PartyAITroopRecruiter : CampaignBehaviorBase
  {
    private readonly Dictionary<CultureObject, List<CharacterObject>> _troopTreeCache = new();
    private readonly Dictionary<CharacterObject, List<CharacterObject>> _upgradeTargetCache = new();
    private bool _firingEvent = false;

    public override void RegisterEvents()
    {
      CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, OnTroopRecruited);
      CampaignEvents.OnLootDistributedToPartyEvent.AddNonSerializedListener(this, OnLootDistributedToParty);
      CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, DailyTickParty);
      CampaignEvents.DailyTickSettlementEvent.AddNonSerializedListener(this, DailyTickSettlement);
    }

        private void OnLootDistributedToParty(PartyBase winnerParty, PartyBase defeatedParty, ItemRoster lootedItems)
        {
            if ((!SubModule.PartySettingsManager.AllowTroopConversion
                 || !SubModule.PartySettingsManager.IsManageable(winnerParty?.LeaderHero))
                && !SubModule.PartySettingsManager.AllowCaravanConversion(winnerParty?.LeaderHero))
            {
                return;
            }

            if (winnerParty?.LeaderHero == null)
                return;

            var heroSettings = SubModule.PartySettingsManager.Settings(winnerParty.LeaderHero);
            if (heroSettings?.PartyTemplate == null)
                return;

            ExchangeRoster(winnerParty.MemberRoster, heroSettings, winnerParty.LeaderHero, null);
        }

        internal void DismissUnwantedTroops(PartyAIClanPartySettings settings, MobileParty party, int max)
    {
      TroopRoster roster = party?.MemberRoster;
      if (roster == null || party?.Party == null) { return; }
      int gotRidOf = 0;
      while (gotRidOf < max)
      {
        List<TroopRosterElement> troops = roster.GetTroopRoster().ToList();
        troops.Shuffle();
        int thisRun = 0;
        foreach (TroopRosterElement e in troops)
        {
          if (e.Character.IsHero) { continue; }
          if (gotRidOf >= max) { return; }
          if ((settings.PartyTemplate != null && !settings.PartyTemplate.Troops.Contains(e.Character)) || OverMaxTier(e.Character, settings.MaxTroopTier))
          {
            roster.RemoveTroop(e.Character, 1);
            gotRidOf++;
            thisRun++;
            roster.RemoveZeroCounts();
          }
        }
        if (thisRun == 0) { break; }
      }

      PartyCompositionObect comp = GetPartyComposition(party.Party, settings);
      Dictionary<FormationClass, int> overages = new();
      foreach (FormationClass formation in new FormationClass[] { FormationClass.Infantry, FormationClass.Ranged, FormationClass.Cavalry, FormationClass.HorseArcher })
      {
        float overage = comp[formation] - settings.Composition[formation];
        int count = (int)(overage * party.Party.PartySizeLimit);
        if (settings.Composition[formation] == 0f && count == 0 && overage * party.Party.PartySizeLimit > 0.9f)
        {
          count = 1;
        }
        overages[formation] = count;
      }

      foreach (KeyValuePair<FormationClass, int> overage in overages.Where(o => o.Value > 0))
      {
        List<TroopRosterElement> troops = roster.GetTroopRoster().ToList();
        troops.Shuffle();

        foreach (TroopRosterElement e in troops)
        {
          if (e.Character.IsHero) { continue; }
          List<FormationClass> upgradeTargets = UpgradeTargets(e.Character, maxTierOnly: true, template: settings.PartyTemplate).ConvertAll(t => FormationClassExtensions.FallbackClass(t.DefaultFormationClass));
          if (!upgradeTargets.Contains(overage.Key)) { continue; }

          // if another formation needs this troop to upgrade to it, don't dismiss it
          if (upgradeTargets.Any(t => overages[t] < 0))
          {
            continue;
          }

          while (gotRidOf < max && roster.GetTroopCount(e.Character) > 0)
          {
            roster.RemoveTroop(e.Character, 1);
            gotRidOf++;
            roster.RemoveZeroCounts();
          }
        }
      }
    }

    private void ExchangeRoster(TroopRoster roster, PartyAIClanPartySettings settings, Hero hero, Settlement settlement)
    {
      List<TroopRosterElement> troops = roster.GetTroopRoster().ToList();
      troops.Shuffle();
      foreach (TroopRosterElement e in troops)
      {
        if (!settings.PartyTemplate.Troops.Contains(e.Character) || OverMaxTier(e.Character, settings.MaxTroopTier))
        {
          if (settings.TroopsConvertibleToday <= 0) { break; }
          ExchangeClanTroops(hero, roster, e.Character, e.Number - e.WoundedNumber, false, settlement);
        }
      }
    }

    private void DailyTickSettlement(Settlement settlement)
    {
      if (settlement?.Town?.GarrisonParty?.MemberRoster == null || settlement?.Owner == null)
      {
        return;
      }

      if (settlement.IsUnderSiege || settlement.InRebelliousState) { return; }

      if (!SubModule.PartySettingsManager.AllowTroopConversionForGarrisons || !SubModule.PartySettingsManager.IsGarrisonManageable(settlement)) { return; }

      PartyAIClanPartySettings settings = SubModule.PartySettingsManager.Settings(settlement);
      if (settings.PartyTemplate == null)
      {
        return;
      }

      ExchangeRoster(settlement.Town.GarrisonParty.MemberRoster, settings, null, settlement);
    }

    private void DailyTickParty(MobileParty party)
    {
      if ((!SubModule.PartySettingsManager.AllowTroopConversion || !SubModule.PartySettingsManager.IsManageable(party?.LeaderHero)) && !SubModule.PartySettingsManager.AllowCaravanConversion(party?.LeaderHero))
      {
        return;
      }
      if (party.MapEvent != null) { return; }

      PartyAIClanPartySettings heroSettings = SubModule.PartySettingsManager.Settings(party.LeaderHero);
      if (heroSettings.PartyTemplate == null)
      {
        return;
      }

      ExchangeRoster(party.MemberRoster, heroSettings, party.LeaderHero, null);
    }

    private void OnTroopRecruited(Hero recruiter, Settlement settlement, Hero recruitmentSource, CharacterObject troop, int count)
    {
      if (_firingEvent || (!SubModule.PartySettingsManager.AllowTroopConversion && !SubModule.PartySettingsManager.AllowCaravanConversion(recruiter)))
      {
        return;
      }

      if (SubModule.PartySettingsManager.IsManageable(recruiter))
      {
        PartyAIClanPartySettings heroSettings = SubModule.PartySettingsManager.Settings(recruiter);
        if (heroSettings.PartyTemplate != null && heroSettings.TroopsConvertibleToday > 0)
        {
          ExchangeClanTroops(recruiter, recruiter?.PartyBelongedTo?.MemberRoster, troop, count, true);
          return;
        }
      }
    }

    private void ExchangeClanTroops(Hero owner, TroopRoster roster, CharacterObject troop, int count, bool fireEvent, Settlement settlement = null)
    {
      if (owner?.PartyBelongedTo?.Party == null && settlement == null) { return; }

      if (!SubModule.PartySettingsManager.IsManageable(owner) && !SubModule.PartySettingsManager.IsGarrisonManageable(settlement)) { return; }

      if (roster == null || troop.IsHero || roster.GetTroopCount(troop) < count || count <= 0) { return; }

      PartyBase party;
      PartyAIClanPartySettings heroSettings;
      PAICustomTemplate template;
      if (settlement != null)
      {
        party = settlement.Town.GarrisonParty.Party;
        heroSettings = SubModule.PartySettingsManager.Settings(settlement);
        template = heroSettings.PartyTemplate;
      }
      else
      {
        party = owner.PartyBelongedTo.Party;
        heroSettings = SubModule.PartySettingsManager.Settings(owner);
        template = heroSettings.PartyTemplate;
      }
      if (template == null) { return; }
      if (heroSettings.TroopsConvertibleToday <= 0) { return; }

      while (count > 0 && heroSettings.TroopsConvertibleToday > 0)
      {
        PartyCompositionObect comp = GetPartyComposition(party, heroSettings, troop);
        List<CharacterObject> eligible = template.Troops.Where(t => ShouldRecruit(comp, heroSettings, t, party)).ToList();

        CharacterObject replacement = DetermineReplacement(eligible, troop.Tier, IsEliteTroop(troop));

        if (replacement == null)
        {
          eligible = template.Troops.Where(t => ShouldRecruit(comp, heroSettings, t, party, false)).ToList();
          replacement = DetermineReplacement(eligible, troop.Tier, IsEliteTroop(troop));
          replacement ??= DetermineReplacement(eligible, troop.Tier, !IsEliteTroop(troop));
        }

        if (replacement == null && !template.Troops.Contains(troop))
        {
          replacement = DetermineReplacement(template.Troops, troop.Tier, IsEliteTroop(troop));
          replacement ??= DetermineReplacement(template.Troops, troop.Tier, !IsEliteTroop(troop));
        }

        if (replacement == null) { return; }

        IEnumerable<FormationClass> targets = UpgradeTargets(replacement, true, heroSettings.PartyTemplate).ConvertAll(c => FormationClassExtensions.FallbackClass(c.DefaultFormationClass)).Distinct();
        int amount = Math.Max(1, targets.Sum(t => (int)Math.Floor((heroSettings.Composition[t] - comp[t]) * party.PartySizeLimit)));
        amount = Math.Min(amount, heroSettings.TroopsConvertibleToday);
        if (amount > count)
        {
          amount = count;
        }

        roster.RemoveTroop(troop, amount);
        roster.AddToCounts(replacement, amount);
        roster.RemoveZeroCounts();
        count -= amount;
        heroSettings.DeductTroopsConvertibleToday(amount);

        if (settlement == null && replacement.Tier != troop.Tier || IsEliteTroop(replacement) != IsEliteTroop(troop))
        {
          // adjust recruitment gold
          var troopCostExpl = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(troop, owner);
          var replacementCostExpl = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(replacement, owner);
                int troopCost = troopCostExpl.RoundedResultNumber;
                int replacementCost = replacementCostExpl.RoundedResultNumber;
          GiveGoldAction.ApplyBetweenCharacters(null, owner, troopCost - replacementCost);
        }

        if (fireEvent)
        {
          _firingEvent = true;
          CampaignEventDispatcher.Instance.OnTroopRecruited(owner, null, null, replacement, amount);
          _firingEvent = false;
        }
      }
    }

    private CharacterObject DetermineReplacement(List<CharacterObject> templateCharacters, int troopTier, bool useElite)
    {
      CharacterObject replacement = null;
      foreach (bool elite in new bool[] { useElite, !useElite })
      {
        if (replacement != null)
        {
          break;
        }

        int tier = troopTier;
        replacement = Extensions.GetRandomElement(templateCharacters.Where(t => t.Tier == tier && IsEliteTroop(t) == elite).ToList());

        for (int i = 1; replacement == null; i++)
        {
          replacement ??= Extensions.GetRandomElement(templateCharacters.Where(t => t.Tier == tier - i && IsEliteTroop(t) == elite).ToList());

          replacement ??= Extensions.GetRandomElement(templateCharacters.Where(t => t.Tier == tier + i && IsEliteTroop(t) == elite).ToList());

          if (tier - i <= 0 && tier + i > Campaign.Current.Models.CharacterStatsModel.MaxCharacterTier)
          {
            break;
          }
        }
      }

      return replacement;
    }

    internal List<CharacterObject> TraverseTree(CharacterObject unit)
    {
      List<CharacterObject> characterObjectList = new();
      Stack<CharacterObject> characterObjectStack = new();
      characterObjectStack.Push(unit);
      characterObjectList.Add(unit);
      while (!Extensions.IsEmpty(characterObjectStack))
      {
        CharacterObject characterObject = characterObjectStack.Pop();
        if (characterObject?.UpgradeTargets != null && characterObject.UpgradeTargets.Length != 0)
        {
          for (int index = 0; index < characterObject.UpgradeTargets.Length; ++index)
          {
            if (!characterObjectList.Contains(characterObject.UpgradeTargets[index]))
            {
              characterObjectList.Add(characterObject.UpgradeTargets[index]);
              characterObjectStack.Push(characterObject.UpgradeTargets[index]);
            }
          }
        }
      }

      return characterObjectList;
    }

    internal bool IsEliteTroop(CharacterObject unit)
    {
      List<CharacterObject> characterObjectList;

      if (_troopTreeCache.ContainsKey(unit.Culture))
      {
        characterObjectList = _troopTreeCache[unit.Culture];
      }
      else
      {
        characterObjectList = TraverseTree(unit.Culture.EliteBasicTroop);
        _troopTreeCache.Add(unit.Culture, characterObjectList);
      }

      return characterObjectList.Contains(unit);
    }

    internal bool ShouldRecruit(PartyCompositionObect comp, PartyAIClanPartySettings heroSettings, CharacterObject troop, PartyBase party, bool mustBeOnePlus = true)
    {
      IEnumerable<FormationClass> targets = UpgradeTargets(troop, true, heroSettings.PartyTemplate).ConvertAll(c => FormationClassExtensions.FallbackClass(c.DefaultFormationClass)).Distinct();

      if (targets.Count() == 0 || OverMaxTier(troop, heroSettings.MaxTroopTier))
      {
        //TaleWorlds.Library.InformationManager.DisplayMessage(new("Will not recruit "+troop.Name+" because it has no valid upgrade paths.",TaleWorlds.Library.Colors.Red));
        return false;
      }

      foreach (FormationClass target in targets)
      {
        float need = heroSettings.Composition[target] - comp[target];
        need *= party.PartySizeLimit;

        if (need >= (mustBeOnePlus ? 1f : 0.4f))
        {
          //TaleWorlds.Library.InformationManager.DisplayMessage(new("Will recruit " + troop.Name, TaleWorlds.Library.Colors.Green));
          return true;
        }
      }

      //TaleWorlds.Library.InformationManager.DisplayMessage(new("Will not recruit " + troop.Name+ " due to insufficient need.", TaleWorlds.Library.Colors.Red));
      return false;
    }

    internal PartyCompositionObect GetPartyComposition(PartyBase party, PartyAIClanPartySettings heroSettings, CharacterObject ignore = null)
    {
      PAICustomTemplate template = heroSettings.PartyTemplate;
      PartyCompositionObect comp = new();
      float total = party.PartySizeLimit;

      if (total <= 0)
      {
        return comp;
      }

      IEnumerable<(IEnumerable<FormationClass>, CharacterObject, int)> roster = party.MemberRoster.GetTroopRoster().ConvertAll(e =>
        (UpgradeTargets(e.Character, true, template).ConvertAll(t => FormationClassExtensions.FallbackClass(t.DefaultFormationClass)).Distinct(), e.Character, e.Number)
      ).OrderBy(e => e.Item1.Count());

      foreach ((IEnumerable<FormationClass>, CharacterObject, int) e in roster)
      {
        if (e.Item2.IsHero || (ignore != null && e.Item2.Equals(ignore)))
        {
          continue;
        }

        FormationClass troopClass = FormationClassExtensions.FallbackClass(e.Item2.DefaultFormationClass);

        if (e.Item1.Count() == 0)
        {
          comp[troopClass] += e.Item3;
          continue;
        }

        if (e.Item1.Count() == 1)
        {
          comp[e.Item1.First()] += e.Item3;
          continue;
        }

        int Number = e.Item3;

        foreach (FormationClass distinctTargetPath in e.Item1)
        {
          if (Number == 0)
          {
            break;
          }

          while (Number > 0)
          {
            float currentSatisfaction = comp[distinctTargetPath] / total;
            float need = heroSettings.Composition[distinctTargetPath] - currentSatisfaction;
            need *= total;
            if (need >= 1f)
            {
              comp[distinctTargetPath] += 1f;
              Number--;
              continue;
            }
            break;
          }
        }

        if (Number > 0)
        {
          comp[troopClass] += Number;
        }
      }

      comp[FormationClass.Infantry] /= total;
      comp[FormationClass.Ranged] /= total;
      comp[FormationClass.Cavalry] /= total;
      comp[FormationClass.HorseArcher] /= total;

      /*PartyCompositionObect comp2 = comp.Clone();
      comp2.Scale(100);
      TaleWorlds.Library.InformationManager.DisplayMessage(new(party.Name.ToString() + " Comp: I:" + ((int)comp2.Infantry).ToString() + "%, R:" + ((int)comp2.Ranged).ToString() + "%, C:" + ((int)comp2.Cavalry).ToString() + "%, H:" + ((int)comp2.HorseArcher).ToString() + "%", TaleWorlds.Library.Colors.Blue));*/

      return comp;
    }

    internal List<CharacterObject> UpgradeTargets(CharacterObject troop, bool maxTierOnly = false, PAICustomTemplate template = null)
    {
      if (troop == null)
      {
        return new List<CharacterObject>();
      }

      List<CharacterObject> targets;
      if (_upgradeTargetCache.ContainsKey(troop))
      {
        targets = _upgradeTargetCache[troop];
      }
      else
      {
        targets = TraverseTree(troop);
        _upgradeTargetCache.Add(troop, targets);
      }

      if (maxTierOnly)
      {
        // Troops with no upgrade targets or no upgrade targets in the template
        targets = targets.Where(c => (c?.UpgradeTargets == null || c.UpgradeTargets.Length == 0) || !c.UpgradeTargets.Any(t => (template != null && template.Troops != null) ? template.Troops.Contains(t) : true)).ToList();
      }

      if (template != null && template.Troops != null)
      {
        return targets.Where(c => template.Troops.Contains(c)).ToList();
      }
      return targets;
    }

    private bool OverMaxTier(CharacterObject troop, int maxTier) => maxTier > 0 && troop?.Tier > maxTier;

    public override void SyncData(IDataStore dataStore)
    {
    }
  }
}
