using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyAIControls.ViewModels.Components
{
  public class PartyAIOrderItemVM : ViewModel
  {
    private readonly PAICustomOrder _order;
    private readonly PartyAIClanPartySettings _settings;
    private readonly Action _refreshCallback;

    public PartyAIOrderItemVM(PAICustomOrder order, PartyAIClanPartySettings settings, Action refreshCallback)
    {
      _order = order;
      OrderType = order.QueueText.ToString();
      _settings = settings;
      _refreshCallback = refreshCallback;
    }

    [DataSourceProperty] public string OrderType { get; set; }
    [DataSourceProperty] public bool IsEnabled => _order != null && _order.Behavior != PAICustomOrder.OrderType.None;
    [DataSourceProperty] public bool CanMoveUp => IsEnabled && _settings.Order != _order;
    [DataSourceProperty] public bool CanMoveDown => IsEnabled && (_settings.OrderQueue.Count - 1 > _settings.OrderQueue.IndexOf(_order) || (_settings.Order == _order && _settings.OrderQueue.Count > 0));

    public void DeleteOrder()
    {
      string title = new TextObject("{=PAIv8ekJ4gs}Are you sure?").ToString();
      InformationManager.ShowInquiry(new(title, string.Empty, true, true, GameTexts.FindText("str_yes").ToString(), GameTexts.FindText("str_cancel").ToString(), () =>
      {
        if (_settings.Order == _order)
        {
          _settings.ClearOrder();
        }
        else
        {
          _settings.OrderQueue.Remove(_order);
        }
        _refreshCallback?.Invoke();
      }, null));
    }

    public void MoveUp()
    {
      if (_settings.Order == _order)
      {
        return;
      }
      int index = _settings.OrderQueue.IndexOf(_order);
      if (index == 0)
      {
        _settings.OrderQueue.RemoveAt(index);
        _settings.SetOrder(_order);
      }
      else
      {
        _settings.OrderQueue.RemoveAt(index);
        _settings.OrderQueue.Insert(index - 1, _order);
      }
      _refreshCallback?.Invoke();
    }

    public void MoveDown()
    {
      if (_settings.Order == _order)
      {
        _settings.ClearOrder();
        _settings.OrderQueue.Insert(0, _order);
      }
      else
      {
        int index = _settings.OrderQueue.IndexOf(_order);

        if (index == _settings.OrderQueue.Count - 1)
        {
          return;
        }

        _settings.OrderQueue.RemoveAt(index);
        _settings.OrderQueue.Insert(index + 1, _order);
      }
      _refreshCallback?.Invoke();
    }
  }
}