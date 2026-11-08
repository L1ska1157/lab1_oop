using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lab1.Core;
using Lab1.Core.Models;

namespace Lab1.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly Table _table = new(10, 10);
    private readonly FormulaEngine _engine;

    [ObservableProperty] private bool _isExpressionMode = true;
    [ObservableProperty] private string _editorText = "";
    [ObservableProperty] private CellViewModel? _selectedCell;

    public ObservableCollection<RowViewModel> Rows { get; } = [];
    public ObservableCollection<string> ColumnHeaders { get; } = [];

    public MainViewModel()
    {
        _engine = new FormulaEngine(_table);
        Rebuild();
    }

    // Викликається автоматично, коли змінилась вибрана клітинка:
    // її вираз іде в поле вводу
    partial void OnSelectedCellChanged(CellViewModel? oldValue, CellViewModel? newValue)
    {
        if (oldValue != null) oldValue.IsSelected = false;
        if (newValue != null) newValue.IsSelected = true;
        EditorText = newValue?.Exp ?? "";
    }

    // Викликається автоматично, коли змінився режим
    partial void OnIsExpressionModeChanged(bool value)
    {
        if (!value) RecalculateAll();   // перейшли в ЗНАЧЕННЯ: рахуємо
        RefreshAll();                   // і перемальовуємо
    }

    [RelayCommand]
    private void Commit()
    {
        if (SelectedCell == null || !IsExpressionMode) return;
        SelectedCell.Apply(EditorText);
        SelectedCell.Refresh();
    }

    [RelayCommand]
    private void AddRow()
    {
        _table.AddRow();
        Rebuild();
    }

    [RelayCommand]
    private void AddCol()
    {
        _table.AddCol();
        Rebuild();
    }

    private void OnCellSelected(CellViewModel cell) => SelectedCell = cell;

    // Створює екранні об'єкти з даних Table
    private void Rebuild()
    {
        SelectedCell = null;

        Rows.Clear();
        for (int r = 1; r <= _table.Rows; r++)
        {
            int row = r;
            var cells = Enumerable.Range(1, _table.Cols).Select(c =>
                new CellViewModel(_table.GetCellByRowCol(row, c), _engine, () => IsExpressionMode, OnCellSelected));
            Rows.Add(new RowViewModel(row, cells));
        }

        ColumnHeaders.Clear();
        for (int c = 1; c <= _table.Cols; c++) ColumnHeaders.Add(ColumnName(c));

        if (!IsExpressionMode) RecalculateAll();
    }

    private void RecalculateAll()
    {
        foreach (var row in Rows)
            foreach (var cell in row.Cells)
                cell.Recalculate();
    }

    private void RefreshAll()
    {
        foreach (var row in Rows)
            foreach (var cell in row.Cells)
                cell.Refresh();
    }

    // 1 -> A, 26 -> Z, 27 -> AA
    private static string ColumnName(int col)
    {
        string name = "";
        while (col > 0)
        {
            col--;
            name = (char)('A' + col % 26) + name;
            col /= 26;
        }
        return name;
    }
}