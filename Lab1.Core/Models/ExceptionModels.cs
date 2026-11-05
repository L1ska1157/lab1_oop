namespace Lab1.Core.Models;

public class CalcException : Exception
{
    public int? Position { get; }
    public CalcException(string message, int? position = null) : base(message)
        => Position = position;
}

public class ParsingException : Exception 
{
    public int? Position { get; }
    public ParsingException(string message, int? position = null) : base(message)
        => Position = position;
}