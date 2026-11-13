using System.Diagnostics;
using System.Numerics;
using System.Text.RegularExpressions;
using Lab1.Core.Models;

namespace Lab1.Core;

public static class FormulaParser
{
    private static readonly HashSet<string> Funcs = ["inc", "dec", "max", "min"];

    public static BaseNode Parse(string exp)
    {
        List<Token> tokens = ParseToTokens(exp);
        return ParseTree(tokens);
    }
    
    private static List<Token> ParseToTokens(string exp)
    {
        var tokens = new List<Token>();
        int i = 1;
        while (i < exp.Length)
        {
            char c = exp[i];

            if (char.IsWhiteSpace(c)) { i++; continue; }

            if (char.IsDigit(c))
            {
                int start = i;
                while (i < exp.Length && char.IsDigit(exp[i])) i++;
                tokens.Add(new Token(TokenType.Num, exp[start..i], start));
                continue;
            }

            if (char.IsLetter(c))
            {
                int start = i;
                while (i < exp.Length && char.IsLetterOrDigit(exp[i])) i++;
                string word = exp[start..i];

                if (Funcs.Contains(word.ToLower()))
                    tokens.Add(new Token(TokenType.Func, word.ToLower(), start));
                else if (Regex.IsMatch(word, @"^[A-Za-z]+\d+$"))
                    tokens.Add(new Token(TokenType.Ref, word.ToUpper(), start));
                else if (!Regex.IsMatch(word, @"^[A-Za-z0-9]+$"))
                    throw new ParsingException($"Недопустимі символи в '{word}'", start);
                else
                    throw new ParsingException($"Невідома функція '{word}'", start);
                continue;
            }

            TokenType? type = c switch
            {
                '+' or '-' => TokenType.PlusMinus,
                '^' => TokenType.Power,
                '(' => TokenType.LParen,
                ')' => TokenType.RParen,
                ',' => TokenType.Comma,
                _ => null
            };

            if (type == null)
                throw new ParsingException($"Невідомий символ '{c}'", i);

            tokens.Add(new Token(type.Value, c.ToString(), i));
            i++;
        }
        return tokens;
    }

    private static int SkipParens(List<Token> tokens, int position)
    {
        if (tokens[position].Type != TokenType.LParen)
            throw new ParsingException("Очікувалася (", tokens[position].Position);

        int depth = 1;
        while (position < tokens.Count - 1)
        {
            position++;
            if (tokens[position].Type == TokenType.LParen)
                depth++;
            else if (tokens[position].Type == TokenType.RParen)
            {
                depth--;
                if (depth == 0)
                    return position;
            }
        }
        throw new ParsingException("Незакриті дужки", tokens[position].Position);
    }

    private static BaseNode ParseFunc(Token funcToken, List<Token> tokens)
    {
        var args = new List<List<Token>>();
        var currentArg = new List<Token>();
        int depth = 0;

        foreach (var token in tokens)
        {
            if (token.Type == TokenType.LParen)
            {
                depth++;
                currentArg.Add(token);
            }
            else if (token.Type == TokenType.RParen)
            {
                depth--;
                currentArg.Add(token);
            }
            else if (token.Type == TokenType.Comma && depth == 0)
            {
                args.Add(currentArg);
                currentArg = new List<Token>();
            }
            else
                currentArg.Add(token);
        }

        if (currentArg.Count > 0 || tokens.Count > 0)
            args.Add(currentArg);

        string func = funcToken.Value;
        switch (func)
        {
            case "inc" or "dec" when args.Count != 1:
                throw new ParsingException($"В {func} передано {args.Count} аргументів замість 1", funcToken.Position);
            case "min" or "max" when args.Count != 2:
                throw new ParsingException($"В {func} передано {args.Count} аргументів замість 2", funcToken.Position);
            default:
            {
                var parsedArgs = args.Select(ParseTree).ToArray();
                return new FuncOpNode(func, parsedArgs);
            }
        }
    }

    private static BigInteger ParseNumber(Token token)
    {
        if (!BigInteger.TryParse(token.Value, out BigInteger number))
        {
            throw new ParsingException($"Неправильний формат числа: '{token.Value}'", token.Position);
        }

        return number;
    }

