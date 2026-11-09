namespace Lab1.Core.Models;

public class CalcException(string message, int? position = null) : Exception(message)
{
    public int? Position { get; } = position;
}

public class ParsingException : Exception 
{
    public int? Position { get; }
    public ParsingException(string message, int? position = null) : base(message)
        => Position = position;
}

public class CellNotFoundException(string message) : Exception(message);

public class TableFileException(string message) : Exception(message);