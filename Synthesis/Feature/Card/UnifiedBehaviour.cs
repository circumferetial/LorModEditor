using System.Xml.Linq;
using Synthesis.Core;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Enums;

namespace Synthesis.Feature.Card;

public class UnifiedBehaviour : XWrapper
{
    public UnifiedBehaviour(XElement element) : base(element)
    {
        InitDefaults();
    }

    // 最小值
    public int Min
    {
        get => GetIntAttr(Element, "Min", 1);
        set => SetIntAttr(Element, "Min", value);
    }

    public int Dice
    {
        get => GetIntAttr(Element, "Dice", 1);
        set => SetIntAttr(Element, "Dice", value);
    }

    // 类型 (Atk=进攻, Def=防御, Standby=反击)
    public DiceType Type
    {
        get => GetEnumAttr(Element, "Type", DiceType.Atk);
        set => SetEnumAttr(Element, "Type", value);
    }

    // 细节 (Slash=斩击, Penetrate=穿刺, Hit=打击, Guard=招架, Evasion=闪避)
    public DiceDetail Detail
    {
        get => GetEnumAttr(Element, "Detail", DiceDetail.Slash);
        set
        {
            // 1. 设置 Detail 自己的值
            SetEnumAttr(Element, "Detail", value);
            if (Motion == DiceMotion.F && value != DiceDetail.Guard && value != DiceDetail.Evasion ||
                GlobalValues.Special.Contains(Motion))
            {
                return;
            }

            if (GlobalValues.VariantMotions.ContainsValue(Motion))
            {
                if (GlobalValues.VariantMotions.TryGetValue(value, out var motion))
                {
                    Motion = motion;
                }
                return;
            }
            // 2. 【核心逻辑】自动联动 Motion
            // 如果字典里有这个 Detail 对应的默认动作，就自动填进去
            if (GlobalValues.DetailToMotion.TryGetValue(value, out var autoMotion))
            {
                // 设置 Motion (这会触发 Motion 的 OnPropertyChanged，界面会自动更新)
                Motion = autoMotion;
            }
        }
    }


    // 动作 (J=斩击动作, Z=穿刺动作, H=打击动作, G=防御动作, E=闪避动作)
    // 这个通常不需要用户手填，可以以后做一个自动映射逻辑
    public DiceMotion Motion
    {
        get => GetEnumAttr(Element, "Motion", DiceMotion.J);
        set => SetEnumAttr(Element, "Motion", value);
    }


    // 【新增】EffectRes (特效资源)
    // 比如 "FX_Slash_Red"
    public string EffectRes
    {
        get => GetAttr(Element, "EffectRes");
        set => SetAttr(Element, "EffectRes", value);
    }

    // 【新增】Script (骰子脚本)
    // 比如 "Paralysis2atk" (命中施加2层麻痹)
    public string Script
    {
        get => GetAttr(Element, "Script");
        set => SetAttr(Element, "Script", value);
    }

    // 【新增】Desc (自定义描述/备注)
    // 虽然原版不用，但为了兼容你的需求
    public string Desc
    {
        get => GetAttr(Element, "Desc");
        set => SetAttr(Element, "Desc", value);
    }

    public string ActionScript
    {
        get => GetAttr(Element, "ActionScript");
        set => SetAttr(Element, "ActionScript", value);
    }
}