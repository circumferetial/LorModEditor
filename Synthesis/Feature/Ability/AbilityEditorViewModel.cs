using System.Windows;
using Synthesis.Core;

namespace Synthesis.Feature.Ability;

public class AbilityEditorViewModel : BindableBase
{
    public AbilityEditorViewModel(ProjectManager manager)
    {
        Manager = manager;
        CreateCommand = new DelegateCommand(delegate { Manager.AbilityRepo.Create(); });
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
        if (SelectedItem != null && MessageBox.Show("删除 [" + SelectedItem.Id + "]？", "提示", MessageBoxButton.YesNo) ==
            MessageBoxResult.Yes)
        {
            Manager.AbilityRepo.Delete(SelectedItem);
        }
    }

    private void ScanMissing()
    {
        var hashSet = new HashSet<string>(Manager.AbilityRepo.Items.Select(x => x.Id));
        var hashSet2 = new HashSet<string>();
        foreach (var item in Manager.CardRepo.Items.Where(x => !x.IsVanilla))
        {
            if (!string.IsNullOrWhiteSpace(item.Script) && !hashSet.Contains(item.Script))
            {
                hashSet2.Add(item.Script);
            }
            foreach (var behaviour in item.Behaviours)
            {
                if (!string.IsNullOrWhiteSpace(behaviour.Script) && !hashSet.Contains(behaviour.Script))
                {
                    hashSet2.Add(behaviour.Script);
                }
            }
        }
        if (hashSet2.Count == 0)
        {
            MessageBox.Show("未发现缺失项。");
        }
        else
        {
            if (MessageBox.Show($"发现 {hashSet2.Count} 个缺失项，是否自动创建？", "扫描结果", MessageBoxButton.YesNo) !=
                MessageBoxResult.Yes)
            {
                return;
            }
            foreach (var item2 in hashSet2)
            {
                Manager.AbilityRepo.Create(item2);
            }
            MessageBox.Show("补全完成！");
        }
    }
}
