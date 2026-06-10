using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace Bannerlord.PartyAI;

public static class OrderVerbalizer
{
    private const string NoActiveOrder = "{=PAIZZ1tGdbA}No active order";
    private const string NoOrdersInQueue = "{=PAISXYCwfO9}No orders in queue";

    public static TextObject GetStatusText(PAICustomOrder? order)
    {
        return order?.Behavior switch
        {
            PAICustomOrder.OrderType.None => new TextObject(NoActiveOrder),
            PAICustomOrder.OrderType.PatrolAroundPoint => new TextObject("{=yUVv3z5V}Patrolling around {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PAICustomOrder.OrderType.BesiegeSettlement => new TextObject("{=JTxI3sW2}Besieging {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PAICustomOrder.OrderType.DefendSettlement => new TextObject("{=rGy8vjOv}Defending {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PAICustomOrder.OrderType.StayInSettlement => new TextObject("{=PAIdTWGYLu0}Staying in {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PAICustomOrder.OrderType.EscortParty => new TextObject("{=OpzzCPiP}Following {TARGET_PARTY}")
                .SetTextVariable("TARGET_PARTY", order.Target.Name),
            PAICustomOrder.OrderType.AttackParty => new TextObject("{=exnL6SS7}Attacking {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PAICustomOrder.OrderType.PatrolClanLands => new TextObject("{=PAI0oBFsSJO}Patrolling Clan Territory"),
            PAICustomOrder.OrderType.RecruitFromTemplate => new TextObject("{=PAIImuFNGIe}Recruiting Troops"),
            PAICustomOrder.OrderType.VisitSettlement => new TextObject("{=PAIzp4R8TTM}Visiting {SETTLEMENT}")
                .SetTextVariable("SETTLEMENT", order.Target.Name),
            _ => new TextObject(NoActiveOrder),
        };
    }

    public static TextObject GetCommandText(PAICustomOrder? order)
    {
        return order?.Behavior switch
        {
            PAICustomOrder.OrderType.None => new TextObject(NoOrdersInQueue),
            PAICustomOrder.OrderType.PatrolAroundPoint => new TextObject("{=PAIpc5Yu18Z}Patrol around {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PAICustomOrder.OrderType.BesiegeSettlement => new TextObject("{=PAIPMS0nSSq}Besiege {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PAICustomOrder.OrderType.DefendSettlement => new TextObject("{=PAITOricrPO}Defend {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PAICustomOrder.OrderType.StayInSettlement => new TextObject("{=PAIj66iTjmT}Stay in {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PAICustomOrder.OrderType.EscortParty => new TextObject("{=PAINt8jD9tc}Follow {TARGET_PARTY}")
                .SetTextVariable("TARGET_PARTY", order.Target.Name),
            PAICustomOrder.OrderType.AttackParty => new TextObject("{=PAIDycETWvm}Attack {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PAICustomOrder.OrderType.PatrolClanLands => new TextObject("{=PAIgvZTEG1V}Patrol Clan Territory"),
            PAICustomOrder.OrderType.RecruitFromTemplate => new TextObject("{=PAIhBXucHBM}Recruit Troops"),
            PAICustomOrder.OrderType.VisitSettlement => new TextObject("{=PAIRyxa5pnP}Visit {SETTLEMENT}")
                .SetTextVariable("SETTLEMENT", order.Target.Name),
            _ => new TextObject(NoOrdersInQueue),
        };
    }
}
