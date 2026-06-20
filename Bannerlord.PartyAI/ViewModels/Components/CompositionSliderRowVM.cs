using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;

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
        IsLockToggleable = true;

        LockHint = IsLockToggleable
            ? new HintViewModel()
            : new HintViewModel(
                new TextObject("{=PAI_locked_formation}This formation class is not used by this party's template."));
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
                {
                    UserChangedValue?.Invoke(this);
                }
            }
        }
    }

    [DataSourceProperty]
    public bool IsLocked
    {
        get;
        set
        {
            IsSliderEnabled = !value;

            if (value != field)
            {
                field = value;
                OnPropertyChangedWithValue(value, nameof(IsLocked));
            }
        }
    }

    [DataSourceProperty]
    public bool IsSliderEnabled
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                OnPropertyChangedWithValue(value, nameof(IsSliderEnabled));
            }
        }
    }

    [DataSourceProperty]
    public bool IsLockToggleable
    {
        get;
        set
        {
            if (!value)
            {
                IsLocked = true;
            }

            if (value != field)
            {
                field = value;
                OnPropertyChangedWithValue(value, nameof(IsLockToggleable));
            }
        }
    }

    [DataSourceProperty]
    public string Percentage => $"{Value}%";

    [DataSourceProperty]
    public HintViewModel LockHint
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                OnPropertyChangedWithValue(value, nameof(LockHint));
            }
        }
    }

    public void SetValueSilently(int value)
    {
        _isProgrammatic = true;
        Value = value;
        _isProgrammatic = false;
    }
}
