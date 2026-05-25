using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;

namespace Bannerlord.PartyAI.Patches;

internal class TakePrisonerActionPatches
{
    public static void Apply(Harmony harmony)
    {
        harmony.Patch()
            .Method(typeof(TakePrisonerAction), "ApplyInternal")
                .Prefix(ApplyInternalPrefix)
                .Postfix(ApplyInternalPostfix);
    }

    private static void ApplyInternalPrefix(PartyBase capturerParty, Hero prisonerCharacter, ref bool isEventCalled)
    {
        if (!SubModule.PartySettingsManager.IsHeroManageable(capturerParty?.LeaderHero))
        {
            return;
        }

        if (!SubModule.PartySettingsManager.Settings(capturerParty?.LeaderHero).AllowLordPrisoners)
        {
            isEventCalled = false;
        }
    }

    private static void ApplyInternalPostfix(PartyBase capturerParty, Hero prisonerCharacter, bool isEventCalled)
    {
        if (!SubModule.PartySettingsManager.IsHeroManageable(capturerParty?.LeaderHero))
        {
            return;
        }

        if (!SubModule.PartySettingsManager.Settings(capturerParty?.LeaderHero).AllowLordPrisoners)
        {
            EndCaptivityAction.ApplyByReleasedAfterBattle(prisonerCharacter);
        }
    }
}
