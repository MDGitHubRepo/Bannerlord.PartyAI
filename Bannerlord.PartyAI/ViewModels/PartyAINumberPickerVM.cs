using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyAIControls.ViewModels
{
  public class PartyAINumberPickerVM : ViewModel
  {
    private readonly Action<int> _onCloseNumberPicker;
    private int _value;
    private readonly int _originalValue;
    private readonly bool _isPercentage;

    public PartyAINumberPickerVM(int value, int min, int max, string text, string description, Action<int> callback, bool isPercentage)
    {
      SlidersTitleText = text;
      Description = description;
      MinValue = min;
      MaxValue = max;
      _value = value;
      _originalValue = value;
      _isPercentage = isPercentage;
      _onCloseNumberPicker = callback;

      RefreshValues();
    }

    [DataSourceProperty]
    public string AcceptText => new TextObject("{=bV75iwKa}Save").ToString();

    [DataSourceProperty]
    public string CancelText => GameTexts.FindText("str_cancel").ToString();

    [DataSourceProperty]
    public string SlidersTitleText { get; set; }

    [DataSourceProperty]
    public string Description { get; set; }

    [DataSourceProperty]
    public string Percentage => Value.ToString() + (_isPercentage ? "%" : "");

    [DataSourceProperty]
    public int MaxValue { get; set; }

    [DataSourceProperty]
    public int MinValue { get; set; }


    [DataSourceProperty]
    public int Value
    {
      get
      {
        return _value;
      }
      set
      {
        if (value != _value)
        {
          _value = value;
        }

        OnPropertyChanged("Value");
        OnPropertyChanged("Percentage");
      }
    }

    public void ManuallyPickValue()
    {
      InformationManager.ShowTextInquiry(new TextInquiryData(new TextObject("{=PAI4nxGNBim}Edit").ToString(), string.Empty, isAffirmativeOptionShown: true, isNegativeOptionShown: true, GameTexts.FindText("str_done").ToString(), GameTexts.FindText("str_cancel").ToString(),
      delegate (string text)
      {
        if (Int32.TryParse(text, out int amount))
        {
          Value = amount;
        }
      }, null, shouldInputBeObfuscated: false,
      (string text) =>
      {
        if (Int32.TryParse(text, out int amount))
        {
          if (amount < MinValue)
          {
            return new(false, new TextObject("{=PAIkYqZ80Dt}Value must be greater than {MINVALUE}").SetTextVariable("MINVALUE", MinValue - 1).ToString());
          }
          if (amount > MaxValue)
          {
            return new(false, new TextObject("{=PAIZX2CAEyH}Value must be less than {MAXVALUE}").SetTextVariable("MAXVALUE", MaxValue + 1).ToString());
          }
          return new(true, string.Empty);
        }
        else
        {
          return new(false, new TextObject("{=PAI5AWANWod}You must enter a number").ToString());
        }
      }, null, Value.ToString()));
    }

    public void Accept() => _onCloseNumberPicker?.Invoke(_value);

    public void Cancel() => _onCloseNumberPicker?.Invoke(_originalValue);
  }
}
