using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using static PartyAIControls.PAICustomOrder;
using static TaleWorlds.CampaignSystem.Party.MobileParty;

namespace PartyAIControls
{
  public class PartyAIClanPartySettings
  {
    [SaveableProperty(1)] public Hero Hero { get; private set; }
    [SaveableProperty(2)] public bool AllowAllowJoinArmies { get; set; } = true;
    [SaveableProperty(3)] public bool AllowDonateTroops { get; set; } = true;
    [SaveableProperty(4)] public bool AllowRaidVillages { get; set; } = true;
    [SaveableProperty(5)] public PAICustomTemplate PartyTemplate { get; set; }
    [SaveableProperty(6)] public PartyCompositionObect Composition { get; set; }
    [SaveableProperty(7)] public bool AllowLordPrisoners { get; set; } = true;
    [SaveableProperty(8)] public PAICustomOrder Order { get; private set; }
    [SaveableProperty(9)] public PartyObjective CachedPartyObjective { get; set; }
    [SaveableProperty(10)] public bool AllowSieging { get; set; } = true;
    [SaveableProperty(11)] public Settlement Settlement { get; private set; }
    [SaveableProperty(12)] public bool BuyHorses { get; set; }
    [SaveableProperty(13)] public int BuyHorsesBudget { get; set; } = 500;
    [SaveableProperty(14)] public int BuyHorsesBudgetToday { get; private set; } = 500;
    [SaveableProperty(15)] public int MaxTroopTier { get; set; }
    [SaveableProperty(16)] public int TroopsConvertibleToday { get; private set; } = 5;
    [SaveableProperty(17)] public PAICustomOrder FallbackOrder { get; set; }
    [SaveableProperty(18)] public bool AllowRecruitment { get; set; } = true;
    [SaveableProperty(19)] public bool FilterSettlements { get; set; } = false;
    [SaveableProperty(20)] public List<Settlement> FilteredSettlements { get; set; } = new();
    [SaveableProperty(21)] public List<PAICustomOrder> OrderQueue { get; set; } = new();
    [SaveableProperty(22)] public bool AutoRecruitment { get; set; } = true;
    [SaveableProperty(23)] public float AutoRecruitmentPercentage { get; set; } = 0.5f;
    [SaveableProperty(24)] public bool DismissUnwantedTroops { get; set; } = false;
    [SaveableProperty(25)] public float DismissUnwantedTroopsPercentage { get; set; } = 0.8f;
    [SaveableProperty(26)] public bool AllowTakeTroopsFromSettlement { get; set; } = false;
    [SaveableProperty(27)] public float PatrolRadius { get; set; } = 1f;
    [SaveableProperty(28)] public bool RecruitFromEnemySettlements { get; set; } = false;

    public PartyAIClanPartySettings(Hero hero)
    {
      Hero = hero;
      PartyTemplate = null;
      Composition = new PartyCompositionObect(0.35f, 0.30f, 0.20f, 0.15f);
    }

    public PartyAIClanPartySettings(Settlement settlement)
    {
      Settlement = settlement;
      PartyTemplate = null;
      Composition = new PartyCompositionObect(0.35f, 0.30f, 0.20f, 0.15f);
    }

    private PartyAIClanPartySettings(PartyAIClanPartySettings cloneFrom, Hero hero, Settlement settlement)
    {
      if (hero != null)
      {
        Hero = hero;
      }
      else
      {
        Settlement = settlement;
      }
      PartyTemplate = cloneFrom.PartyTemplate;
      Composition = cloneFrom.Composition.Clone();
      CopyOptionsFrom(cloneFrom);
    }

    internal void SetOrder(PAICustomOrder order)
    {
      if (order == null || Settlement != null)
      {
        return;
      }
      if (Hero.PartyBelongedTo?.Army != null && Hero.PartyBelongedTo.Army.LeaderParty.LeaderHero != Hero)
      {
        Hero.PartyBelongedTo.Army = null;
      }

      if (HasActiveOrder)
      {
        OrderQueue.Insert(0, Order);
      }

      Order = order;
    }

    internal bool HasActiveOrder => Order != null && Order.Behavior != OrderType.None;

    internal void ClearOrder()
    {
      if (Settlement != null) { return; }
      if (Hero.IsPartyLeader && Hero.PartyBelongedTo != null && HasActiveOrder)
      {
        Hero.PartyBelongedTo.SetPartyObjective(CachedPartyObjective);
      }
      Hero.PartyBelongedTo?.Ai.SetDoNotMakeNewDecisions(false);

      Order = null;

      if (OrderQueue.Count > 0)
      {
        Order = OrderQueue[0];
        OrderQueue.RemoveAt(0);
      }
    }

