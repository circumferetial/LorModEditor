using System.IO;
using System.Xml.Linq;
using LorModEditor.Core.Extension;
using LorModEditor.Core.Wrappers;

// 引用 UnifiedEnemy

namespace LorModEditor.Core.Services;

public class EnemyRepository : BaseRepository<UnifiedEnemy>
{
    private readonly List<XDocument> _deckDocs = [];

    // 敌人仓库比较特殊，它有两个独立的数据源列表
    private readonly List<XDocument> _unitDocs = [];

    // 重写状态检查：必须 Unit 和 Deck 都有非原版文件才算 HasData
    public override bool HasData => _unitDocs.Any(d => !d.IsVanilla()) && _deckDocs.Any(d => !d.IsVanilla());

    // 添加文档的方法
    public void AddUnitDoc(XDocument doc)
    {
        _unitDocs.Add(doc);
        NotifyStatusChanged();
    }

    public void AddDeckDoc(XDocument doc)
    {
        _deckDocs.Add(doc);
        NotifyStatusChanged();
    }

    public override void Clear()
    {
        _unitDocs.Clear();
        _deckDocs.Clear();
        base.Clear();// 清空 Items 和 _locDocs
    }

    public override void LoadResources(string root, string lang, string modId)
    {
        // 1. Unit 数据
        ScanAndLoad(Path.Combine(root, @"StaticInfo\EnemyUnitInfo"), "EnemyUnitClassRoot", modId, AddUnitDoc);
        // 2. Deck 数据
        ScanAndLoad(Path.Combine(root, @"StaticInfo\Deck"), "DeckXmlRoot", modId, AddDeckDoc);
        // 3. 翻译 (CharactersName)
        ScanAndLoad(Path.Combine(root, $@"Localize\{lang}\CharactersName"), "CharactersNameRoot", modId, AddLocDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        // 检查是否有非原版的 Unit 文件
        if (_unitDocs.All(d => d.IsVanilla()))
            CreateXmlTemplate(Path.Combine(root, @"StaticInfo\EnemyUnitInfo\EnemyUnitInfo.xml"), "EnemyUnitClassRoot",
                modId, AddUnitDoc);

        // 检查是否有非原版的 Deck 文件
        if (_deckDocs.All(d => d.IsVanilla()))
            CreateXmlTemplate(Path.Combine(root, @"StaticInfo\Deck\Deck.xml"), "DeckXmlRoot", modId, AddDeckDoc);

        // 检查翻译
        if (!HasLoc)
            CreateXmlTemplate(Path.Combine(root, $@"Localize\{lang}\CharactersName\EnemyName.xml"),
                "CharactersNameRoot", modId, AddLocDoc);
    }

    public override void SaveFiles(string currentModId)
    {
        SaveDocs(_unitDocs, currentModId);
        SaveDocs(_deckDocs, currentModId);
        SaveDocs(_locDocs, currentModId);
    }

    public override void Load()
    {
        // 预先获取翻译文件的默认挂载点（用于传递给 Wrapper）
        var defaultLocParent = GetTargetLocDoc("CharactersNameRoot")?.Root;
        // 预先获取 Deck 文件的默认挂载点
        var defaultDeckParent = _deckDocs.FirstOrDefault(d => !d.IsVanilla())?.Root;

        foreach (var doc in _unitDocs)
        {
            if (doc.Root?.Name.LocalName != "EnemyUnitClassRoot") continue;

            foreach (var node in doc.Root.Elements("Enemy"))
            {
                var id = node.Attribute("ID")?.Value.Trim() ?? "";
                if (string.IsNullOrEmpty(id)) continue;

                // 获取 NameID，没有则回退到 ID
                var nameId = node.Element("NameID")?.Value?.Trim() ?? id;

                // 1. 查找对应的 Deck
                XElement? deck = null;
                foreach (var d in _deckDocs)
                {
                    // 只需要找 ID 匹配的 Deck
                    deck = d.Descendants("Deck").FirstOrDefault(x => x.Attribute("ID")?.Value.Trim() == id);
                    if (deck != null) break;
                }

                // 2. 查找对应的 Name
                XElement? text = null;
                // 注意：_locDocs 里可能混着 StageName，但因为 LoadResources 指定了路径，这里相对安全
                // 也可以再次过滤 RootName
                foreach (var l in _locDocs.Where(d => d.Root?.Name.LocalName == "CharactersNameRoot"))
                {
                    text = l.Descendants("Name").FirstOrDefault(x => x.Attribute("ID")?.Value.Trim() == nameId);
                    if (text != null) break;
                }

                // 添加到列表
                Items.Add(new UnifiedEnemy(node, deck, text, defaultLocParent, defaultDeckParent));
            }
        }
    }

    public void Create()
    {
        // 1. 查找可写入的 Unit 文件 (非原版)
        var unitDoc = _unitDocs.FirstOrDefault(d => !d.IsVanilla());
        // 2. 查找可写入的 Deck 文件 (非原版)
        var deckDoc = _deckDocs.FirstOrDefault(d => !d.IsVanilla());

        if (unitDoc == null || deckDoc == null)
        {
            throw new Exception("未找到可写入的敌人数据文件或卡组文件(非原版)。\n请检查 StaticInfo 文件夹结构。");
        }

        // 3. 计算 ID
        var newId = 10000000;
        if (Items.Any(x => !x.IsVanilla))
        {
            // 只计算 Mod 数据的最大 ID
            newId = Items.Where(x => !x.IsVanilla)
                .Max(x => int.Parse(x.Id)) + 1;
        }
        var strId = newId.ToString();

        // 4. 创建 Unit 节点
        var newUnit = new XElement("Enemy", new XAttribute("ID", strId));
        newUnit.Add(new XElement("NameID", strId));
        newUnit.Add(new XElement("MinHeight", "175"));
        newUnit.Add(new XElement("MaxHeight", "185"));
        newUnit.Add(new XElement("BookId", ""));
        newUnit.Add(new XElement("Retreat", "false"));// 默认不撤退

        unitDoc.Root?.Add(newUnit);

        // 5. 创建 Deck 节点
        var newDeck = new XElement("Deck", new XAttribute("ID", strId));
        deckDoc.Root?.Add(newDeck);

        // 6. 创建 Name 节点
        var locParent = GetTargetLocDoc("CharactersNameRoot")?.Root;
        XElement? newText = null;
        if (locParent != null)
        {
            newText = new XElement("Name", new XAttribute("ID", strId), "New Enemy");
            locParent.Add(newText);
        }

        // 7. 添加到 UI
        Items.Add(new UnifiedEnemy(newUnit, newDeck, newText, locParent, deckDoc.Root));
    }

    public override void Delete(UnifiedEnemy item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}