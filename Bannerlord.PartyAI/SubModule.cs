using Bannerlord.UIExtenderEx;
using HarmonyLib;
using PartyAIControls.CampaignBehaviors;
using PartyAIControls.HarmonyPatches;
using PartyAIControls.Models;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.ArmyManagement;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace PartyAIControls
{
  public class SubModule : MBSubModuleBase
  {
    private Harmony _harmony = null;
    private bool _harmonyRan = false;
    private bool _UIExtenderRan = false;
    private bool _isChoosePartiesPopupOpenAlready = false;
    private static readonly bool _bannerKingsLoaded = AccessTools.TypeByName("BannerKings.Main") != null;

    internal static PartyAIClanPartySettingsManager PartySettingsManager;
    internal static PartyAITroopRecruiter PartyTroopRecruiter;
    internal static PartyAIThinker PartyThinker;
    internal static PartyAIDetachmentManager DetatchmentManager;
    internal static PAInformationManager InformationManager;
    private UIExtender _extender;

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

      //DetatchmentManager = new();
      //campaignGameStarter.AddBehavior(DetatchmentManager);

      //campaignGameStarter.AddBehavior(new PartyAIFoodBuyer());

      campaignGameStarter.AddModel(new PAITroopUpgradeModel(GetGameModel<PartyTroopUpgradeModel>(gameStarterObject)));
      campaignGameStarter.AddModel(new PAIArmyManagementCalculationModel(GetGameModel<ArmyManagementCalculationModel>(gameStarterObject)));
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

      if (!_harmonyRan)
      {
        _harmony.PatchAll();
        if (!_bannerKingsLoaded)
        {
          _harmony.Patch(AccessTools.Method(typeof(ArmyManagementVM), "ExecuteDone"), postfix: new(typeof(ArmyManagementVMPatches.ExecuteDone), "Postfix"));
          _harmony.Patch(AccessTools.Method(typeof(ArmyManagementVM), "RefreshValues"), postfix: new(typeof(ArmyManagementVMPatches.Constructor), "Postfix"));
        }
        _harmonyRan = true;
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
      var state = Game.Current?.GameStateManager?.ActiveState;

      if (state == null
                || state is not MapState
                || state.IsMenuState
                || state is MissionState
                || Mission.Current != null
         )
            {
        return;
      }

      if ((Input.IsKeyDown(PartySettingsManager.ControlPanelModiferKey) || PartySettingsManager.ControlPanelModiferKey == InputKey.Invalid) && Input.IsKeyDown(PartySettingsManager.ControlPanelKey))
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
      _harmony ??= new Harmony("carbon.partyaicontrols");

      if (!_UIExtenderRan)
      {
        _extender = new UIExtender("PartyAIControls");
        _extender.Register(typeof(SubModule).Assembly);
        _extender.Enable();
        _UIExtenderRan = true;
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
}
