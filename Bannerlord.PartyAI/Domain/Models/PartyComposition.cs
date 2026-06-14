using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace Bannerlord.PartyAI.Domain.Models;

public class PartyComposition
{
    [SaveableProperty(1)] public float Infantry { get; set; }
    [SaveableProperty(2)] public float Ranged { get; set; }
    [SaveableProperty(3)] public float Cavalry { get; set; }
    [SaveableProperty(4)] public float HorseArcher { get; set; }

    public PartyComposition(float infantry, float ranged, float cavalry, float horseArcher)
    {
        Infantry = infantry;
        Ranged = ranged;
        Cavalry = cavalry;
        HorseArcher = horseArcher;
    }

    public PartyComposition() : this(0, 0, 0, 0)
    {
    }

    public PartyComposition(PartyComposition original)
        : this(original.Infantry, original.Ranged, original.Cavalry, original.HorseArcher)
    {
    }

    public void Scale(float scalar)
    {
        Infantry *= scalar;
        Ranged *= scalar;
        Cavalry *= scalar;
        HorseArcher *= scalar;
    }

    public float this[FormationClass i]
    {
        get
        {
            return i switch
            {
                FormationClass.Infantry => Infantry,
                FormationClass.Ranged => Ranged,
                FormationClass.Cavalry => Cavalry,
                FormationClass.HorseArcher => HorseArcher,
                _ => 0,
            };
        }
        set
        {
            switch (i)
            {
                case FormationClass.Infantry: Infantry = value; break;
                case FormationClass.Ranged: Ranged = value; break;
                case FormationClass.Cavalry: Cavalry = value; break;
                case FormationClass.HorseArcher: HorseArcher = value; break;
                default: break;
            }
        }
    }
}
