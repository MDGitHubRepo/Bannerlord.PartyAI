using HarmonyLib;
using HarmonyLib.BUTR.Extensions;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors;
using TaleWorlds.CampaignSystem.Party;
using static PartyAIControls.PAICustomOrder;

namespace PartyAIControls.HarmonyPatches
{
    [HarmonyPatch(typeof(AiMilitaryBehavior), "AiHourlyTick")]
    internal class AiMilitaryBehaviorPatches
    {
        public static void Apply(Harmony harmony)
        {
            var originalMethod = AccessTools2.DeclaredMethod(typeof(AiMilitaryBehavior), "AiHourlyTick");
            var postfix = AccessTools2.DeclaredMethod(typeof(AiMilitaryBehaviorPatches), nameof(AiHourlyTickPostfix));
            harmony.TryPatch(originalMethod, postfix: postfix);
        }

        private static void AiHourlyTickPostfix(MobileParty mobileParty, PartyThinkParams partyThinkParams)
        {
            PreventSoloRaidingAndSieging(mobileParty, partyThinkParams);
            RemoveBusyArmyMemberCandidates(mobileParty, partyThinkParams);
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

        private static void RemoveBusyArmyMemberCandidates(MobileParty mobileParty, PartyThinkParams p)
        {
            List<MobileParty> armyMembers = p.PossibleArmyMembersUponArmyCreation;
            var leaderParty = mobileParty.Army.LeaderParty;

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
