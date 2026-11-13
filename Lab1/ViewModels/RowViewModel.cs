namespace Lab1.ViewModels;

public class RowViewModel(int number, IEnumerable<CellViewModel> cells)
{
    public int Number { get; } = number;
    public List<CellViewModel> Cells { get; } = new(cells);
}