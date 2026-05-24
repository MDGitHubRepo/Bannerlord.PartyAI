using PartyAIControls.CampaignBehaviors;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.InputSystem;
using TaleWorlds.SaveSystem;
using static PartyAIControls.PAICustomOrder;
using static PartyAIControls.PAIDetatchmentConfig;

namespace PartyAIControls
{
  internal class PAISaveableTypeDefiner : SaveableTypeDefiner
  {
    public PAISaveableTypeDefiner() : base(548730888) { }

    protected override void DefineClassTypes()
    {
      AddClassDefinition(typeof(PartyAIClanPartySettings), 1);
      AddClassDefinition(typeof(PAICustomTemplate), 2);
      AddClassDefinition(typeof(PartyCompositionObect), 3);
      AddClassDefinition(typeof(PAICustomOrder), 4);
      AddClassDefinition(typeof(PAISettlementVisitLog), 5);
      AddClassDefinition(typeof(PAIDetatchmentConfig), 6);
    }

    protected override void DefineEnumTypes()
    {
      AddEnumDefinition(typeof(OrderType), 1001);
      AddEnumDefinition(typeof(InputKey), 1002);
      AddEnumDefinition(typeof(DetatchmentType), 1003);
    }

    protected override void DefineContainerDefinitions()
    {
      ConstructContainerDefinition(typeof(Dictionary<Hero, PartyAIClanPartySettings>));
      ConstructContainerDefinition(typeof(Dictionary<Settlement, PartyAIClanPartySettings>));
      ConstructContainerDefinition(typeof(List<PAICustomTemplate>));
      ConstructContainerDefinition(typeof(List<CharacterObject>));
      ConstructContainerDefinition(typeof(List<Hero>));
      ConstructContainerDefinition(typeof(Dictionary<Settlement, CampaignTime>));
      ConstructContainerDefinition(typeof(List<PAISettlementVisitLog>));
      ConstructContainerDefinition(typeof(List<PAICustomOrder>));
      ConstructContainerDefinition(typeof(List<PAIDetatchmentConfig>));
    }
  }
}
