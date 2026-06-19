using System;
using TaleWorlds.Library;

namespace Bannerlord.PartyAI.ViewModels.Components;

public class CompositionSliderRowVM : ViewModel
{
    public event Action<CompositionSliderRowVM>? UserChangedValue;

    private bool _isProgrammatic;

    public CompositionSliderRowVM(int initialValue, string icon)
    {
        Value = initialValue;
        IsLocked = false;
        Icon = icon;
    }

    [DataSourceProperty]
    public string Icon { get; }

    [DataSourceProperty]
    public int Value
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                OnPropertyChangedWithValue(value, nameof(Value));
                OnPropertyChanged(nameof(Percentage));
                if (!_isProgrammatic)
                    UserChangedValue?.Invoke(this);
            }
        }
    }

    [DataSourceProperty]
    public bool IsLocked
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                OnPropertyChangedWithValue(value, nameof(IsLocked));
            }
        }
    }

    [DataSourceProperty]
    public string Percentage => $"{Value}%";

    public void SetValueSilently(int value)
    {
        _isProgrammatic = true;
        Value = value;
        _isProgrammatic = false;
    }
}
