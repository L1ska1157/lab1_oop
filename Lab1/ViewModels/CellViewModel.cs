using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lab1.Core;
using Lab1.Core.Models;
using Cell = Lab1.Core.Models.Cell;

namespace Lab1.ViewModels;

public partial class CellViewModel(
    Cell cell,
    int row,
    int col,
    FormulaEngine engine,
    Func<bool> isExpressionMode,
    Action<CellViewModel> onSelected) : ObservableObject
{
    public int Row { get; } = row;
    public int Col { get; } = col;
    
    private string _value = "";

    // Що показувати в клітинці: вираз або порахований результат
    public string Text => isExpressionMode() ? cell.Exp ?? "" : _value;

    // Вираз для поля вводу
    public string Exp => cell.Exp ?? "";

    // Зберегти введений вираз (тут працює синтаксична перевірка)
    public void Apply(string exp) => cell.SetExpression(exp);

    // Порахувати значення й покласти в _value
    public void Recalculate()
    {
        if (cell.Type != CellType.Exp)
        {
            _value = cell.Value;
            return;
        }

        var res = engine.Calculate(cell.ExpTree!);
        _value = res.IsSuccess
            ? res.Result.ToString()
            : res.ErrorPos is int p
                ? $"{p + 2}: {res.ErrorMessage}"
                : res.ErrorMessage!;
    }

    // Сказати екрану: перечитай Text
    public void Refresh() => OnPropertyChanged(nameof(Text));

    // Команда для дотику по клітинці
    [RelayCommand]
    private void Select() => onSelected(this);
    
    [ObservableProperty] private bool _isSelected;
}