using Synthesis.Core.Enums;

namespace Synthesis.Core;

// 这是一个静态类，专门放游戏里的常量列表
// ReSharper disable CollectionNeverQueried.Global
public static class GlobalValues
{
    // 稀有度
    public static Rarity[] Rarities { get; } = Enum.GetValues<Rarity>();

    // 攻击距离类型
    public static CardRange[] Ranges { get; } = Enum.GetValues<CardRange>();

    // 骰子类型 (Type)
    public static DiceType[] DiceTypes { get; } = Enum.GetValues<DiceType>();

    // 骰子细节 (Detail)
    public static Dictionary<DiceDetail, DiceMotion> DetailToMotion { get; } = new()
    {
        { DiceDetail.Hit, DiceMotion.H },
        { DiceDetail.Slash, DiceMotion.J },
        { DiceDetail.Penetrate, DiceMotion.Z },
        { DiceDetail.Guard, DiceMotion.G },
        { DiceDetail.Evasion, DiceMotion.E }// XML 里通常写 Evasion，对应代码里的 Evade
    };

    // 更新 DiceDetails 列表，确保下拉框里的选项都在我们的字典里
    public static DiceDetail[] DiceDetails { get; } = Enum.GetValues<DiceDetail>();

    // 更新 DiceMotions 列表，加入 S1-S15 等
    public static DiceMotion[] DiceMotions { get; } = Enum.GetValues<DiceMotion>();

    public static DiceMotion[] Special
    {
        get
        {
            if (field != null) return field;
            var all = Enum.GetValues<DiceMotion>().ToList();
            all.RemoveAll(x => DetailToMotion.ContainsValue(x) || VariantMotions.ContainsValue(x));
            field = all.ToArray();
            return field;
        }
    }

    public static List<string> DialogTypes { get; } =
    [
        "START_BATTLE",// 战斗开始
        "BATTLE_VICTORY",// 胜利
        "DEATH",// 死亡
        "KILLS_OPPONENT",// 击杀
        "COLLEAGUE_DEATH"// 队友阵亡
    ];

    public static Dictionary<DiceDetail, DiceMotion> VariantMotions { get; } = new()
    {
        { DiceDetail.Hit, DiceMotion.H2 },
        { DiceDetail.Slash, DiceMotion.J2 },
        { DiceDetail.Penetrate, DiceMotion.Z2 }
    };

    public static AtkResist[] Resistances { get; } = Enum.GetValues<AtkResist>();

    // 书页攻击距离类型 (近战混用 / 远程限定)
    public static RangeType[] RangeTypes { get; } = Enum.GetValues<RangeType>();

    public static InvitationCombine[] InvitationCombines { get; } = Enum.GetValues<InvitationCombine>();

    public static string[] SkinTypes { get; } = ["Lor", "Custom"];

    public static CardOption[] CardOptions { get; } = Enum.GetValues<CardOption>();
    // 在 GlobalValues 类中添加：

    // 书籍等级 (1=Canard ... 7=Impuritas)
    public static int[] BookChapters { get; } = [1, 2, 3, 4, 5, 6, 7];
    public static ActionDetail[] SkinActions { get; } = Enum.GetValues<ActionDetail>();
}