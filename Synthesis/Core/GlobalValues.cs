using System.Runtime.InteropServices;
using Synthesis.Core.Enums;

namespace Synthesis.Core;

public static class GlobalValues
{
    static GlobalValues()
    {
        const int num = 5;
        var list = new List<string>(num);
        CollectionsMarshal.SetCount(list, num);
        var span = CollectionsMarshal.AsSpan(list);
        span[0] = "START_BATTLE";
        span[1] = "BATTLE_VICTORY";
        span[2] = "DEATH";
        span[3] = "KILLS_OPPONENT";
        span[4] = "COLLEAGUE_DEATH";
        DialogTypes = list;
        VariantMotions = new Dictionary<DiceDetail, DiceMotion>
        {
            {
                DiceDetail.Hit,
                DiceMotion.H2
            },
            {
                DiceDetail.Slash,
                DiceMotion.J2
            },
            {
                DiceDetail.Penetrate,
                DiceMotion.Z2
            }
        };
        Resistances = Enum.GetValues<AtkResist>();
        Affections = Enum.GetValues<CardAffection>();
        RangeTypes = Enum.GetValues<RangeType>();
        InvitationCombines = Enum.GetValues<InvitationCombine>();
        SkinTypes = ["Lor", "Custom"];
        CardOptions = Enum.GetValues<CardOption>();
        BookChapters = [1, 2, 3, 4, 5, 6, 7];
        SkinActions = Enum.GetValues<ActionDetail>();
    }

    public static Rarity[] Rarities { get; } = Enum.GetValues<Rarity>();

    public static CardRange[] Ranges { get; } = Enum.GetValues<CardRange>();

    public static DiceType[] DiceTypes { get; } = Enum.GetValues<DiceType>();

    public static Dictionary<DiceDetail, DiceMotion> DetailToMotion { get; } = new()
    {
        {
            DiceDetail.Hit,
            DiceMotion.H
        },
        {
            DiceDetail.Slash,
            DiceMotion.J
        },
        {
            DiceDetail.Penetrate,
            DiceMotion.Z
        },
        {
            DiceDetail.Guard,
            DiceMotion.G
        },
        {
            DiceDetail.Evasion,
            DiceMotion.E
        }
    };

    public static DiceDetail[] DiceDetails { get; } = Enum.GetValues<DiceDetail>();

    public static DiceMotion[] DiceMotions { get; } = Enum.GetValues<DiceMotion>();

    public static DiceMotion[] Special
    {
        get
        {
            if (field != null)
            {
                return field;
            }
            var list = Enum.GetValues<DiceMotion>().ToList();
            list.RemoveAll(x => DetailToMotion.ContainsValue(x) || VariantMotions.ContainsValue(x));
            field = list.ToArray();
            return field;
        }
    }

    public static List<string> DialogTypes { get; }

    public static Dictionary<DiceDetail, DiceMotion> VariantMotions { get; }

    public static AtkResist[] Resistances { get; }

    public static CardAffection[] Affections { get; }

    public static RangeType[] RangeTypes { get; }

    public static InvitationCombine[] InvitationCombines { get; }

    public static string[] SkinTypes { get; }

    public static CardOption[] CardOptions { get; }

    public static int[] BookChapters { get; }

    public static ActionDetail[] SkinActions { get; }
}
