using System.Collections.ObjectModel;
using System.Windows;
using Synthesis.Core;

namespace Synthesis.Feature.Card;

public class CardEditorViewModel : BindableBase
{
    private UnifiedCard? _selectedCard;
    private int _visibleBehavioursVersion;

    public CardEditorViewModel(ProjectManager manager)
    {
        Manager = manager;
        CreateCommand = new DelegateCommand(CreateCard);
        DeleteCommand =
            new DelegateCommand(DeleteCard, () => SelectedCard != null).ObservesProperty(() => SelectedCard);
        AddDiceCommand = new DelegateCommand(AddDice, () => SelectedCard != null).ObservesProperty(() => SelectedCard);
        RemoveDiceCommand =
            new DelegateCommand<UnifiedBehaviour>(RemoveDice, _ => SelectedCard != null).ObservesProperty(() =>
                SelectedCard);
        AddOptionCommand =
            new DelegateCommand(AddOption, () => SelectedCard != null && !string.IsNullOrEmpty(SelectedOptionToAdd))
                .ObservesProperty(() => SelectedCard).ObservesProperty(() => SelectedOptionToAdd);
        RemoveOptionCommand = new DelegateCommand<string>(RemoveOption);
        AddKeywordCommand =
            new DelegateCommand(AddKeyword, () => SelectedCard != null && !string.IsNullOrWhiteSpace(KeywordToAdd))
                .ObservesProperty(() => SelectedCard).ObservesProperty(() => KeywordToAdd);
        RemoveKeywordCommand = new DelegateCommand<string>(RemoveKeyword);
        DuplicateCommand =
            new DelegateCommand(DuplicateCard, () => SelectedCard != null).ObservesProperty(() => SelectedCard);
    }

    public string KeywordToAdd
    {
        get;
        set => SetProperty(ref field, value);
    } = "";

    public DelegateCommand AddKeywordCommand { get; }

    public DelegateCommand<string> RemoveKeywordCommand { get; }

    public ProjectManager Manager { get; }

    public UnifiedCard? SelectedCard
    {
        get => _selectedCard;
        set
        {
            if (SetProperty(ref _selectedCard, value))
            {
                SelectedDice = null;
                DeleteCommand.RaiseCanExecuteChanged();
                AddDiceCommand.RaiseCanExecuteChanged();
                RefreshVisibleBehavioursAsync();
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

    public string? SelectedOptionToAdd
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ObservableCollection<UnifiedBehaviour>? VisibleBehaviours
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public DelegateCommand CreateCommand { get; }

    public DelegateCommand DeleteCommand { get; }

    public DelegateCommand AddDiceCommand { get; }

    public DelegateCommand<UnifiedBehaviour> RemoveDiceCommand { get; }

    public DelegateCommand AddOptionCommand { get; }

    public DelegateCommand<string> RemoveOptionCommand { get; }

    public DelegateCommand DuplicateCommand { get; }

    private void AddKeyword()
    {
        if (SelectedCard != null && !string.IsNullOrWhiteSpace(KeywordToAdd))
        {
            SelectedCard.AddKeyword(KeywordToAdd.Trim());
            KeywordToAdd = "";
        }
    }

    private void RemoveKeyword(string keyword)
    {
        SelectedCard?.RemoveKeyword(keyword);
    }

    private void DuplicateCard()
    {
        try
        {
            if (SelectedCard != null)
            {
                Manager.CardRepo.DuplicateCard(SelectedCard);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("复制失败: " + ex.Message);
        }
    }

    private void CreateCard()
    {
        try
        {
            SelectedCard = Manager.CardRepo.Create();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
        }
    }

    private void DeleteCard()
    {
        if (SelectedCard != null &&
            MessageBox.Show("确定删除 [" + SelectedCard.DisplayName + "] 吗？", "提示", MessageBoxButton.YesNo) ==
            MessageBoxResult.Yes)
        {
            Manager.CardRepo.Delete(SelectedCard);
            SelectedCard = null;
        }
    }

    private void AddDice()
    {
        SelectedCard?.AddBehaviour();
    }

    private void RemoveDice(UnifiedBehaviour? targetDice)
    {
        if (SelectedCard != null && targetDice != null)
        {
            SelectedCard.RemoveBehaviour(targetDice);
            if (SelectedDice == targetDice)
            {
                SelectedDice = null;
            }
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

    private async void RefreshVisibleBehavioursAsync()
    {
        var currentVersion = ++_visibleBehavioursVersion;
        VisibleBehaviours = null;

        if (_selectedCard == null)
        {
            return;
        }

        await Task.Yield();

        if (currentVersion != _visibleBehavioursVersion)
        {
            return;
        }

        VisibleBehaviours = _selectedCard.Behaviours;
    }
}
