using System.Collections.ObjectModel;
using System.Xml.Linq;
using Synthesis.Core;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.Stage;

public class UnifiedWave : XWrapper
{
    public UnifiedWave(XElement element)
        : base(element)
    {
        LoadUnits();
        InitDefaults();
    }

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

    public ObservableCollection<LorId> Units { get; } = [];

    private void LoadUnits()
    {
        Units.Clear();
        foreach (var item in Element.Elements("Unit"))
        {
            Units.Add(LorId.ParseXmlReference(item, GlobalId.PackageId));
        }
    }

    public void AddUnit(LorId uid)
    {
        if (!IsVanilla)
        {
            var xElement = new XElement("Unit", uid.ItemId);
            if (uid.PackageId != GlobalId.PackageId)
            {
                xElement.SetAttributeValue("Pid", uid.PackageId);
            }
            Element.Add(xElement);
            Units.Add(uid);
        }
    }

    public void RemoveUnit(LorId uid)
    {
        if (!IsVanilla)
        {
            Element.Elements("Unit").FirstOrDefault(x =>
                (x.Attribute("Pid")?.Value ?? GlobalId.PackageId) == uid.PackageId && x.Value == uid.ItemId)?.Remove();
            Units.Remove(uid);
        }
    }
}
