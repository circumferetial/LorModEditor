using System.Collections.ObjectModel;
using System.Xml.Linq;
using JetBrains.Annotations;
using LorModEditor.Core.Attributes;
using LorModEditor.Core.Enums;
using LorModEditor.Core.Services;

// 引用 LorId, SafeCast, Enums

// 引用 EtcRepository

namespace LorModEditor.Core.Wrappers;

// --- 子包装器：掉落项 ---
public class UnifiedDropItem : XWrapper
{
    public UnifiedDropItem(XElement element) : base(element)
    {
        InitDefaults();
    }

    // 1. 强类型枚举 (Equip / Card)
    [UsedImplicitly]
    public DropItemType Type
    {
        get => GetEnumAttr(Element, "Type", DropItemType.Equip);
        set => SetEnumAttr(Element, "Type", value);
    }

    // 2. 强类型 LorId (处理引用)
    // 掉落物可能是原版的，也可能是 Mod 的，所以必须处理 Pid
    public LorId ItemId
    {
        get => LorId.ParseXmlReference(Element, GlobalId.PackageId);

        set
        {
            if (IsVanilla) return;

            // 写入值
            Element.Value = value.ItemId;

            // 处理 Pid
            if (value.PackageId != GlobalId.PackageId && !string.IsNullOrEmpty(value.PackageId))
            {
                Element.SetAttributeValue("Pid", value.PackageId);
            }
            else
            {
                Element.Attribute("Pid")?.Remove();
            }

            OnPropertyChanged();
        }
    }
}

// --- 主包装器：书籍 ---
public class UnifiedDropBook : XWrapper
{
    private readonly XElement _data;


    // 依赖注入翻译仓库
    private readonly EtcRepository? _etcRepo;

    public UnifiedDropBook(XElement data, EtcRepository? etcRepo) : base(data)
    {
        _data = data;
        _etcRepo = etcRepo;

        LoadDrops();
        InitDefaults();
    }

    [NoAutoInit] public string DisplayName => $"{GlobalId} {LocalizedName}";

    public string Id
    {
        get => GetAttr(_data, "ID");
        set
        {
            SetAttr(_data, "ID", value);
            // DropBook 的 TextId 通常默认等于 ID，改 ID 时顺便改一下 TextId 比较方便
            if (!IsVanilla) SetElementValue(_data, "TextId", value);

            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public string TextId
    {
        get => GetElementValue(_data, "TextId", Id);
        set
        {
            SetElementValue(_data, "TextId", value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(LocalizedName));// TextId 变了，查到的翻译也会变
        }
    }

    // 读写 EtcRepository
    [NoAutoInit]
    public string LocalizedName
    {
        get
        {
            // 尝试获取翻译
            var text = _etcRepo?.GetText(TextId);

            // 如果找到了，返回翻译；没找到，返回 "未翻译" 或 TextId
            return text ?? "未翻译";
        }
        set
        {
            if (IsVanilla || _etcRepo == null) return;
            // 防止把默认提示语写入 XML
            if (value == "未翻译") return;

            _etcRepo.SetText(TextId, value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public string BookIcon
    {
        get => GetElementValue(_data, "BookIcon", "FullStopOffice");
        set => SetElementValue(_data, "BookIcon", value);
    }

    // 强类型 Chapter (1-7)
    public int Chapter
    {
        get => GetInt(_data, "Chapter", 1);
        set => SetInt(_data, "Chapter", Math.Clamp(value, 1, 7));
    }

    // 可选：添加 Usage 属性 (Invitation/Reward) 用于分类
    public string Usage
    {
        get => GetAttr(_data, "Usage", "Reward");
        set => SetAttr(_data, "Usage", value);
    }

    // --- 集合操作 ---
    public ObservableCollection<UnifiedDropItem> DropItems { get; } = new();

// Wrappers/UnifiedDropBook.cs

    private void LoadDrops()
    {
        DropItems.Clear();
        foreach (var d in Element.Elements("DropItem"))
        {
            // 【可选优化】只加载 Type="Equip" 的项
            // 这样可以彻底屏蔽掉那些非标准的写法，保证 UI 纯净
            var type = d.Attribute("Type")?.Value;
            if (type == "Equip")
            {
                DropItems.Add(new UnifiedDropItem(d));
            }
        }
    }

    public void AddDropItem()
    {
        if (IsVanilla) return;

        // 默认添加一个装备掉落，且没有 Pid (当前 Mod)
        var d = new XElement("DropItem", new XAttribute("Type", "Equip"), "");

        _data.Add(d);
        DropItems.Add(new UnifiedDropItem(d));
    }

    public void RemoveDropItem(UnifiedDropItem i)
    {
        if (IsVanilla) return;
        i.Element.Remove();
        DropItems.Remove(i);
    }

    public void DeleteXml()
    {
        if (IsVanilla) return;
        _data.Remove();
        // EtcRepo 里的翻译通常不删，因为可能被复用，或者很难定位
    }
}