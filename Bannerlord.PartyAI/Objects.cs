using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using static Bannerlord.PartyAI.PAICustomOrder;
using static TaleWorlds.CampaignSystem.Party.MobileParty;

namespace Bannerlord.PartyAI;

public class PartyAIClanPartySettings
{
    [SaveableProperty(1)] public Hero? Hero { get; private set; }
    [SaveableProperty(2)] public bool AllowJoinArmies { get; set; } = true;
    [SaveableProperty(3)] public bool AllowDonateTroops { get; set; } = true;
    [SaveableProperty(4)] public bool AllowRaidVillages { get; set; } = true;
    [SaveableProperty(5)] public PAICustomTemplate? PartyTemplate { get; set; }
    [SaveableProperty(6)] public PartyComposition Composition { get; set; }
    [SaveableProperty(7)] public bool AllowLordPrisoners { get; set; } = true;
    [SaveableProperty(8)] public PAICustomOrder? Order { get; private set; }
    [SaveableProperty(9)] public PartyObjective CachedPartyObjective { get; set; }
    [SaveableProperty(10)] public bool AllowSieging { get; set; } = true;
    [SaveableProperty(11)] public Settlement? Settlement { get; private set; }
    [SaveableProperty(12)] public bool BuyHorses { get; set; }
    [SaveableProperty(13)] public int BuyHorsesBudget { get; set; } = 500;
    [SaveableProperty(14)] public int BuyHorsesBudgetToday { get; private set; } = 500;
    [SaveableProperty(15)] public int MaxTroopTier { get; set; }
    [SaveableProperty(16)] public int TroopsConvertibleToday { get; private set; } = 5;
    [SaveableProperty(17)] public PAICustomOrder? FallbackOrder { get; private set; }
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

    public PartyAIClanPartySettings()
    {
        Composition = new PartyComposition(0.35f, 0.30f, 0.20f, 0.15f);
    }

    public PartyAIClanPartySettings(Hero hero) : this()
    {
        Hero = hero;
    }

    public PartyAIClanPartySettings(Settlement settlement) : this()
    {
        Settlement = settlement;
    }

    public PartyAIClanPartySettings(
        PartyAIClanPartySettings cloneFrom,
        Hero? hero = null,
        Settlement? settlement = null)
    {
        if (hero is not null)
        {
            Hero = hero;
        }

        if (settlement is not null)
        {
            Settlement = settlement;
        }

        PartyTemplate = cloneFrom.PartyTemplate;
        Composition = new PartyComposition(cloneFrom.Composition);
        CopyOptionsFrom(cloneFrom);
    }

    internal void SetOrder(OrderType behavior, IMapPoint? target = null)
    {
        var order = new PAICustomOrder(behavior, target);

        if (Settlement != null)
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

    internal void SetFallbackOrder(OrderType behavior, IMapPoint? target = null)
    {
        var order = new PAICustomOrder(behavior, target);

        if (Settlement != null)
        {
            return;
        }

        FallbackOrder = order;
    }

    [MemberNotNullWhen(true, nameof(Order))]
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

    internal void ClearAllOrders()
    {
        OrderQueue.Clear();
        ClearOrder();
    }

    internal void CopyOptionsFrom(PartyAIClanPartySettings settings)
    {
        AllowJoinArmies = settings.AllowJoinArmies;
        AllowDonateTroops = settings.AllowDonateTroops;
        AllowTakeTroopsFromSettlement = settings.AllowTakeTroopsFromSettlement;
        AllowSieging = settings.AllowSieging;
        AllowRaidVillages = settings.AllowRaidVillages;
        AllowLordPrisoners = settings.AllowLordPrisoners;
        BuyHorses = settings.BuyHorses;
        Composition = new PartyComposition(settings.Composition);
        BuyHorsesBudget = settings.BuyHorsesBudget;
        MaxTroopTier = settings.MaxTroopTier;
        AllowRecruitment = settings.AllowRecruitment;
        FilterSettlements = settings.FilterSettlements;
        FilteredSettlements = settings.FilteredSettlements?.ToList() ?? new();
        OrderQueue = settings.OrderQueue?
            .Select(order => new PAICustomOrder(order))
            .ToList() ?? [];
        AutoRecruitment = settings.AutoRecruitment;
        AutoRecruitmentPercentage = settings.AutoRecruitmentPercentage;
        DismissUnwantedTroops = settings.DismissUnwantedTroops;
        DismissUnwantedTroopsPercentage = settings.DismissUnwantedTroopsPercentage;
        PatrolRadius = settings.PatrolRadius;
        RecruitFromEnemySettlements = settings.RecruitFromEnemySettlements;

        if (settings.FallbackOrder is not null)
        {
            SetFallbackOrder(
                settings.FallbackOrder.Behavior,
                settings.FallbackOrder.Target);
        }

        ResetBudgets();
    }

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

public class PartyComposition
{
    [SaveableProperty(1)] public float Infantry { get; set; }
    [SaveableProperty(2)] public float Ranged { get; set; }
    [SaveableProperty(3)] public float Cavalry { get; set; }
    [SaveableProperty(4)] public float HorseArcher { get; set; }

    public PartyComposition(float infantry, float ranged, float cavalry, float horseArcher)
    {
        Infantry = infantry;
        Ranged = ranged;
        Cavalry = cavalry;
        HorseArcher = horseArcher;
    }

    public PartyComposition() : this(0, 0, 0, 0)
    {
    }

    public PartyComposition(PartyComposition original)
        : this(original.Infantry, original.Ranged, original.Cavalry, original.HorseArcher)
    {
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
    [SaveableProperty(1)] public IMapPoint? Target { get; set; }
    [SaveableProperty(2)] public OrderType Behavior { get; set; }

    public PAICustomOrder(OrderType behavior, IMapPoint? target = null)
    {
        Target = target;
        Behavior = behavior;
    }

    public PAICustomOrder(PAICustomOrder original)
    {
        Target = original.Target;
        Behavior = original.Behavior;
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