    private static BaseNode ParseTree(List<Token> tokens, int prevTokenPosition = 0)
    {
        if (tokens.Count == 0)
        {
            throw new ParsingException("Відсутній вираз або аргумент",  prevTokenPosition);
        }
            
        if (tokens.Count == 1)
        {
            if (tokens[0].Type == TokenType.Num)
                return new NumNode(ParseNumber(tokens[0]));
            if (tokens[0].Type == TokenType.Ref)
                return new RefNode(tokens[0].Value, tokens[0].Position);
            throw new ParsingException($"Неправильний токен або число: {tokens[0].Value}", tokens[0].Position);
        }

        int position = 0;
        int opPriority = 0;
        Token? topOp = null;
        int topOpPosition = -1;
        bool expectOperand = true;

        while (position < tokens.Count)
        {
            Token token = tokens[position];

            bool isOperandStart = token.Type is TokenType.Num or TokenType.LParen
                                  or TokenType.PlusMinus or TokenType.Func or TokenType.Ref;

            if (expectOperand && !isOperandStart)
                throw new ParsingException($"Очікувався операнд, отримано {token.Value}", token.Position);
            if (!expectOperand && isOperandStart)
                throw new ParsingException($"Очікувався оператор, отримано {token.Value}", token.Position);

            // Binary operator ^
            if (token.Type == TokenType.Power && opPriority < 3)
            {
                topOp = token;
                topOpPosition = position;
                opPriority = 3;
            }
            // Unary + or - (higher priority then ^)
            else if (token.Type == TokenType.PlusMinus && opPriority < 2)
            {
                topOp = token;
                topOpPosition = position;
                opPriority = 2;
            }
            // Functions
            else if (token.Type == TokenType.Func && opPriority < 1)
            {
                topOp = token;
                topOpPosition = position;
                opPriority = 1;
                if (position + 1 >= tokens.Count || tokens[position + 1].Type != TokenType.LParen)
                    throw new ParsingException($"Очікувалася '(' після функції {token.Value}", token.Position);
            }
            // Skip parens
            else if (token.Type == TokenType.LParen)
            {
                position = SkipParens(tokens, position);
                expectOperand = false;
                continue;
            }

            position++;
            expectOperand = token.Type is TokenType.Power or TokenType.PlusMinus or TokenType.LParen
                            or TokenType.Comma or TokenType.Func;
        }

        if (topOp is { Type: TokenType.Func })
        {
            int parenStart = topOpPosition + 1;
            int parenEnd = SkipParens(tokens, parenStart);
            if (parenEnd != tokens.Count - 1)
                throw new ParsingException($"Зайві токени після функції {topOp.Value}", tokens[parenEnd + 1].Position);
            return ParseFunc(topOp, tokens.GetRange(parenStart + 1, parenEnd - parenStart - 1));
        }
        
        if (topOp is { Type: TokenType.PlusMinus })
        {
            if (topOpPosition != 0)
                throw new ParsingException($"Неочікувані токени перед унарним {topOp.Value}", topOp.Position);
            return new UnaryOpNode(topOp.Value, ParseTree(tokens.GetRange(1, tokens.Count - 1), topOpPosition));
        }
        
        if (topOp is { Type: TokenType.Power })
        {
            var left = tokens.GetRange(0, topOpPosition);
            var right = tokens.GetRange(topOpPosition + 1, tokens.Count - topOpPosition - 1);
            if (left.Count == 0 || right.Count == 0)
                throw new ParsingException("Відсутній операнд для '^'", topOp.Position);
            return new BinaryOpNode("^", ParseTree(left, topOpPosition), ParseTree(right, topOpPosition));
        }
        
        if (tokens[0].Type == TokenType.LParen && SkipParens(tokens, 0) == tokens.Count - 1)
            return ParseTree(tokens.GetRange(1, tokens.Count - 2), topOpPosition);

        throw new ParsingException("Неочікувана послідовність токенів", tokens[0].Position);
    }
}