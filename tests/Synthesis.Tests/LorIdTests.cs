using System.Xml.Linq;
using Synthesis.Core;
using Xunit;

namespace Synthesis.Tests;

public class LorIdTests
{
    [Fact]
    public void ParseXmlReference_UsesDefaultPackage_WhenPidMissing()
    {
        var element = new XElement("Passive", "10001");

        var result = LorId.ParseXmlReference(element, "MyMod");

        Assert.Equal("MyMod", result.PackageId);
        Assert.Equal("10001", result.ItemId);
    }

    [Fact]
    public void ParseXmlReference_UsesPid_WhenProvided()
    {
        var element = new XElement("Passive", new XAttribute("Pid", "@origin"), "10002");

        var result = LorId.ParseXmlReference(element, "MyMod");

        Assert.Equal("@origin", result.PackageId);
        Assert.Equal("10002", result.ItemId);
        Assert.True(result.IsVanilla);
    }

    [Fact]
    public void ToString_ShowsVanillaPrefix_ForVanillaId()
    {
        var id = new LorId(LorId.Vanilla, "20001");

        Assert.Equal("[原版] 20001", id.ToString());
    }
}
