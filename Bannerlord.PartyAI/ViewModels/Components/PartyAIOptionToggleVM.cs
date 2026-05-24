using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyAIControls.ViewModels.Components
{
  public class PartyAIOptionToggleVM : ViewModel
  {
    private string _text;
    private bool _isSelected;
    private HintViewModel _hint;
    private bool _isDisabled;
    private readonly Action<bool> _onChange;

    public PartyAIOptionToggleVM(TextObject text, bool isSelected, TextObject hint, Action<bool> onChange = null)
    {
      Text = text.ToString();
      IsSelected = isSelected;
      Hint = new HintViewModel(hint ?? new TextObject(""));
      _onChange = onChange;
    }

    [DataSourceProperty]
    public string Text
    {
      get
      {
        return _text;
      }
      set
      {
        if (value != _text)
        {
          _text = value;
          OnPropertyChangedWithValue(value, "Text");
        }
      }
    }

    [DataSourceProperty]
    public bool IsSelected
    {
      get
      {
        return _isSelected;
      }
      set
      {
        if (value != _isSelected)
        {
          _onChange?.Invoke(value);
          _isSelected = value;
          OnPropertyChangedWithValue(value, "IsSelected");
        }
      }
    }

    [DataSourceProperty]
    public bool IsDisabled
    {
      get
      {
        return _isDisabled;
      }
      set
      {
        if (value != _isDisabled)
        {
          _isDisabled = value;
          OnPropertyChangedWithValue(value, "IsDisabled");
        }
      }
    }

    [DataSourceProperty]
    public HintViewModel Hint
    {
      get
      {
        return _hint;
      }
      set
      {
        if (value != _hint)
        {
          _hint = value;
          OnPropertyChangedWithValue(value, "Hint");
        }
      }
    }
  }
}
