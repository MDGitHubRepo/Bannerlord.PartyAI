using Bannerlord.PartyAI.Domain.Models;
using TaleWorlds.Localization;

namespace Bannerlord.PartyAI;

public static class OrderVerbalizer
{
    private const string NoActiveOrder = "{=PAIZZ1tGdbA}No active order";
    private const string NoOrdersInQueue = "{=PAISXYCwfO9}No orders in queue";

    public static TextObject GetStatusText(PartyAiOrder? order)
    {
        return order?.Behavior switch
        {
            PartyAiOrderType.None => new TextObject(NoActiveOrder),
            PartyAiOrderType.PatrolAroundPoint => new TextObject("{=yUVv3z5V}Patrolling around {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PartyAiOrderType.BesiegeSettlement => new TextObject("{=JTxI3sW2}Besieging {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PartyAiOrderType.DefendSettlement => new TextObject("{=rGy8vjOv}Defending {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PartyAiOrderType.StayInSettlement => new TextObject("{=PAIdTWGYLu0}Staying in {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PartyAiOrderType.EscortParty => new TextObject("{=OpzzCPiP}Following {TARGET_PARTY}")
                .SetTextVariable("TARGET_PARTY", order.Target.Name),
            PartyAiOrderType.AttackParty => new TextObject("{=exnL6SS7}Attacking {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PartyAiOrderType.PatrolClanLands => new TextObject("{=PAI0oBFsSJO}Patrolling Clan Territory"),
            PartyAiOrderType.RecruitFromTemplate => new TextObject("{=PAIImuFNGIe}Recruiting Troops"),
            PartyAiOrderType.VisitSettlement => new TextObject("{=PAIzp4R8TTM}Visiting {SETTLEMENT}")
                .SetTextVariable("SETTLEMENT", order.Target.Name),
            _ => new TextObject(NoActiveOrder),
        };
    }

    public static TextObject GetCommandText(PartyAiOrder? order)
    {
        return order?.Behavior switch
        {
            PartyAiOrderType.None => new TextObject(NoOrdersInQueue),
            PartyAiOrderType.PatrolAroundPoint => new TextObject("{=PAIpc5Yu18Z}Patrol around {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PartyAiOrderType.BesiegeSettlement => new TextObject("{=PAIPMS0nSSq}Besiege {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PartyAiOrderType.DefendSettlement => new TextObject("{=PAITOricrPO}Defend {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PartyAiOrderType.StayInSettlement => new TextObject("{=PAIj66iTjmT}Stay in {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PartyAiOrderType.EscortParty => new TextObject("{=PAINt8jD9tc}Follow {TARGET_PARTY}")
                .SetTextVariable("TARGET_PARTY", order.Target.Name),
            PartyAiOrderType.AttackParty => new TextObject("{=PAIDycETWvm}Attack {TARGET_SETTLEMENT}")
                .SetTextVariable("TARGET_SETTLEMENT", order.Target.Name),
            PartyAiOrderType.PatrolClanLands => new TextObject("{=PAIgvZTEG1V}Patrol Clan Territory"),
            PartyAiOrderType.RecruitFromTemplate => new TextObject("{=PAIhBXucHBM}Recruit Troops"),
            PartyAiOrderType.VisitSettlement => new TextObject("{=PAIRyxa5pnP}Visit {SETTLEMENT}")
                .SetTextVariable("SETTLEMENT", order.Target.Name),
            _ => new TextObject(NoOrdersInQueue),
        };
    }
}
