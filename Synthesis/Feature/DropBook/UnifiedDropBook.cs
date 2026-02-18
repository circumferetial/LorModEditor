using System.Collections.ObjectModel;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Attributes;

namespace Synthesis.Feature.DropBook;

public class UnifiedDropBook : XWrapper
{
    private readonly XElement _data;

    private readonly EtcRepository? _etcRepo;

    public UnifiedDropBook(XElement data, EtcRepository? etcRepo)
        : base(data)
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
            if (!IsVanilla)
            {
                SetElementValue(_data, "TextId", value);
            }
            OnPropertyChanged("DisplayName");
        }
    }

    public string TextId
    {
        get => GetElementValue(_data, "TextId", Id);
        set
        {
            SetElementValue(_data, "TextId", value);
            OnPropertyChanged();
            OnPropertyChanged("LocalizedName");
        }
    }

    [NoAutoInit]
    public string LocalizedName
    {
        get => _etcRepo?.GetText(TextId) ?? "未翻译";
        set
        {
            if (!IsVanilla && _etcRepo != null && !(value == "未翻译"))
            {
                _etcRepo.SetText(TextId, value);
                OnPropertyChanged();
                OnPropertyChanged("DisplayName");
            }
        }
    }

    public string BookIcon
    {
        get => GetElementValue(_data, "BookIcon", "FullStopOffice");
        set => SetElementValue(_data, "BookIcon", value);
    }

    public int Chapter
    {
        get => GetInt(_data, "Chapter", 1);
        set => SetInt(_data, "Chapter", Math.Clamp(value, 1, 7));
    }

    public string Usage
    {
        get => GetAttr(_data, "Usage", "Reward");
        set => SetAttr(_data, "Usage", value);
    }

    public ObservableCollection<UnifiedDropItem> DropItems { get; } = [];

    private void LoadDrops()
    {
        DropItems.Clear();
        foreach (var item in Element.Elements("DropItem"))
        {
            if (item.Attribute("Type")?.Value == "Equip")
            {
                DropItems.Add(new UnifiedDropItem(item));
            }
        }
    }

    public void AddDropItem()
    {
        if (!IsVanilla)
        {
            var xElement = new XElement("DropItem", new XAttribute("Type", "Equip"), "");
            _data.Add(xElement);
            DropItems.Add(new UnifiedDropItem(xElement));
        }
    }

    public void RemoveDropItem(UnifiedDropItem i)
    {
        if (!IsVanilla)
        {
            i.Element.Remove();
            DropItems.Remove(i);
        }
    }

    public void DeleteXml()
    {
        if (!IsVanilla)
        {
            _data.Remove();
        }
    }
}
