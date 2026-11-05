using System.Globalization;
using System.Text.RegularExpressions;
using Lab1.Core.Models;

namespace Lab1.Core;

public class FormulaEngine
{
    
    private HashSet<string> _usedRefs = new();

    public CalcRes Calculate(BaseNode expTree)
    {
        try
        {
            return CalcRes.Ok(expTree.Calc());
        }
        catch (CalcException e)
        {
            return CalcRes.Fail(e.Message, e.Position);
        }
    }


    private List<Token> ParseToTokens(string exp)
    {
        var tokens = new List<Token>();
        int i = 0;
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
                else
                    throw new CalcException($"Unknown function '{word}'", start);
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
                throw new CalcException($"Can't process symbol '{c}'", i);

            tokens.Add(new Token(type.Value, c.ToString(), i));
            i++;
        }
        return tokens;
    }

    private BaseNode ParseFunc(Token funcToken, List<Token> tokens)
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
        if ((func == "inc" || func == "dec") && args.Count != 1)
            throw new CalcException($"{func} expects 1 argument, got {args.Count}", funcToken.Position);
        if ((func == "min" || func == "max") && args.Count != 2)
            throw new CalcException($"{func} expects 2 arguments, got {args.Count}", funcToken.Position);

        var parsedArgs = args.Select(a => ParseTree(a)).ToArray();
        return new FuncOpNode(func, parsedArgs);
    }

    private BaseNode ParseTree(List<Token> tokens)
    {
        if (tokens.Count == 0)
            throw new CalcException("Empty expression or argument");

        if (tokens.Count == 1)
        {
            if (tokens[0].Type == TokenType.Num)
                return new NumNode(FormulaParser.ParseNumber(tokens[0]));
            if (tokens[0].Type == TokenType.Ref)
                return new RefNode(tokens[0].Value, tokens[0].Position);
            throw new CalcException($"Invalid token/number: {tokens[0].Value}", tokens[0].Position);
        }

        int position = 0;
        int opPriority = 0;
        Token? topOp = null;
        int topOpPosition = -1;
        bool expectOperand = true;

        while (position < tokens.Count)
        {
            var token = tokens[position];

            bool isOperandStart = token.Type is TokenType.Num or TokenType.LParen
                or TokenType.PlusMinus or TokenType.Func;

            if (expectOperand && !isOperandStart)
                throw new CalcException($"Expected operand, got {token.Value}", token.Position);
            if (!expectOperand && isOperandStart)
                throw new CalcException($"Expected operator, got {token.Value}", token.Position);

            // Бінарний степінь ^ (корінь дерева; перший ^ дає праву асоціативність)
            if (token.Type == TokenType.Power && opPriority < 3)
            {
                topOp = token;
                topOpPosition = position;
                opPriority = 3;
            }
            // Унарний + або - (прив'язується сильніше за ^)
            else if (token.Type == TokenType.PlusMinus && opPriority < 2)
            {
                topOp = token;
                topOpPosition = position;
                opPriority = 2;
            }
            // Функції min, max, inc, dec
            else if (token.Type == TokenType.Func && opPriority < 1)
            {
                topOp = token;
                topOpPosition = position;
                opPriority = 1;
                if (position + 1 >= tokens.Count || tokens[position + 1].Type != TokenType.LParen)
                    throw new CalcException($"Expected '(' after function {token.Value}", token.Position);
            }
            // Пропуск блоків у дужках
            else if (token.Type == TokenType.LParen)
            {
                position = FormulaParser.SkipParens(tokens, position);
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
            int parenEnd = FormulaParser.SkipParens(tokens, parenStart);
            if (parenEnd != tokens.Count - 1)
                throw new CalcException($"Trailing tokens after function {topOp.Value}", tokens[parenEnd + 1].Position);
            return ParseFunc(topOp, tokens.GetRange(parenStart + 1, parenEnd - parenStart - 1));
        }
        else if (topOp is { Type: TokenType.PlusMinus })
        {
            if (topOpPosition != 0)
                throw new CalcException($"Unexpected token before unary {topOp.Value}", topOp.Position);
            return new UnaryOpNode(topOp.Value, ParseTree(tokens.GetRange(1, tokens.Count - 1)));
        }
        else if (topOp is { Type: TokenType.Power })
        {
            var left = tokens.GetRange(0, topOpPosition);
            var right = tokens.GetRange(topOpPosition + 1, tokens.Count - topOpPosition - 1);
            if (left.Count == 0 || right.Count == 0)
                throw new CalcException("Missing operand for '^'", topOp.Position);
            return new BinaryOpNode("^", ParseTree(left), ParseTree(right));
        }
        else if (tokens[0].Type == TokenType.LParen && FormulaParser.SkipParens(tokens, 0) == tokens.Count - 1)
            return ParseTree(tokens.GetRange(1, tokens.Count - 2));

        else
            throw new CalcException("Unexpected sequence of tokens", tokens[0].Position);
    }
}