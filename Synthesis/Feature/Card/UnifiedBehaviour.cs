using System.Xml.Linq;
using Synthesis.Core;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Enums;

namespace Synthesis.Feature.Card;

public class UnifiedBehaviour : XWrapper
{
    public UnifiedBehaviour(XElement element)
        : base(element)
    {
        InitDefaults();
    }

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

    public DiceType Type
    {
        get => GetEnumAttr(Element, "Type", DiceType.Atk);
        set => SetEnumAttr(Element, "Type", value);
    }

    public DiceDetail Detail
    {
        get => GetEnumAttr(Element, "Detail", DiceDetail.Slash);
        set
        {
            SetEnumAttr(Element, "Detail", value);
            if (Motion == DiceMotion.F && value != DiceDetail.Guard && value != DiceDetail.Evasion ||
                GlobalValues.Special.Contains(Motion))
            {
                return;
            }
            DiceMotion value3;
            if (GlobalValues.VariantMotions.ContainsValue(Motion))
            {
                if (GlobalValues.VariantMotions.TryGetValue(value, out var value2))
                {
                    Motion = value2;
                }
            }
            else if (GlobalValues.DetailToMotion.TryGetValue(value, out value3))
            {
                Motion = value3;
            }
        }
    }

    public DiceMotion Motion
    {
        get => GetEnumAttr(Element, "Motion", DiceMotion.J);
        set => SetEnumAttr(Element, "Motion", value);
    }

    public string EffectRes
    {
        get => GetAttr(Element, "EffectRes");
        set => SetAttr(Element, "EffectRes", value);
    }

    public string Script
    {
        get => GetAttr(Element, "Script");
        set => SetAttr(Element, "Script", value);
    }

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
