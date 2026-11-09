using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.Input;
using Lab1.Core;
using Lab1.Core.Models;

namespace Lab1.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private Table _table = new(10, 10);
    private FormulaEngine _engine;

    [ObservableProperty] private bool _isExpressionMode = true;
    [ObservableProperty] private string _editorText = "";
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteRowCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteColCommand))]
    private CellViewModel? _selectedCell;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    private bool _hasUnsavedChanges;

    private bool CanDeleteRow() => SelectedCell != null && _table.Rows > 1;
    private bool CanDeleteCol() => SelectedCell != null && _table.Cols > 1;

    private bool CanSave() => HasUnsavedChanges;

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

        string exp = EditorText.Trim();
        if (exp == SelectedCell.Exp) return;   // нічого не змінилось, не вважаємо це змінами

        SelectedCell.Apply(exp);
        SelectedCell.Refresh();
        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void AddRow()
    {
        _table.AddRow();
        Rebuild();
        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void AddCol()
    {
        _table.AddCol();
        Rebuild();
        HasUnsavedChanges = true;
    }
    
    [RelayCommand(CanExecute = nameof(CanDeleteRow))]
    private void DeleteRow()
    {
        int row = SelectedCell!.Row;
        _table.DeleteRow(row);
        Rebuild();
        HasUnsavedChanges = true;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteCol))]
    private void DeleteCol()
    {
        int col = SelectedCell!.Col;
        _table.DeleteCol(col);
        Rebuild();
        HasUnsavedChanges = true;
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
                new CellViewModel(_table.GetCellByRowCol(row, c), row, c, _engine, () => IsExpressionMode, OnCellSelected));
            Rows.Add(new RowViewModel(row, cells));
        }

        ColumnHeaders.Clear();
        for (int c = 1; c <= _table.Cols; c++) ColumnHeaders.Add(ColumnName(c));

        if (!IsExpressionMode) RecalculateAll();
        
        System.Diagnostics.Debug.WriteLine($"[rebuild] table {_table.Rows}x{_table.Cols}, Rows.Count={Rows.Count}");
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
    
    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        using var stream = new MemoryStream();
        TableFileManager.Save(_table, stream);
        stream.Position = 0;
        var result = await FileSaver.Default.SaveAsync("table.json", stream);

        if (result.IsSuccessful)
            HasUnsavedChanges = false;
        else
            await Shell.Current.DisplayAlert("Помилка", "Не вдалося зберегти файл", "OK");
    }

    [RelayCommand]
    [Obsolete("Obsolete")]
    private async Task LoadAsync()
    {
        if (HasUnsavedChanges)
        {
            bool proceed = await Shell.Current.DisplayAlert(
                "Незбережені зміни",
                "Поточна таблиця має незбережені зміни. Завантажити іншу таблицю без збереження?",
                "Завантажити", "Скасувати");
            if (!proceed) return;
        }

        var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Виберіть файл таблиці" });
        if (file == null) return;

        try
        {
            using var stream = await file.OpenReadAsync();
            _table = TableFileManager.Load(stream);
            _engine = new FormulaEngine(_table);
            Rebuild();
            HasUnsavedChanges = false;
        }
        catch (TableFileException e)
        {
            await Shell.Current.DisplayAlert("Помилка", e.Message, "OK");
        }
    }
    
    [RelayCommand]
    private async Task ExitAsync()
    {
        string message = HasUnsavedChanges
            ? "Є незбережені зміни, вони будуть втрачені. Ви точно хочете вийти?"
            : "Ви точно хочете вийти?";

        bool exit = await Shell.Current.DisplayAlert("Вихід", message, "Вийти", "Скасувати");
        if (exit) Application.Current?.Quit();
    }
    
    [RelayCommand]
    private async Task ShowHelpAsync() => await Shell.Current.GoToAsync(nameof(HelpPage));
}