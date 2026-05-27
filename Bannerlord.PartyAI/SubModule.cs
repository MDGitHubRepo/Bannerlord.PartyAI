using Bannerlord.PartyAI.CampaignBehaviors;
using Bannerlord.PartyAI.Models;
using Bannerlord.PartyAI.Patches;
using Bannerlord.UIExtenderEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.PartyAI;

public class SubModule : MBSubModuleBase
{
    private static readonly string Namespace = typeof(SubModule).Namespace;

    private Harmony _harmony = new(Namespace);

    private bool _isChoosePartiesPopupOpenAlready = false;

    internal static PartyAIClanPartySettingsManager PartySettingsManager;
    internal static PartyAITroopRecruiter PartyTroopRecruiter;
    internal static PartyAIThinker PartyThinker;
    internal static PartyAIDetachmentManager DetachmentManager;
    internal static PAInformationManager InformationManager;

    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
    {
        if ((game.GameType is not Campaign))
        {
            return;
        }

        CampaignGameStarter campaignGameStarter = (CampaignGameStarter)gameStarterObject;

        PartySettingsManager = new PartyAIClanPartySettingsManager();
        campaignGameStarter.AddBehavior(PartySettingsManager);

        PartyTroopRecruiter = new PartyAITroopRecruiter();
        campaignGameStarter.AddBehavior(PartyTroopRecruiter);

        PartyThinker = new PartyAIThinker();
        campaignGameStarter.AddBehavior(PartyThinker);

        //DetachmentManager = new();
        //campaignGameStarter.AddBehavior(DetachmentManager);

        //campaignGameStarter.AddBehavior(new PartyAIFoodBuyer());

        campaignGameStarter.AddModel(new PAITroopUpgradeModel(GetGameModel<PartyTroopUpgradeModel>(gameStarterObject)));
        campaignGameStarter.AddModel(new PAIPrisonerRecruitmentCalculationModel(GetGameModel<PrisonerRecruitmentCalculationModel>(gameStarterObject)));
        campaignGameStarter.AddModel(new PAISettlementGarrisonModel(GetGameModel<SettlementGarrisonModel>(gameStarterObject)));
        campaignGameStarter.AddModel(new PAIPartyFoodBuyingModel(GetGameModel<PartyFoodBuyingModel>(gameStarterObject)));

        InformationManager = new();
    }

