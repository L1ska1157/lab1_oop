namespace Lab1.Core.Models;

public class CalcRes
{
    public double Result { get; init; }
    public string? ErrorMessage { get; init; }
    public int? ErrorPos { get; init; }
    public bool IsSuccess => ErrorMessage == null;

    public static CalcRes Ok(double r) => new() { Result = r };
    public static CalcRes Fail(string msg, int? pos) => new() { ErrorMessage = msg, ErrorPos = pos };
}



