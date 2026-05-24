using PartyAIControls.ViewModels.Components;
using PartyAIControls.ViewModels.Dialogs;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyAIControls.ViewModels
{
  public class PartyAIOrderQueueVM : ViewModel
  {
    private readonly PartyAIClanPartySettings _settings;
    private MBBindingList<PartyAIOrderItemVM> _orderList;
    private readonly Action _callback;

    public PartyAIOrderQueueVM(PartyAIClanPartySettings settings, Action callback)
    {
      if (settings == null) { return; }
      _settings = settings;
      _callback = callback;
      TitleText = new TextObject("{=PAI4eHNvDEM}Order Queue for {HERO}'s party").SetTextVariable("HERO", _settings.Hero.Name.ToString()).ToString();
      OrderList = new MBBindingList<PartyAIOrderItemVM>();

      RefreshOrderQueue();
      RefreshValues();
    }

    [DataSourceProperty]
    public MBBindingList<PartyAIOrderItemVM> OrderList
    {
      get
      {
        return _orderList;
      }
      set
      {
        if (value != _orderList)
        {
          _orderList = value;
          OnPropertyChangedWithValue(value, "OrderList");
        }
      }
    }

    [DataSourceProperty] public string AcceptText => GameTexts.FindText("str_done").ToString();

    [DataSourceProperty] public string TitleText { get; private set; }
    [DataSourceProperty] public string AddOrderText => new TextObject("{=PAI9PHY91SP}Add Order").ToString();
    [DataSourceProperty] public string ClearQueueText => new TextObject("{=PAIl7GEAaaD}Clear Queue").ToString();

    private void RefreshOrderQueue()
    {
      OrderList.Clear();
      if (_settings.HasActiveOrder)
      {
        OrderList.Add(new(_settings.Order, _settings, RefreshOrderQueue));
        foreach (PAICustomOrder order in _settings.OrderQueue)
        {
          OrderList.Add(new(order, _settings, RefreshOrderQueue));
        }
      }
      else
      {
        OrderList.Add(new(new(null, PAICustomOrder.OrderType.None), _settings, RefreshOrderQueue));
      }
      OnPropertyChanged("OrderList");
    }

    public void AddOrder() => CreateOrder.Create(_settings, RefreshOrderQueue);

    public void ClearQueue()
    {
      string title = new TextObject("{=PAIv8ekJ4gs}Are you sure?").ToString();
      InformationManager.ShowInquiry(new(title, string.Empty, true, true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_cancel").ToString(), () =>
      {
        _settings.OrderQueue.Clear();
        _settings.ClearOrder();
        RefreshOrderQueue();
      }, null));
    }

    public void DoneOrderQueue()
    {
      _callback.Invoke();
    }
  }
}
