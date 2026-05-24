using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encyclopedia;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyAIControls.CampaignBehaviors
{
  public class PartyAIClanPartySettingsManager : CampaignBehaviorBase
  {
    private Dictionary<Hero, PartyAIClanPartySettings> _partySettings = new();
    private Dictionary<Settlement, PartyAIClanPartySettings> _garrisonSettings = new();
    private Dictionary<Hero, PartyAIClanPartySettings> _caravanSettings = new();
    private List<PAICustomTemplate> _partyTemplates = new();

    internal bool AllowTroopConversion = false;
    internal bool AllowTroopConversionForCaravans = true;
    internal bool AllowTroopConversionForGarrisons = true;
    internal bool ManageCaravans;
    internal bool ManageClanGarrisons;
    internal bool ManageKingdomParties;
    internal bool ManageKingdomGarrisons;
    internal int TroopsConvertedPerDay = 4;
    internal bool AutoCreateClanParties = false;
    internal int AutoCreateClanPartiesMax = 0;
    internal List<Hero> AutoCreateClanPartiesRoster = new();
    internal PartyAIClanPartySettings _defaultClanPartySettings = new((Hero)null);
    internal PartyAIClanPartySettings _defaultClanCaravanSettings = new((Hero)null);
    internal PartyAIClanPartySettings _defaultClanGarrisonSettings = new((Hero)null);
    internal PartyAIClanPartySettings _defaultKingdomPartySettings = new((Hero)null);
    internal PartyAIClanPartySettings _defaultKingdomGarrisonSettings = new((Hero)null);
    internal bool AggressivePatrols = false;
    internal bool AIRecruitCulture = false;
    internal InputKey ControlPanelModiferKey = InputKey.LeftControl;
    internal InputKey ControlPanelKey = InputKey.P;
    internal InputKey CommandedPartiesModiferKey = InputKey.LeftAlt;
    internal InputKey CommandedPartiesKey = InputKey.X;
    internal InputKey CommandPartiesKey = InputKey.LeftAlt;

    public override void RegisterEvents()
    {
      CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, new Action(OnDailyTick));
      CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter>(OnSessionLaunched));
    }

    private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
    {
            //Taleworlds removed InventoryManager for whatever fucking reason so the entire "inspect your partys inventory" feature is removed for 1.3.9

      foreach (PartyAIClanPartySettings settings in _partySettings.ToList().ConvertAll(s => s.Value).Concat(_caravanSettings.ToList().ConvertAll(s => s.Value)).Concat(_garrisonSettings.ToList().ConvertAll(s => s.Value)))
      {
        settings.FilteredSettlements ??= new();
        settings.OrderQueue ??= new();
        if (settings.PatrolRadius == 0f)
        {
          settings.PatrolRadius = 1f;
        }
      }
    }

    private void OnDailyTick()
    {
      // reset budgets
      IEnumerable<PartyAIClanPartySettings> allSettings = _partySettings.ToList().ConvertAll(s => s.Value).Concat(_caravanSettings.ToList().ConvertAll(s => s.Value)).Concat(_garrisonSettings.ToList().ConvertAll(s => s.Value)).AsEnumerable();
      foreach (PartyAIClanPartySettings item in allSettings)
      {
        item.ResetBudgets();
      }

      // cleanup dead heroes
      foreach (KeyValuePair<Hero, PartyAIClanPartySettings> item in _partySettings.AsEnumerable().Reverse())
      {
        if (item.Value.Hero?.IsDead ?? true || item.Value.Hero.IsDisabled)
        {
          _partySettings.Remove(item.Key);
        }
      }

      foreach (KeyValuePair<Hero, PartyAIClanPartySettings> item in _caravanSettings.AsEnumerable().Reverse())
      {
        if (item.Value.Hero?.IsDead ?? true || item.Value.Hero.IsDisabled)
        {
          _caravanSettings.Remove(item.Key);
        }
      }

      foreach (Hero h in AutoCreateClanPartiesRoster.AsEnumerable().Reverse())
      {
        if (h?.IsDead ?? true || h.IsDisabled)
        {
          AutoCreateClanPartiesRoster.Remove(h);
        }
      }
    }

    internal List<PartyAIClanPartySettings> HeroesWithOrders => _partySettings.Where(s => s.Value.HasActiveOrder).ToList().ConvertAll(s => s.Value);

    internal IEnumerable<PartyAIClanPartySettings> AllPartySettings => _partySettings.Values;

    internal void AddPartyTemplate(PAICustomTemplate template)
    {
      _partyTemplates.Add(template);
    }

    internal void DeletePartyTemplate(PAICustomTemplate template)
    {
      _partyTemplates.Remove(template);

      foreach (KeyValuePair<Hero, PartyAIClanPartySettings> settings in _partySettings)
      {
        if (settings.Value.PartyTemplate == template)
        {
          settings.Value.PartyTemplate = null;
        }
      }
    }

    internal List<PAICustomTemplate> AllTemplates => _partyTemplates.ToList();

    internal bool HasActiveOrder(Hero h) => Settings(h).HasActiveOrder;
    internal bool IsUniqueTemplateName(string name)
    {
      foreach (PAICustomTemplate t in _partyTemplates)
      {
        if (t.Name == name)
        {
          return false;
        }
      }

      return true;
    }

    internal PartyAIClanPartySettings Settings(Settlement settlement)
    {
      if (settlement == null)
      {
        return new PartyAIClanPartySettings((Settlement)null);
      }

      if (!_garrisonSettings.ContainsKey(settlement))
      {
        if (settlement.OwnerClan == Clan.PlayerClan)
        {
          _garrisonSettings.Add(settlement, _defaultClanGarrisonSettings.Clone(null, settlement));
        }
        else if (settlement.MapFaction == Hero.MainHero.MapFaction)
        {
          _garrisonSettings.Add(settlement, _defaultKingdomGarrisonSettings.Clone(null, settlement));
        }
        else { return new PartyAIClanPartySettings((Settlement)null); }
      }

      return _garrisonSettings[settlement];
    }

    internal PartyAIClanPartySettings Settings(Hero hero)
    {
      if (hero == null)
      {
        return new PartyAIClanPartySettings((Hero)null);
      }

      if (IsLeadingCaravan(hero))
      {
        if (!_caravanSettings.ContainsKey(hero))
        {
          _caravanSettings.Add(hero, _defaultClanCaravanSettings.Clone(hero));
        }
        return _caravanSettings[hero];
      }

      if (!_partySettings.ContainsKey(hero))
      {
        if (hero.Clan == Clan.PlayerClan)
        {
          _partySettings.Add(hero, _defaultClanPartySettings.Clone(hero));
        }
        else if (IsHeroManageable(hero))
        {
          _partySettings.Add(hero, _defaultKingdomPartySettings.Clone(hero));
        }
        else { return new PartyAIClanPartySettings((Hero)null); }
      }

      if (_partySettings[hero].OrderQueue == null)
      {
        _partySettings[hero].OrderQueue = new();
      }

      return _partySettings[hero];
    }

    internal bool IsAIHeroManageable(Hero hero) => !IsLeadingCaravan(hero) && hero?.Clan != null && hero.Clan != Clan.PlayerClan && !hero.Clan.IsBanditFaction && hero.Occupation == Occupation.Lord;

    internal bool IsManageable(Hero hero) => IsHeroManageable(hero) || IsCaravanManageable(hero);

    internal bool IsHeroManageable(Hero hero)
    {
      if (hero == null || Hero.MainHero.Equals(hero)) { return false; }

      if (IsLeadingCaravan(hero)) { return false; }

      if (Clan.PlayerClan.Heroes.Contains(hero)) { return true; }

      // if we're not managing kingdom parties, we can skip the rest
      if (!ManageKingdomParties)
      {
        return false;
      }

      if (Clan.PlayerClan.Kingdom == null || hero?.Clan?.Kingdom == null || !hero.Clan.Kingdom.Equals(Clan.PlayerClan.Kingdom))
      {
        return false;
      }

      if (!Clan.PlayerClan.Kingdom.Leader.Equals(Hero.MainHero))
      {
        return false;
      }

      return true;
    }

    internal bool AllowCaravanConversion(Hero hero) => SubModule.PartySettingsManager.IsCaravanManageable(hero) && SubModule.PartySettingsManager.AllowTroopConversionForCaravans;

    internal bool IsCaravanManageable(Hero hero)
    {
      if (!ManageCaravans) { return false; }

      if (hero == null || Hero.MainHero.Equals(hero)) { return false; }

      if (!Clan.PlayerClan.Heroes.Contains(hero)) { return false; }

      return IsLeadingCaravan(hero);
    }

    internal bool IsGarrisonManageable(Settlement settlement)
    {
      if (!ManageClanGarrisons && !ManageKingdomGarrisons) { return false; }
      if (settlement == null) { return false; }
      if (!settlement.IsFortification) { return false; }

      if (settlement.OwnerClan == Clan.PlayerClan && ManageClanGarrisons)
      {
        return true;
      }
      if (ManageKingdomGarrisons && settlement.MapFaction == Hero.MainHero.MapFaction && Clan.PlayerClan.Kingdom?.RulingClan == Clan.PlayerClan)
      {
        return true;
      }
      return false;
    }

    internal bool IsLeadingCaravan(Hero hero)
    {
      return hero?.PartyBelongedTo != null && hero.IsPartyLeader && hero.PartyBelongedTo.IsCaravan;
    }

    internal TextObject GetOrderText(Hero hero) => Settings(hero).Order?.Text ?? new TextObject("{=PAIZZ1tGdbA}No Active Order");

    internal TroopRoster GetAllTopTierTroops()
    {
      TroopRoster results = TroopRoster.CreateDummyTroopRoster();
      List<CharacterObject> characters = new();
      Occupation[] occupations = new Occupation[3] { Occupation.Soldier, Occupation.Mercenary, Occupation.CaravanGuard };
      List<CharacterObject> exclude = new();

      foreach (CharacterObject troop in CharacterObject.All)
      {
        if (!characters.Contains(troop) && !troop.IsHero && troop.Culture != null && !troop.Culture.IsBandit && occupations.Contains(troop.Occupation))
        {
          characters.AppendList(SubModule.PartyTroopRecruiter.TraverseTree(troop).Where(co => co.UpgradeTargets?.Length == 0).ToList());
        }
      }

      characters = characters.Distinct().ToList();

      // check that it's a valid troop by running it through the encyclopedia 
      EncyclopediaPage pageOf = Campaign.Current.EncyclopediaManager.GetPageOf(typeof(CharacterObject));
      foreach (CharacterObject c in characters.OrderBy(co => co.Culture?.StringId))
      {
        if (pageOf.IsValidEncyclopediaItem(c))
        {
          results.AddToCounts(c, 1);
        }
      }

      return results;
    }

    public override void SyncData(IDataStore dataStore)
    {
      dataStore.SyncData("_partySettings", ref _partySettings);
      dataStore.SyncData("_garrisonSettings", ref _garrisonSettings);
      dataStore.SyncData("_caravanSettings", ref _caravanSettings);
      dataStore.SyncData("_partyTemplates", ref _partyTemplates);
      _partySettings ??= new Dictionary<Hero, PartyAIClanPartySettings>();
      _garrisonSettings ??= new Dictionary<Settlement, PartyAIClanPartySettings>();
      _caravanSettings ??= new Dictionary<Hero, PartyAIClanPartySettings>();
      _partyTemplates ??= new List<PAICustomTemplate>();

      // set default fallback values here
      if (!dataStore.SyncData("AllowTroopConversion", ref AllowTroopConversion) && dataStore.IsLoading)
      {
        AllowTroopConversion = false;
      }

      if (!dataStore.SyncData("AllowTroopConversionForCaravans", ref AllowTroopConversionForCaravans) && dataStore.IsLoading)
      {
        AllowTroopConversionForCaravans = true;
      }

      if (!dataStore.SyncData("AllowTroopConversionForGarrisons", ref AllowTroopConversionForGarrisons) && dataStore.IsLoading)
      {
        AllowTroopConversionForGarrisons = true;
      }

      if (!dataStore.SyncData("ManageCaravans", ref ManageCaravans) && dataStore.IsLoading)
      {
        ManageCaravans = false;
      }

      if (!dataStore.SyncData("ManageClanGarrisons", ref ManageClanGarrisons) && dataStore.IsLoading)
      {
        ManageClanGarrisons = false;
      }

      if (!dataStore.SyncData("ManageKingdomParties", ref ManageKingdomParties) && dataStore.IsLoading)
      {
        ManageKingdomParties = false;
      }

      if (!dataStore.SyncData("ManageKingdomGarrisons", ref ManageKingdomGarrisons) && dataStore.IsLoading)
      {
        ManageKingdomGarrisons = false;
      }

      if (!dataStore.SyncData("TroopsConvertedPerDay", ref TroopsConvertedPerDay) && dataStore.IsLoading)
      {
        TroopsConvertedPerDay = 4;
      }

      if (!dataStore.SyncData("AutoCreateClanParties", ref AutoCreateClanParties) && dataStore.IsLoading)
      {
        AutoCreateClanParties = false;
      }

      if (!dataStore.SyncData("AutoCreateClanPartiesMax", ref AutoCreateClanPartiesMax) && dataStore.IsLoading)
      {
        AutoCreateClanPartiesMax = 0;
      }

      if (!dataStore.SyncData("AutoCreateClanPartiesRoster", ref AutoCreateClanPartiesRoster) && dataStore.IsLoading)
      {
        AutoCreateClanPartiesRoster = new List<Hero>();
      }

      if (!dataStore.SyncData("_defaultClanPartySettings", ref _defaultClanPartySettings) && dataStore.IsLoading)
      {
        _defaultClanPartySettings = new((Hero)null);
      }

      if (!dataStore.SyncData("_defaultClanCaravanSettings", ref _defaultClanCaravanSettings) && dataStore.IsLoading)
      {
        _defaultClanCaravanSettings = new((Hero)null);
      }

      if (!dataStore.SyncData("_defaultClanGarrisonSettings", ref _defaultClanGarrisonSettings) && dataStore.IsLoading)
      {
        _defaultClanGarrisonSettings = new((Hero)null);
      }

      if (!dataStore.SyncData("_defaultKingdomPartySettings", ref _defaultKingdomPartySettings) && dataStore.IsLoading)
      {
        _defaultKingdomPartySettings = new((Hero)null);
      }

      if (!dataStore.SyncData("_defaultKingdomGarrisonSettings", ref _defaultKingdomGarrisonSettings) && dataStore.IsLoading)
      {
        _defaultKingdomGarrisonSettings = new((Hero)null);
      }

      if (!dataStore.SyncData("AggressivePatrols", ref AggressivePatrols) && dataStore.IsLoading)
      {
        AggressivePatrols = false;
      }

      if (!dataStore.SyncData("AIRecruitCulture", ref AIRecruitCulture) && dataStore.IsLoading)
      {
        AIRecruitCulture = false;
      }

      if (!dataStore.SyncData("ControlPanelModiferKey", ref ControlPanelModiferKey) && dataStore.IsLoading)
      {
        ControlPanelModiferKey = InputKey.LeftControl;
      }

      if (!dataStore.SyncData("ControlPanelKey", ref ControlPanelKey) && dataStore.IsLoading)
      {
        ControlPanelKey = InputKey.P;
      }

      if (!dataStore.SyncData("CommandedPartiesModiferKey", ref CommandedPartiesModiferKey) && dataStore.IsLoading)
      {
        CommandedPartiesModiferKey = InputKey.LeftAlt;
      }

      if (!dataStore.SyncData("CommandedPartiesKey", ref CommandedPartiesKey) && dataStore.IsLoading)
      {
        CommandedPartiesKey = InputKey.X;
      }

      if (!dataStore.SyncData("CommandPartiesKey", ref CommandPartiesKey) && dataStore.IsLoading)
      {
        CommandPartiesKey = InputKey.LeftAlt;
      }
    }
  }
}
