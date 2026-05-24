using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyAIControls.CampaignBehaviors
{
  internal class PartyAIDetachmentManager : CampaignBehaviorBase
  {
    private List<PAIDetatchmentConfig> _detatchments = new();

    public override void RegisterEvents()
    {
    }

    private static CampaignVec2 TryGetSettlementGatePosition(Settlement settlement)
    {
        if (settlement == null)
            return CampaignVec2.Zero;
        return PartyAIThinker.SafeGet(() => settlement.GatePosition, PartyAIThinker.SafeGet(() => new CampaignVec2(settlement.GetPosition2D, true), CampaignVec2.Zero));
    }

    internal void CreateNewDetatchment(TroopRoster roster, PAIDetatchmentConfig config)
    {
        CampaignVec2 spawnPoint;
        Settlement home;
        TextObject name = new("{=PAIxQtQFzxH}Detatchment of {NAME}");
        Clan clan;

        if (config.Target is Settlement s)
        {
            spawnPoint = TryGetSettlementGatePosition(s); // safe access
            home = s;
            name.SetTextVariable("NAME", s.Name);
            clan = s.OwnerClan;
        }
        else if (config.Target is MobileParty m)
        {
            // m.GetPosition2D is Vec2 → wrap it into CampaignVec2
            spawnPoint = new CampaignVec2(m.GetPosition2D, true);
            home = m.Owner?.HomeSettlement;
            name.SetTextVariable("NAME", m.Name);
            clan = m.ActualClan;
        }
        else
        {
            return;
        }

        config.Party = CustomPartyComponent.CreateCustomPartyWithTroopRoster(
            spawnPoint,                          // CampaignVec2
            5f,                                  // spawn radius
            home,
            name,
            clan,
            roster,
            TroopRoster.CreateDummyTroopRoster(),
            clan.Leader,
            customPartyBaseSpeed: 4f            // same as old code
        );

        _detatchments.Add(config);
    }

    internal bool IsDetatchment(MobileParty party) => party != null && _detatchments.Any(d => d.Party == party);

    public override void SyncData(IDataStore dataStore)
    {
      dataStore.SyncData("_detatchments", ref _detatchments);
    }
  }
}
