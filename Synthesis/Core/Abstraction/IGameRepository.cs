namespace Synthesis.Core.Abstraction;

public interface IGameRepository
{
    void LoadResources(string projectRoot, string language, string modId);

    void LoadLocResources(string projectRoot, string language, string modId);

    void EnsureDefaults(string projectRoot, string language, string modId);

    void Parse(bool containOriginal);

    void ClearLocOnly();

    void ClearModOnly();

    void ClearAll();

    void SaveFiles(string currentModId);
}
