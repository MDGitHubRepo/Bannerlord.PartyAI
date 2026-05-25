using HarmonyLib;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;
using static Bannerlord.PartyAI.PAICustomOrder;

namespace Bannerlord.PartyAI.HarmonyPatches
{
    internal class AiMilitaryBehaviorPatches
    {
        public static void Apply(Harmony harmony)
        {
            harmony.Patch<AiMilitaryBehavior>()
                .Method("AiHourlyTick")
                    .Postfix(AiHourlyTickPostfix);
        }

        private static void AiHourlyTickPostfix(MobileParty mobileParty, PartyThinkParams p)
        {
            PreventSoloRaidingAndSieging(mobileParty, p);

            var leaderParty = mobileParty.Army?.LeaderParty;
            if (leaderParty is not null && mobileParty == leaderParty)
            {
                RemoveBusyArmyMemberCandidates(leaderParty, p);
            }
        }

        private static void PreventSoloRaidingAndSieging(MobileParty mobileParty, PartyThinkParams partyThinkParams)
        {
            if (!SubModule.PartySettingsManager.IsHeroManageable(mobileParty.LeaderHero))
            {
                return;
            }

            PartyAIClanPartySettings heroSettings = SubModule.PartySettingsManager.Settings(mobileParty.LeaderHero);

            if (heroSettings.AllowRaidVillages && heroSettings.AllowSieging)
            {
                return;
            }

            List<AIBehaviorData> overrides = new();

            for (int i = 0; i < partyThinkParams.AIBehaviorScores.Count; i++)
            {
                (AIBehaviorData, float) score = partyThinkParams.AIBehaviorScores[i];
                if (score.Item1.AiBehavior == AiBehavior.RaidSettlement && !heroSettings.AllowRaidVillages)
                {
                    overrides.Add(score.Item1);
                }

                if (score.Item1.AiBehavior == AiBehavior.BesiegeSettlement && !heroSettings.AllowSieging)
                {
                    overrides.Add(score.Item1);
                }
            }

            foreach (AIBehaviorData o in overrides)
            {
                partyThinkParams.SetBehaviorScore(o, 0f);
            }
        }

        private static void RemoveBusyArmyMemberCandidates(MobileParty leaderParty, PartyThinkParams partyThinkParams)
        {
            List<MobileParty> armyMembers = partyThinkParams.PossibleArmyMembersUponArmyCreation;

            if (armyMembers is null)
            {
                return;
            }

            for (int index = armyMembers.Count - 1; index >= 0; index--)
            {
                MobileParty candidate = armyMembers[index];

                if (candidate?.LeaderHero == null
                    || candidate.LeaderHero.Equals(leaderParty?.LeaderHero))
                {
                    continue;
                }

                if (!SubModule.PartySettingsManager.IsHeroManageable(candidate.LeaderHero))
                {
                    continue;
                }

                PartyAIClanPartySettings settings = SubModule.PartySettingsManager.Settings(candidate.LeaderHero);

                if (!settings.AllowAllowJoinArmies)
                {
                    armyMembers.RemoveAt(index);
                    continue;
                }

                if (!settings.AllowSieging && IsArmyBesieging(leaderParty.Army))
                {
                    armyMembers.RemoveAt(index);
                    continue;
                }

                if (!settings.AllowRaidVillages && IsArmyRaiding(leaderParty.Army))
                {
                    armyMembers.RemoveAt(index);
                    continue;
                }

                if (settings.HasActiveOrder && settings.Order.Behavior == OrderType.RecruitFromTemplate)
                {
                    armyMembers.RemoveAt(index);
                    continue;
                }
            }
        }

        private static bool IsArmyBesieging(Army army)
        {
            if (army == null)
                return false;

            // In newer versions AIBehavior / AIBehaviorFlags are gone; ArmyType is enough here.
            return army.ArmyType == Army.ArmyTypes.Besieger;
        }

        private static bool IsArmyRaiding(Army army)
        {
            if (army == null)
                return false;

            return army.ArmyType == Army.ArmyTypes.Raider;
        }
    }
}
