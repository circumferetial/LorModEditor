using System.Windows;
using Synthesis.Core;
using Synthesis.Feature.Card;
using Synthesis.Feature.Passive;

namespace Synthesis.Feature.Book;

public class BookEditorViewModel : BindableBase
{
    // 显式字段定义 (为了兼容性，防止 field 关键字报错)

    public BookEditorViewModel(ProjectManager manager)
    {
        // 1. 安全检查
        Manager = manager;

        try
        {
            // --- 基础命令 ---
            CreateCommand = new DelegateCommand(CreateBook);

            DeleteCommand = new DelegateCommand(DeleteBook, () => SelectedItem != null)
                .ObservesProperty(() => SelectedItem);

            // --- 被动能力 (Passive) ---
            AddPassiveCommand =
                new DelegateCommand(AddPassive, () => SelectedItem != null && SelectedPassiveToAdd != null)
                    .ObservesProperty(() => SelectedItem)
                    .ObservesProperty(() => SelectedPassiveToAdd);

            // 【修复 1】LorId 是结构体，Prism 要求必须用 LorId? (可空类型)
            RemovePassiveCommand = new DelegateCommand<LorId?>(RemovePassive);

            // --- 专属卡牌 (OnlyCard) ---
            // UnifiedCard 是类，所以这里不需要加 ?
            AddOnlyCardCommand = new DelegateCommand<UnifiedCard>(AddOnlyCard, _ => SelectedItem != null)
                .ObservesProperty(() => SelectedItem);

            // 【修复 2】同理，这里也要改成 LorId?
            RemoveOnlyCardCommand = new DelegateCommand<LorId?>(RemoveOnlyCard, _ => SelectedItem != null)
                .ObservesProperty(() => SelectedItem);
        }
        catch (Exception ex)
        {
            // 捕获构造函数中的任何其他错误
            MessageBox.Show($"ViewModel 初始化崩溃: {ex.Message}");
            throw;
        }
    }

    // --- 属性 ---
    public ProjectManager Manager { get; }

    public UnifiedBook? SelectedItem
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                // 刷新按钮状态
                DeleteCommand.RaiseCanExecuteChanged();
                AddPassiveCommand.RaiseCanExecuteChanged();
                AddOnlyCardCommand.RaiseCanExecuteChanged();
                RemoveOnlyCardCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public UnifiedPassive? SelectedPassiveToAdd
    {
        get;
        set => SetProperty(ref field, value);
    }

    // --- 命令定义 ---
    public DelegateCommand CreateCommand { get; }
    public DelegateCommand DeleteCommand { get; }

    public DelegateCommand AddPassiveCommand { get; }

    // 【修复 3】属性定义改为 LorId?
    public DelegateCommand<LorId?> RemovePassiveCommand { get; }

    public DelegateCommand<UnifiedCard> AddOnlyCardCommand { get; }

    // 【修复 4】属性定义改为 LorId?
    public DelegateCommand<LorId?> RemoveOnlyCardCommand { get; }

    // --- 业务逻辑 ---

    private void CreateBook()
    {
        try
        {
            Manager.BookRepo.Create();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void DeleteBook()
    {
        if (SelectedItem != null &&
            MessageBox.Show($"确定删除书页 [{SelectedItem.DisplayName}]？", "提示", MessageBoxButton.YesNo) ==
            MessageBoxResult.Yes)
        {
            Manager.BookRepo.Delete(SelectedItem);
            SelectedItem = null;
        }
    }

    private void AddPassive()
    {
        if (SelectedItem != null && SelectedPassiveToAdd != null)
        {
            var pid = SelectedPassiveToAdd.GlobalId;
            if (SelectedItem.Passives.Contains(pid))
            {
                MessageBox.Show("该被动已存在！");
                return;
            }
            SelectedItem.AddPassive(pid);
        }
    }

    // 【修复 5】接收可空参数
    private void RemovePassive(LorId? pid)
    {
        if (pid.HasValue)
        {
            SelectedItem?.RemovePassive(pid.Value);
        }
    }

    private void AddOnlyCard(UnifiedCard? card)
    {
        if (SelectedItem != null && card != null)
        {
            var cid = card.GlobalId;
            if (SelectedItem.OnlyCards.Contains(cid)) return;
            SelectedItem.AddOnlyCard(cid);
        }
    }

    // 【修复 6】接收可空参数
    private void RemoveOnlyCard(LorId? cid)
    {
        if (SelectedItem != null && cid.HasValue)
        {
            SelectedItem.RemoveOnlyCard(cid.Value);
        }
    }
}