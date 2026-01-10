using System.Windows;
using LorModEditor.Core;

namespace LorModEditor.ViewModels;

public class AbilityEditorViewModel : BindableBase
{
    public AbilityEditorViewModel(ProjectManager manager)
    {
        Manager = manager;
        CreateCommand = new DelegateCommand(() => Manager.AbilityRepo.Create());
        DeleteCommand = new DelegateCommand(Delete, () => SelectedItem != null).ObservesProperty(() => SelectedItem);
        ScanMissingCommand = new DelegateCommand(ScanMissing);
    }

    public ProjectManager Manager { get; }

    public UnifiedAbility? SelectedItem
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand CreateCommand { get; }
    public DelegateCommand DeleteCommand { get; }
    public DelegateCommand ScanMissingCommand { get; }

    private void Delete()
    {
        if (SelectedItem != null && MessageBox.Show($"删除 [{SelectedItem.Id}]？", "提示", MessageBoxButton.YesNo) ==
            MessageBoxResult.Yes)
            Manager.AbilityRepo.Delete(SelectedItem);
    }

    private void ScanMissing()
    {
        var existingIds = new HashSet<string>(Manager.AbilityRepo.Items.Select(x => x.Id));
        var missingIds = new HashSet<string>();

        // 扫描卡牌
        foreach (var card in Manager.CardRepo.Items.Where(x => !x.IsVanilla))
        {
            if (!string.IsNullOrWhiteSpace(card.Script) && !existingIds.Contains(card.Script))
                missingIds.Add(card.Script);
            foreach (var d in card.Behaviours)
                if (!string.IsNullOrWhiteSpace(d.Script) && !existingIds.Contains(d.Script))
                    missingIds.Add(d.Script);
        }

        if (missingIds.Count == 0)
        {
            MessageBox.Show("未发现缺失项。");
            return;
        }

        if (MessageBox.Show($"发现 {missingIds.Count} 个缺失项，是否自动创建？", "扫描结果", MessageBoxButton.YesNo) ==
            MessageBoxResult.Yes)
        {
            foreach (var id in missingIds) Manager.AbilityRepo.Create(id);
            MessageBox.Show("补全完成！");
        }
    }
}