    internal void CopyOptionsFrom(PartyAIClanPartySettings settings)
    {
      AllowAllowJoinArmies = settings.AllowAllowJoinArmies;
      AllowDonateTroops = settings.AllowDonateTroops;
      AllowSieging = settings.AllowSieging;
      AllowRaidVillages = settings.AllowRaidVillages;
      AllowLordPrisoners = settings.AllowLordPrisoners;
      BuyHorses = settings.BuyHorses;
      BuyHorsesBudget = settings.BuyHorsesBudget;
      MaxTroopTier = settings.MaxTroopTier;
      FallbackOrder = settings.FallbackOrder;
      AllowRecruitment = settings.AllowRecruitment;
      FilterSettlements = settings.FilterSettlements;
      FilteredSettlements = settings.FilteredSettlements?.ToList() ?? new();
      AutoRecruitment = settings.AutoRecruitment;
      AutoRecruitmentPercentage = settings.AutoRecruitmentPercentage;
      DismissUnwantedTroops = settings.DismissUnwantedTroops;
      DismissUnwantedTroopsPercentage = settings.DismissUnwantedTroopsPercentage;
      PatrolRadius = settings.PatrolRadius;
      RecruitFromEnemySettlements = settings.RecruitFromEnemySettlements;
      ResetBudgets();
    }

    internal PartyAIClanPartySettings Clone(Hero hero, Settlement settlement = null) => new(this, hero, settlement);

    internal void ResetBudgets()
    {
      BuyHorsesBudgetToday = BuyHorsesBudget;
      TroopsConvertibleToday = SubModule.PartySettingsManager.TroopsConvertedPerDay > 0 ? SubModule.PartySettingsManager.TroopsConvertedPerDay : int.MaxValue;
    }

    internal void DeductHorseBudget(int amount) => BuyHorsesBudgetToday -= amount;
    internal void DeductTroopsConvertibleToday(int amount) => TroopsConvertibleToday -= amount;

    internal void SetPartyTemplate(PAICustomTemplate template)
    {
      PartyTemplate = template;

      // Only affect recruiting targets
      if (HasActiveOrder && Order?.Behavior == PAICustomOrder.OrderType.RecruitFromTemplate && template != null)
      {
        if (Order.Target is Settlement settlement)
        {
          var cultures = template.TroopCultures;
          bool unrestricted = cultures == null || cultures.Count == 0;
          if (!unrestricted && !cultures.Contains(settlement.Culture))
          {
            Order.Target = null;
          }
        }
        else
        {
          // If recruit order is active but target isn't a Settlement, clear it defensively.
          Order.Target = null;
        }
      }

      // Unlock party AI so it will re-evaluate on next hourly tick
      MobileParty ownedParty = Hero?.PartyBelongedTo;
      if (ownedParty?.Ai != null)
      {
        ownedParty.Ai.SetDoNotMakeNewDecisions(false);
        ownedParty.Ai.RethinkAtNextHourlyTick = true;
      }
    }
  }

  public class PartyCompositionObect
  {
    [SaveableProperty(1)] public float Infantry { get; set; }
    [SaveableProperty(2)] public float Ranged { get; set; }
    [SaveableProperty(3)] public float Cavalry { get; set; }
    [SaveableProperty(4)] public float HorseArcher { get; set; }

    public PartyCompositionObect(float infantry, float ranged, float cavalry, float horseArcher)
    {
      Infantry = infantry;
      Ranged = ranged;
      Cavalry = cavalry;
      HorseArcher = horseArcher;
    }

    public PartyCompositionObect()
    {
      Infantry = 0;
      Ranged = 0;
      Cavalry = 0;
      HorseArcher = 0;
    }

    public void Scale(float scalar)
    {
      Infantry *= scalar;
      Ranged *= scalar;
      Cavalry *= scalar;
      HorseArcher *= scalar;
    }

    public float this[FormationClass i]
    {
      get
      {
        return i switch
        {
          FormationClass.Infantry => Infantry,
          FormationClass.Ranged => Ranged,
          FormationClass.Cavalry => Cavalry,
          FormationClass.HorseArcher => HorseArcher,
          _ => 0,
        };
      }
      set
      {
        switch (i)
        {
          case FormationClass.Infantry: Infantry = value; break;
          case FormationClass.Ranged: Ranged = value; break;
          case FormationClass.Cavalry: Cavalry = value; break;
          case FormationClass.HorseArcher: HorseArcher = value; break;
          default: break;
        }
      }
    }

    public PartyCompositionObect Clone()
    {
      return new PartyCompositionObect(Infantry, Ranged, Cavalry, HorseArcher);
    }
  }

  public class PAIDetatchmentConfig
  {
    public enum DetatchmentType
    {
      Recruiter
    }

    [SaveableProperty(1)] public IMapPoint Target { get; set; }
    [SaveableProperty(2)] public DetatchmentType Type { get; set; }
    [SaveableProperty(3)] public MobileParty Party { get; set; }

    public PAIDetatchmentConfig(DetatchmentType type, IMapPoint target)
    {
      Type = type;
      Target = target;
    }
  }

