using Bannerlord.PartyAI.Domain;
using Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.Categories;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Bannerlord.PartyAI.CampaignBehaviors;

public class PartyAutoCreationBehavior : CampaignBehaviorBase
{
    private bool _autoCreateClanParties;
    private int _autoCreateClanPartiesMax;
    private List<Hero> _autoCreateClanPartiesRoster;

    public PartyAutoCreationBehavior()
    {
        _autoCreateClanParties = false;
        _autoCreateClanPartiesMax = 0;
        _autoCreateClanPartiesRoster = new List<Hero>();
    }

    public bool AutoCreateClanParties => _autoCreateClanParties;

    public int AutoCreateClanPartiesMax => _autoCreateClanPartiesMax;

    public ReadOnlyCollection<Hero> AutoCreateClanPartiesRoster => _autoCreateClanPartiesRoster.AsReadOnly();

    public override void RegisterEvents()
    {
        CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("AutoCreateClanParties", ref _autoCreateClanParties);
        dataStore.SyncData("AutoCreateClanPartiesMax", ref _autoCreateClanPartiesMax);
        dataStore.SyncData("AutoCreateClanPartiesRoster", ref _autoCreateClanPartiesRoster);
    }

    public void UpdateSettings(
        bool autoCreateClanParties,
        int autoCreateClanPartiesMax,
        List<Hero> autoCreateClanPartiesRoster)
    {
        _autoCreateClanParties = autoCreateClanParties;
        _autoCreateClanPartiesMax = autoCreateClanPartiesMax;
        _autoCreateClanPartiesRoster = autoCreateClanPartiesRoster;
    }

    private void OnDailyTick()
    {
        RemoveUnsuitableHeroesFromRoster();
        ImplementAutoCreateClanParties();
    }

    private void RemoveUnsuitableHeroesFromRoster()
    {
        for (int i = _autoCreateClanPartiesRoster.Count - 1; i >= 0; i--)
        {
            var hero = _autoCreateClanPartiesRoster[i];
            
            if (hero.IsDead || hero.IsDisabled)
            {
                _autoCreateClanPartiesRoster.RemoveAt(i);
            }
        }
    }

    private void ImplementAutoCreateClanParties()
    {
        if (!_autoCreateClanParties)
        {
            return;
        }

        if (_autoCreateClanPartiesMax > 0
            && ActiveClanParties(Clan.PlayerClan).Count() >= _autoCreateClanPartiesMax)
        {
            return;
        }

        while (Clan.PlayerClan.WarPartyComponents.Count < Clan.PlayerClan.WarPartyLimit)
        {
            ClanPartiesVM stockVM = new(() => { }, null, () => { }, (i) => { });

            if (!stockVM.CanCreateNewParty)
            {
                return;
            }

            IEnumerable<Hero> eligibleLeadersQuery = Clan.PlayerClan.Heroes
                .Where((Hero h) => !h.IsDisabled)
                .Union(Clan.PlayerClan.Companions)
                .Where(h => h.IsActive
                    && !h.IsReleased
                    && !h.IsFugitive
                    && !h.IsPrisoner
                    && !h.IsChild
                    && h != Hero.MainHero
                    && h.CanLeadParty()
                    && !h.IsPartyLeader
                    && h.GovernorOf == null
                    && h.PartyBelongedTo == null
                    && (!h.CurrentSettlement?.IsUnderSiege ?? true));

            if (_autoCreateClanPartiesRoster.Count > 0)
            {
                eligibleLeadersQuery = eligibleLeadersQuery.Where(_autoCreateClanPartiesRoster.Contains);
            }

            if (eligibleLeadersQuery.Count() == 0)
            {
                return;
            }

            var eligibleLeaders = eligibleLeadersQuery.ToArray();

            Hero leader = eligibleLeaders.GetRandomElement();
            Settlement? settlement = Navigation.FindNearestSettlement(leader.GetMapPoint());
            MobileParty newParty = MobilePartyHelper.CreateNewClanMobileParty(leader, Clan.PlayerClan);

            InformationManager.DisplayMessage(
                new InformationMessage(
                    new TextObject("{=PAIJPxU5978}{HERO} has created a new party near {SETTLEMENT}")
                        .SetTextVariable("HERO", leader.Name)
                        .SetTextVariable("SETTLEMENT", settlement?.Name)
                        .ToString(), Colors.Gray));

            if (_autoCreateClanPartiesMax > 0
                && ActiveClanParties(Clan.PlayerClan).Count() >= _autoCreateClanPartiesMax)
            {
                break;
            }
        }
    }

    private IEnumerable<WarPartyComponent> ActiveClanParties(Clan c)
        => c.WarPartyComponents.Where(p => p.MobileParty != MobileParty.MainParty);
}
