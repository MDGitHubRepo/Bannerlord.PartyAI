using Bannerlord.PartyAI.Domain.Models;
using Bannerlord.PartyAI.Models;
using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Bannerlord.PartyAI.ViewModels;

public class PartyAICompositionSlidersVM : ViewModel
{
    private static readonly FormationClass[] FormationClasses =
    [
        FormationClass.Infantry,
        FormationClass.Ranged,
        FormationClass.Cavalry,
        FormationClass.HorseArcher
    ];

    private readonly Action<PartyComposition> _onSavePartyComposition;
    private readonly PartyAiEntitySettings _settings;

    private bool _doNotClamp;

    public PartyAICompositionSlidersVM(PartyAiEntitySettings settings, Action<PartyComposition> callback)
    {
        SlidersTitleText = new TextObject("{=PAgaRahFHeV}Edit Party Composition").ToString();

        _settings = settings;
        _onSavePartyComposition = callback;

        IsInfantryLocked = false; // to clear locks
        IsRangedLocked = false; // to clear locks
        IsCavalryLocked = false; // to clear locks
        IsHorseArcherLocked = false; // to clear locks

        PartyComposition composition = new PartyComposition(settings.Composition);
        composition.Scale(100);

        _doNotClamp = true;
        InfantryInt = (int)Math.Round(composition.Infantry);
        RangedInt = (int)Math.Round(composition.Ranged);
        CavalryInt = (int)Math.Round(composition.Cavalry);
        HorseArcherInt = (int)Math.Round(composition.HorseArcher);
        _doNotClamp = false;

        RefreshValues();
    }

    [DataSourceProperty]
    public string AcceptText => new TextObject("{=bV75iwKa}Save").ToString();

    [DataSourceProperty]
    public string CancelText => GameTexts.FindText("str_cancel").ToString();

    [DataSourceProperty]
    public string SlidersTitleText { get; set; }

    [DataSourceProperty]
    public string InfantryPercentage => InfantryInt.ToString() + "%";

    [DataSourceProperty]
    public string RangedPercentage => RangedInt.ToString() + "%";

    [DataSourceProperty]
    public string CavalryPercentage => CavalryInt.ToString() + "%";

    [DataSourceProperty]
    public string HorseArcherPercentage => HorseArcherInt.ToString() + "%";

    [DataSourceProperty]
    public bool IsInfantryLocked
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                OnPropertyChangedWithValue(value, "IsInfantryLocked");
            }
        }
    }

    [DataSourceProperty]
    public bool IsRangedLocked
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                OnPropertyChangedWithValue(value, "IsRangedLocked");
            }
        }
    }

    [DataSourceProperty]
    public bool IsCavalryLocked
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                OnPropertyChangedWithValue(value, "IsCavalryLocked");
            }
        }
    }

    [DataSourceProperty]
    public bool IsHorseArcherLocked
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                OnPropertyChangedWithValue(value, "IsHorseArcherLocked");
            }
        }
    }

    [DataSourceProperty]
    public int InfantryInt
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                ClampTo100(FormationClass.Infantry);
            }

            OnPropertyChanged(nameof(InfantryInt));
            OnPropertyChanged(nameof(InfantryPercentage));
        }
    }

    [DataSourceProperty]
    public int RangedInt
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                ClampTo100(FormationClass.Ranged);
            }

            OnPropertyChanged(nameof(RangedInt));
            OnPropertyChanged(nameof(RangedPercentage));
        }
    }

    [DataSourceProperty]
    public int CavalryInt
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                ClampTo100(FormationClass.Cavalry);
            }

            OnPropertyChanged(nameof(CavalryInt));
            OnPropertyChanged(nameof(CavalryPercentage));
        }
    }

    [DataSourceProperty]
    public int HorseArcherInt
    {
        get;
        set
        {
            if (value != field)
            {
                field = value;
                ClampTo100(FormationClass.HorseArcher);
            }

            OnPropertyChanged(nameof(HorseArcherInt));
            OnPropertyChanged(nameof(HorseArcherPercentage));
        }
    }

    private int Total => InfantryInt + RangedInt + CavalryInt + HorseArcherInt;

    public void AcceptEditPartyComposition()
    {
        PartyComposition composition = new()
        {
            Infantry = InfantryInt,
            Ranged = RangedInt,
            Cavalry = CavalryInt,
            HorseArcher = HorseArcherInt
        };
        composition.Scale(0.01f);

        _onSavePartyComposition.Invoke(composition);
    }

    public void CancelEditPartyComposition()
    {
        _onSavePartyComposition.Invoke(new PartyComposition(_settings.Composition));
    }

    private void ClampTo100(FormationClass changedType)
    {
        if (_doNotClamp)
        {
            return;
        }

        if (Total == 100)
        {
            return;
        }

        _doNotClamp = true;

        bool mayChangeMain = false;
        while (Total > 100)
        {
            bool actionTaken = false;
            foreach (FormationClass type in FormationClasses)
            {
                int sign = Total > 100 ? -1 : 1;

                if (type == changedType && !mayChangeMain)
                {
                    continue;
                }

                if ((sign > 0 && this[type] >= 100) || (sign < 0 && this[type] <= 0))
                {
                    continue;
                }

                if (!GetLocked(type))
                {
                    this[type] += sign;
                    actionTaken = true;
                }

                if (Total <= 100)
                {
                    break;
                }
            }

            if (!actionTaken)
            {
                mayChangeMain = true;
            }
        }

        _doNotClamp = false;
        return;
    }

    public int this[FormationClass formationClass]
    {
        get
        {
            return formationClass switch
            {
                FormationClass.Infantry => InfantryInt,
                FormationClass.Ranged => RangedInt,
                FormationClass.Cavalry => CavalryInt,
                FormationClass.HorseArcher => HorseArcherInt,
                _ => 0,
            };
        }
        set
        {
            switch (formationClass)
            {
                case FormationClass.Infantry: InfantryInt = value; break;
                case FormationClass.Ranged: RangedInt = value; break;
                case FormationClass.Cavalry: CavalryInt = value; break;
                case FormationClass.HorseArcher: HorseArcherInt = value; break;
                default: break;
            }
        }
    }

    private bool GetLocked(FormationClass type)
    {
        return type switch
        {
            FormationClass.Infantry => IsInfantryLocked,
            FormationClass.Ranged => IsRangedLocked,
            FormationClass.Cavalry => IsCavalryLocked,
            FormationClass.HorseArcher => IsHorseArcherLocked,
            _ => false,
        };
    }
}
