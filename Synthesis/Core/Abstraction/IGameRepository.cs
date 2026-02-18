namespace Synthesis.Core.Abstraction;

public interface IGameRepository
{
    // 加载资源（读取XML到内存，不解析对象）
    void LoadResources(string projectRoot, string language, string modId);
    
    // 确保默认文件存在
    void EnsureDefaults(string projectRoot, string language, string modId);
    
    // 解析数据（将XML转为对象，建立引用关系，填充 ObservableCollection）
    void Load();
    
    // 清除模组数据
    void ClearModOnly();
    
    // 清除所有数据
    void ClearAll();
    
    // 保存文件
    void SaveFiles(string currentModId);
}