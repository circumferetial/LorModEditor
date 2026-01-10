using System.Windows;
using LorModEditor.Core;

namespace LorModEditor.ViewModels;

/// <summary>
///     卡牌编辑器的视图模型 (ViewModel)
///     负责处理业务逻辑，不直接操作 UI 控件
/// </summary>
public class CardEditorViewModel : BindableBase
{
    // --- 构造函数 (依赖注入) ---
    public CardEditorViewModel(ProjectManager manager)
    {
        Manager = manager;

        // 初始化命令
        // 格式: new DelegateCommand(执行的方法, 判断是否可执行的方法)

        // 1. 创建卡牌
        CreateCommand = new DelegateCommand(CreateCard);

        // 2. 删除卡牌 (只有当 SelectedCard 不为空时才能点)
        // .ObservesProperty 意思时：当 SelectedCard 变化时，自动重新检查按钮是否变灰
        DeleteCommand = new DelegateCommand(DeleteCard, () => SelectedCard != null)
            .ObservesProperty(() => SelectedCard);

        // 3. 骰子操作
        AddDiceCommand = new DelegateCommand(AddDice, () => SelectedCard != null)
            .ObservesProperty(() => SelectedCard);

        // 只有当选中了卡牌且选中了某行骰子时，才能删除骰子
        RemoveDiceCommand = new DelegateCommand(RemoveDice, () => SelectedCard != null && SelectedDice != null)
            .ObservesProperty(() => SelectedCard)
            .ObservesProperty(() => SelectedDice);

        // 4. 特性(Option)操作
        AddOptionCommand = new DelegateCommand(AddOption,
                () => SelectedCard != null && !string.IsNullOrEmpty(SelectedOptionToAdd))
            .ObservesProperty(() => SelectedCard)
            .ObservesProperty(() => SelectedOptionToAdd);// 只有下拉框选了值，才能点添加

        // 删除特性带参数 (因为按钮在列表每一行里)，不需要 CanExecute 检查
        RemoveOptionCommand = new DelegateCommand<string>(RemoveOption);

        AddKeywordCommand = new DelegateCommand(AddKeyword,
                () => SelectedCard != null && !string.IsNullOrWhiteSpace(KeywordToAdd))
            .ObservesProperty(() => SelectedCard)
            .ObservesProperty(() => KeywordToAdd);

        RemoveKeywordCommand = new DelegateCommand<string>(RemoveKeyword);
        DuplicateCommand = new DelegateCommand(DuplicateCard, () => SelectedCard != null)
            .ObservesProperty(() => SelectedCard);
    }

    // 持有 ProjectManager 的引用
    public string KeywordToAdd
    {
        get;
        set => SetProperty(ref field, value);
    } = "";

    // 【新增】命令
    public DelegateCommand AddKeywordCommand { get; }

    public DelegateCommand<string> RemoveKeywordCommand { get; }
    // --- 属性绑定 (Data Binding) ---

    /// <summary>
    ///     数据源管理器，供界面绑定列表 (Manager.CardRepo.Items)
    /// </summary>
    public ProjectManager Manager { get; }

    // 2. 使用标准属性写法
    public UnifiedCard? SelectedCard
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                // 切换卡牌时，清空选中的骰子
                SelectedDice = null;

                // 强制刷新删除命令的状态 (虽然 ObservesProperty 应该会做，但手动加个保险)
                DeleteCommand.RaiseCanExecuteChanged();
                AddDiceCommand.RaiseCanExecuteChanged();
            }
        }
    }


    public UnifiedBehaviour? SelectedDice
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                RemoveDiceCommand.RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    ///     特性下拉框当前选中的值 (例如 "OnlyPage")
    /// </summary>
    public string? SelectedOptionToAdd
    {
        get;
        set => SetProperty(ref field, value);
    }

    // --- 命令 (Commands) ---

    public DelegateCommand CreateCommand { get; }
    public DelegateCommand DeleteCommand { get; }
    public DelegateCommand AddDiceCommand { get; }
    public DelegateCommand RemoveDiceCommand { get; }
    public DelegateCommand AddOptionCommand { get; }
    public DelegateCommand<string> RemoveOptionCommand { get; }// 带参数
    public DelegateCommand DuplicateCommand { get; }

    private void AddKeyword()
    {
        if (SelectedCard != null && !string.IsNullOrWhiteSpace(KeywordToAdd))
        {
            SelectedCard.AddKeyword(KeywordToAdd.Trim());
            KeywordToAdd = "";// 添加完清空输入框
        }
    }

    private void RemoveKeyword(string keyword)
    {
        SelectedCard?.RemoveKeyword(keyword);
    }

    // --- 业务逻辑 ---

    private void DuplicateCard()
    {
        try
        {
            if (SelectedCard != null)
            {
                Manager.CardRepo.DuplicateCard(SelectedCard);
                // 此时新卡会加到列表末尾
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"复制失败: {ex.Message}");
        }
    }

    private void CreateCard()
    {
        try
        {
            // 调用仓库创建
            SelectedCard = Manager.CardRepo.Create();
            // 此时 Repository 会自动把新卡加到 Items 里，UI 会自动刷新
            // 这里的选中逻辑，如果 Repository 没有返回新对象，可能需要在 View 层处理，或者让 Repo 返回新对象
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteCard()
    {
        if (SelectedCard != null &&
            MessageBox.Show($"确定删除 [{SelectedCard.DisplayName}] 吗？", "提示", MessageBoxButton.YesNo) ==
            MessageBoxResult.Yes)
        {
            Manager.CardRepo.Delete(SelectedCard);
            SelectedCard = null;// 清空选中
        }
    }

    private void AddDice()
    {
        SelectedCard?.AddBehaviour();
    }

    private void RemoveDice()
    {
        if (SelectedCard != null && SelectedDice != null)
        {
            SelectedCard.RemoveBehaviour(SelectedDice);
            SelectedDice = null;// 删完后清空选中
        }
    }

    private void AddOption()
    {
        if (SelectedCard != null && !string.IsNullOrEmpty(SelectedOptionToAdd))
        {
            SelectedCard.AddOption(SelectedOptionToAdd);
        }
    }

    private void RemoveOption(string optionName)
    {
        if (SelectedCard != null && !string.IsNullOrEmpty(optionName))
        {
            SelectedCard.RemoveOption(optionName);
        }
    }
}