using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;

namespace Bannerlord.PartyAI.Domain.Models;

public class PartyComposition
{
    private static readonly FormationClass[] FormationTypes = 
    [
        FormationClass.Infantry,
        FormationClass.Ranged,
        FormationClass.Cavalry,
        FormationClass.HorseArcher
    ];

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

    public void ApplyTemplate(PAICustomTemplate? template, out FormationClass[] formationTypes)
    {
        if (template is not null)
        {
            formationTypes = template.UpgradeTargets
                .GetTroopRoster()
                .Select(element => element.Character.DefaultFormationClass.FallbackClass())
                    .Distinct()
                    .ToArray();

            ClearUnusedFormations(formationTypes);
        }
        else
        {
            formationTypes = FormationTypes;
        }
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

    public float GetTotal()
    {
        return Infantry + Ranged + Cavalry + HorseArcher;
    }

    private void ClearUnusedFormations(FormationClass[]? templateTroopClasses)
    {
        if (templateTroopClasses is null || templateTroopClasses.Length == 0)
        {
            return;
        }

        foreach (FormationClass formation in FormationTypes)
        {
            if (!templateTroopClasses.Contains(formation))
            {
                this[formation] = 0;
            }
        }

        float total = GetTotal();
        if (total == 0)
        {
            return;
        }

        float scalar = 1f / total;
        Scale(scalar);
    }
}
