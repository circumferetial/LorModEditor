using System.Collections.ObjectModel;
using System.Xml.Linq;
using Synthesis.Core;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.Stage;

public class UnifiedWave : XWrapper
{
    public UnifiedWave(XElement element) : base(element)
    {
        LoadUnits();
        InitDefaults();
    }

    // 使用 GetInt (Element)
    public int FormationId
    {
        get => GetInt(Element, "Formation", 1);
        set => SetInt(Element, "Formation", value);
    }

    public int AvailableUnit
    {
        get => GetInt(Element, "AvailableUnit", 5);
        set => SetInt(Element, "AvailableUnit", value);
    }

    public string ManagerScript
    {
        get => GetElementValue(Element, "ManagerScript");
        set => SetElementValue(Element, "ManagerScript", value);
    }

    public ObservableCollection<LorId> Units { get; } = new();

    private void LoadUnits()
    {
        Units.Clear();
        foreach (var u in Element.Elements("Unit"))
            Units.Add(LorId.ParseXmlReference(u, GlobalId.PackageId));
    }

    public void AddUnit(LorId uid)
    {
        if (IsVanilla) return;
        var node = new XElement("Unit", uid.ItemId);
        if (uid.PackageId != GlobalId.PackageId) node.SetAttributeValue("Pid", uid.PackageId);
        Element.Add(node);
        Units.Add(uid);
    }

    public void RemoveUnit(LorId uid)
    {
        if (IsVanilla) return;
        var node = Element.Elements("Unit").FirstOrDefault(x =>
            (x.Attribute("Pid")?.Value ?? GlobalId.PackageId) == uid.PackageId && x.Value == uid.ItemId);
        node?.Remove();
        Units.Remove(uid);
    }
}