    public override void OnGameInitializationFinished(Game game)
    {
        if (game.GameType is not Campaign)
        {
            return;
        }

        ValidateGameModel(Campaign.Current.Models.PartyTroopUpgradeModel);
        ValidateGameModel(Campaign.Current.Models.ArmyManagementCalculationModel);
        ValidateGameModel(Campaign.Current.Models.PrisonerRecruitmentCalculationModel);
        ValidateGameModel(Campaign.Current.Models.SettlementGarrisonModel);
        ValidateGameModel(Campaign.Current.Models.PartyFoodBuyingModel);

        string keycombo = PartySettingsManager.ControlPanelModiferKey.ToString() + "+" + PartySettingsManager.ControlPanelKey.ToString();
        TaleWorlds.Library.InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=PAIEUwVpMPm}Thank you for using Party AI Controls! To access the configuration panel, press {KEYBIND}!").SetTextVariable("KEYBIND", keycombo).ToString(), Colors.Green));
    }

    private void ValidateGameModel(GameModel model)
    {
        if (model.GetType().Assembly == GetType().Assembly) { return; }
        if (!model.GetType().BaseType.IsAbstract)
        {
            TextObject error = new("{=I2LlBDKr}Game Model Error: Please move " + GetType().Assembly.GetName().Name + " below " + model.GetType().Assembly.GetName().Name + " in your load order to ensure mod compatibility");
            TaleWorlds.Library.InformationManager.DisplayMessage(new InformationMessage(error.ToString(), Colors.Red));
        }
    }

    protected override void OnApplicationTick(float dt)
    {
        var activeState = Game.Current?.GameStateManager?.ActiveState;

        if (activeState == null
                  || activeState is not MapState
                  || activeState.IsMenuState
                  || activeState is MissionState
                  || Mission.Current != null
           )
        {
            return;
        }

        if ((Input.IsKeyDown(PartySettingsManager.ControlPanelModiferKey)
            || PartySettingsManager.ControlPanelModiferKey == InputKey.Invalid)
            && Input.IsKeyDown(PartySettingsManager.ControlPanelKey))
        {
            GameStateManager.Current.PushState(GameStateManager.Current.CreateState<PartyAIControlsMenuState>());
            return;
        }

        if ((Input.IsKeyDown(PartySettingsManager.CommandedPartiesModiferKey) || PartySettingsManager.CommandedPartiesModiferKey == InputKey.Invalid) && Input.IsKeyDown(PartySettingsManager.CommandedPartiesKey))
        {
            if (_isChoosePartiesPopupOpenAlready) { return; }
            CampaignTimeControlMode mode = Campaign.Current.TimeControlMode;
            Campaign.Current.TimeControlMode = CampaignTimeControlMode.FastForwardStop;
            string title = new TextObject("{=PAIFHytp3D7}Choose which parties to directly command").ToString();
            string desc = new TextObject("{=PAIRzSgh49H}Parties must be manageable and in visual range to appear here.").ToString();
            List<InquiryElement> list = MobileParty.AllLordParties.Where(m => PartySettingsManager.IsHeroManageable(m.LeaderHero) && m.GetPosition2D.Distance(MobileParty.MainParty.GetPosition2D) <= MobileParty.MainParty.SeeingRange).Where(m => m.Army == null || m.Army.LeaderParty == m).OrderByDescending(m => m.ActualClan.Equals(Clan.PlayerClan)).ThenBy(m => m.Name?.ToString()).ToList().ConvertAll(m => new InquiryElement(m, m.Name.ToString(), new CharacterImageIdentifier(CharacterCode.CreateFrom(m.LeaderHero?.CharacterObject))));

            MBInformationManager.ShowMultiSelectionInquiry(
              new(title, desc, list, isExitShown: true, minSelectableOptionCount: 0, maxSelectableOptionCount: list.Count, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(),
                affirmativeAction: (List<InquiryElement> results) =>
                {
                    PartyThinker.ClearAssumingDirectControl();
                    foreach (InquiryElement e in results)
                    {
                        if (e.Identifier is MobileParty m)
                        {
                            PartyThinker.AddToAssumingDirectControl(m);
                        }
                    }
                    _isChoosePartiesPopupOpenAlready = false;
                    Campaign.Current.TimeControlMode = mode;
                },
                (List<InquiryElement> results) =>
                {
                    _isChoosePartiesPopupOpenAlready = false;
                    Campaign.Current.TimeControlMode = mode;
                }, isSeachAvailable: true
              )
            );
            _isChoosePartiesPopupOpenAlready = true;
        }
    }

    protected override void OnSubModuleLoad()
    {
        ApplyPatches(_harmony);

        var extender = UIExtender.Create(Namespace);
        extender.Register(typeof(SubModule).Assembly);
        extender.Enable();

        base.OnSubModuleLoad();
    }

    protected override void OnBeforeInitialModuleScreenSetAsRoot()
    {
        TryApplyBannerKingsConflictPatches(_harmony);

        base.OnBeforeInitialModuleScreenSetAsRoot();
    }

    private static void ApplyPatches(Harmony harmony)
    {
        harmony.PatchAll();

        AiMilitaryBehaviorPatches.Apply(harmony);
        AiVisitSettlementBehaviorPatches.Apply(harmony);
        ArmyPatches.Apply(harmony);
        CampaignEventDispatcherPatches.Apply(harmony);
        CaravansCampaignBehaviorPatches.Apply(harmony);
        DisbandArmyActionPatches.Apply(harmony);
        FixModdedGameStateScreenCrashOnShow.Apply(harmony);
        GauntletClanScreenPatches.Apply(harmony);
        InventoryLogicPatches.Apply(harmony);
        LeaveTroopsToSettlementActionPatch.Apply(harmony);
        MobilePartyAiPatches.Apply(harmony);
        MobilePartyPatches.Apply(harmony);
        PartiesBuyHorseCampaignBehaviorPatch.Apply(harmony);
        PartyVMPatches.Apply(harmony);
        RecruitmentCampaignBehaviorPatches.Apply(harmony);
        TakePrisonerActionPatches.Apply(harmony);
    }

    private static void TryApplyBannerKingsConflictPatches(Harmony harmony)
    {
        var bannerKingsLoaded = AccessTools.TypeByName("BannerKings.Main") != null;

        MapBarVMPatches.Apply(harmony, bannerKingsLoaded);

        if (!bannerKingsLoaded)
        {
            ArmyManagementVMPatches.Apply(harmony);
        }
    }

    private T GetGameModel<T>(IGameStarter gameStarterObject) where T : GameModel
    {
        GameModel[] array = gameStarterObject.Models.ToArray();
        for (int index = array.Length - 1; index >= 0; --index)
        {
            if (array[index] is T gameModel)
                return gameModel;
        }
        return default(T);
    }
}
