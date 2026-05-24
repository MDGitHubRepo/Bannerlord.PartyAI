using System;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace PartyAIControls.ViewModels
{
  public class PartyAICompositionSlidersVM : ViewModel
  {
    private readonly Action<PartyCompositionObect> _onSavePartyComposition;
    private readonly PartyAIClanPartySettings _settings;
    private bool _doNotClamp;
    private int _infantry;
    private int _ranged;
    private int _cavalry;
    private int _horseArcher;
    private bool _isInfantryLocked;
    private bool _isRangedLocked;
    private bool _isCavalryLocked;
    private bool _isHorseArcherLocked;

    // keep the constructor safe for settings to be null
    public PartyAICompositionSlidersVM(PartyAIClanPartySettings settings, Action<PartyCompositionObect> callback)
    {
      SlidersTitleText = new TextObject("{=PAgaRahFHeV}Edit Party Composition").ToString();

      if (settings == null) { return; }
      _settings = settings;

      PartyCompositionObect composition = settings.Composition.Clone();
      composition.Scale(100);

      _doNotClamp = true;
      InfantryInt = (int)Math.Round(composition.Infantry);
      RangedInt = (int)Math.Round(composition.Ranged);
      CavalryInt = (int)Math.Round(composition.Cavalry);
      HorseArcherInt = (int)Math.Round(composition.HorseArcher);
      _doNotClamp = false;

      IsInfantryLocked = false; // to clear locks
      IsRangedLocked = false; // to clear locks
      IsCavalryLocked = false; // to clear locks
      IsHorseArcherLocked = false; // to clear locks
      _onSavePartyComposition = callback;

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
      get
      {
        return _isInfantryLocked;
      }
      set
      {
        if (value != _isInfantryLocked)
        {
          _isInfantryLocked = value;
          OnPropertyChangedWithValue(value, "IsInfantryLocked");
        }
      }
    }

    [DataSourceProperty]
    public bool IsRangedLocked
    {
      get
      {
        return _isRangedLocked;
      }
      set
      {
        if (value != _isRangedLocked)
        {
          _isRangedLocked = value;
          OnPropertyChangedWithValue(value, "IsRangedLocked");
        }
      }
    }

    [DataSourceProperty]
    public bool IsCavalryLocked
    {
      get
      {
        return _isCavalryLocked;
      }
      set
      {
        if (value != _isCavalryLocked)
        {
          _isCavalryLocked = value;
          OnPropertyChangedWithValue(value, "IsCavalryLocked");
        }
      }
    }

    [DataSourceProperty]
    public bool IsHorseArcherLocked
    {
      get
      {
        return _isHorseArcherLocked;
      }
      set
      {
        if (value != _isHorseArcherLocked)
        {
          _isHorseArcherLocked = value;
          OnPropertyChangedWithValue(value, "IsHorseArcherLocked");
        }
      }
    }

    [DataSourceProperty]
    public int InfantryInt
    {
      get
      {
        return _infantry;
      }
      set
      {
        if (value != _infantry)
        {
          _infantry = value;
          ClampTo100(FormationClass.Infantry);
        }

        OnPropertyChanged("InfantryInt");
        OnPropertyChanged("InfantryPercentage");
      }
    }

    [DataSourceProperty]
    public int RangedInt
    {
      get
      {
        return _ranged;
      }
      set
      {
        if (value != _ranged)
        {
          _ranged = value;
          ClampTo100(FormationClass.Ranged);
        }

        OnPropertyChanged("RangedInt");
        OnPropertyChanged("RangedPercentage");
      }
    }

    [DataSourceProperty]
    public int CavalryInt
    {
      get
      {
        return _cavalry;
      }
      set
      {
        if (value != _cavalry)
        {
          _cavalry = value;
          ClampTo100(FormationClass.Cavalry);
        }

        OnPropertyChanged("CavalryInt");
        OnPropertyChanged("CavalryPercentage");
      }
    }

    [DataSourceProperty]
    public int HorseArcherInt
    {
      get
      {
        return _horseArcher;
      }
      set
      {
        if (value != _horseArcher)
        {
          _horseArcher = value;
          ClampTo100(FormationClass.HorseArcher);
        }

        OnPropertyChanged("HorseArcherInt");
        OnPropertyChanged("HorseArcherPercentage");
      }
    }

    private int Total => InfantryInt + RangedInt + CavalryInt + HorseArcherInt;

    public void AcceptEditPartyComposition()
    {
      PartyCompositionObect composition = new();
      composition.Infantry = _infantry;
      composition.Ranged = _ranged;
      composition.Cavalry = _cavalry;
      composition.HorseArcher = _horseArcher;
      composition.Scale(0.01f);

      _onSavePartyComposition.Invoke(composition);
    }

    public void CancelEditPartyComposition()
    {
      _onSavePartyComposition.Invoke(_settings.Composition.Clone());
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
      while (Total != 100)
      {
        bool actionTaken = false;
        foreach (FormationClass type in new FormationClass[] { FormationClass.Infantry, FormationClass.Ranged, FormationClass.Cavalry, FormationClass.HorseArcher })
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

          if (Total == 100)
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

    public int this[FormationClass i]
    {
      get
      {
        switch (i)
        {
          case FormationClass.Infantry: return InfantryInt;
          case FormationClass.Ranged: return RangedInt;
          case FormationClass.Cavalry: return CavalryInt;
          case FormationClass.HorseArcher: return HorseArcherInt;
          default: return 0;
        }
      }
      set
      {
        switch (i)
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
      switch (type)
      {
        case FormationClass.Infantry:
          return IsInfantryLocked;
        case FormationClass.Ranged:
          return IsRangedLocked;
        case FormationClass.Cavalry:
          return IsCavalryLocked;
        case FormationClass.HorseArcher:
          return IsHorseArcherLocked;
        default:
          return false;
      }
    }
  }
}
