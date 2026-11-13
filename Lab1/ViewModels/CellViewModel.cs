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

    public string Text => isExpressionMode() ? cell.Exp ?? "" : _value;

    public string Exp => cell.Exp ?? "";

    public void Apply(string exp) => cell.SetExpression(exp);

    public void Recalculate()
    {
        if (cell.Type != CellType.Exp)
        {
            _value = cell.Value;
            return;
        }

        try
        {
            _value = engine.Calculate(cell.ExpTree!).ToString();
        }
        catch (CalcException e)
        {
            _value = e.Position is int p ? $"{p + 1}: {e.Message}" : e.Message;
        }
    }

    public void Refresh() => OnPropertyChanged(nameof(Text));

    [RelayCommand]
    private void Select() => onSelected(this);
    
    [ObservableProperty] private bool _isSelected;
}