  public class PAICustomOrder
  {
    public enum OrderType
    {
      None,
      PatrolAroundPoint,
      BesiegeSettlement,
      DefendSettlement,
      PatrolClanLands,
      EscortParty,
      StayInSettlement,
      AttackParty,
      RecruitFromTemplate,
      VisitSettlement
    }
    [SaveableProperty(1)] public IMapPoint Target { get; set; }
    [SaveableProperty(2)] public OrderType Behavior { get; set; }

    public PAICustomOrder(IMapPoint target, OrderType behavior)
    {
      Target = target;
      Behavior = behavior;
    }

    public TextObject Text
    {
      get
      {
        return Behavior switch
        {
          OrderType.None => new TextObject("{=PAIZZ1tGdbA}No Active Order"),
          OrderType.PatrolAroundPoint => new TextObject("{=yUVv3z5V}Patrolling around {TARGET_SETTLEMENT}").SetTextVariable("TARGET_SETTLEMENT", ((Settlement)Target).Name),
          OrderType.BesiegeSettlement => new TextObject("{=JTxI3sW2}Besieging {TARGET_SETTLEMENT}").SetTextVariable("TARGET_SETTLEMENT", ((Settlement)Target).Name),
          OrderType.DefendSettlement => new TextObject("{=rGy8vjOv}Defending {TARGET_SETTLEMENT}").SetTextVariable("TARGET_SETTLEMENT", ((Settlement)Target).Name),
          OrderType.StayInSettlement => new TextObject("{=PAIdTWGYLu0}Staying in {TARGET_SETTLEMENT}").SetTextVariable("TARGET_SETTLEMENT", ((Settlement)Target).Name),
          OrderType.EscortParty => new TextObject("{=OpzzCPiP}Following {TARGET_PARTY}").SetTextVariable("TARGET_PARTY", ((MobileParty)Target)?.Name),
          OrderType.AttackParty => new TextObject("{=exnL6SS7}Attacking {TARGET_SETTLEMENT}").SetTextVariable("TARGET_SETTLEMENT", ((MobileParty)Target)?.Name),
          OrderType.PatrolClanLands => new TextObject("{=PAI0oBFsSJO}Patrolling Clan Territory"),
          OrderType.RecruitFromTemplate => new TextObject("{=PAIImuFNGIe}Recruiting Troops"),
          OrderType.VisitSettlement => new TextObject("{=PAIzp4R8TTM}Visiting {SETTLEMENT}").SetTextVariable("SETTLEMENT", ((Settlement)Target).Name),
          _ => null,
        };
      }
    }

    public TextObject QueueText
    {
      get
      {
        return Behavior switch
        {
          OrderType.None => new TextObject("{=PAISXYCwfO9}No orders in queue"),
          OrderType.PatrolAroundPoint => new TextObject("{=PAIpc5Yu18Z}Patrol around {TARGET_SETTLEMENT}").SetTextVariable("TARGET_SETTLEMENT", ((Settlement)Target).Name),
          OrderType.BesiegeSettlement => new TextObject("{=PAIPMS0nSSq}Besiege {TARGET_SETTLEMENT}").SetTextVariable("TARGET_SETTLEMENT", ((Settlement)Target).Name),
          OrderType.DefendSettlement => new TextObject("{=PAITOricrPO}Defend {TARGET_SETTLEMENT}").SetTextVariable("TARGET_SETTLEMENT", ((Settlement)Target).Name),
          OrderType.StayInSettlement => new TextObject("{=PAIj66iTjmT}Stay in {TARGET_SETTLEMENT}").SetTextVariable("TARGET_SETTLEMENT", ((Settlement)Target).Name),
          OrderType.EscortParty => new TextObject("{=PAINt8jD9tc}Follow {TARGET_PARTY}").SetTextVariable("TARGET_PARTY", ((MobileParty)Target)?.Name),
          OrderType.AttackParty => new TextObject("{=PAIDycETWvm}Attack {TARGET_SETTLEMENT}").SetTextVariable("TARGET_SETTLEMENT", ((MobileParty)Target)?.Name),
          OrderType.PatrolClanLands => new TextObject("{=PAIgvZTEG1V}Patrol Clan Territory"),
          OrderType.RecruitFromTemplate => new TextObject("{=PAIhBXucHBM}Recruit Troops"),
          OrderType.VisitSettlement => new TextObject("{=PAIRyxa5pnP}Visit {SETTLEMENT}").SetTextVariable("SETTLEMENT", ((Settlement)Target).Name),
          _ => null,
        };
      }
    }
  }

  public class PAIHeroInventoryListener : InventoryListener
  {
    private readonly MobileParty _mobileParty;

    public PAIHeroInventoryListener(MobileParty mobileParty)
    {
      _mobileParty = mobileParty;
    }

    public override int GetGold()
    {
      return _mobileParty.PartyTradeGold;
    }

    public override TextObject GetTraderName()
    {
      return _mobileParty.Name;
    }

    public override void SetGold(int gold)
    {
    }

    public override void OnTransaction()
    {
    }

    public override PartyBase GetOppositeParty()
    {
      return null;
    }
  }
}
