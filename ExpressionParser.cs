using System;
using System.Collections.Generic;
using System.Text;

namespace GraphEq
{
    delegate VariableExpr TryGetVariable(string name);

    class ParseException : Exception
    {
        public ParseException(Lexer lexer, string message) : base(message)
        {
            InputString = lexer.InputString;
            InputPos = lexer.TokenPos;
        }
        public string InputString { get; }
        public int InputPos { get; }

    }

    internal class ExpressionParser
    {
        Lexer m_lexer = new Lexer();
        string[] m_varNames;

        public ExpressionParser()
        {
        }

        public Expr Parse(string input, string[] varNames)
        {
            m_lexer.InputString = input;
            CheckLexerError();

            m_varNames = varNames;

            var expr = ParseExpression();

            if (m_lexer.TokenType != TokenType.None)
                throw new ParseException(m_lexer, "Invalid expression.");

            return expr.Simplify();
        }

        void CheckLexerError()
        {
            if (m_lexer.HaveError)
            {
                throw new ParseException(m_lexer, "Invalid token.");
            }
        }

        void Advance()
        {
            m_lexer.Advance();
            CheckLexerError();
        }

        // expr -> unary (BinOp expr)*
        Expr ParseExpression()
        {
            if (!m_lexer.HaveToken)
                throw new ParseException(m_lexer, "Expected expression.");

            var expr = ParseUnary();

            var op = GetBinaryOp(m_lexer.TokenSymbol);
            if (op == null)
                return expr;

            return ParseBinaryExpression(expr, op, Precedence.None);
        }

        Expr ParseBinaryExpression(Expr left, BinaryOp op, Precedence minPrecedence)
        {
            while (op != null && op.Precedence >= minPrecedence)
            {
                // Advance past the operator.
                Advance();

                // Parse the right-hand operand.
                var right = ParseUnary();

                // If the next token is a binary operator of higher precedence than the
                // current operator, recursively parse it and let the right-hand expression
                // be the result.
                var nextOp = GetBinaryOp(m_lexer.TokenSymbol);
                while (nextOp != null && nextOp.Precedence > op.Precedence)
                {
                    right = ParseBinaryExpression(right, nextOp, nextOp.Precedence);
                    nextOp = GetBinaryOp(m_lexer.TokenSymbol);
                }

                // Replace the left-hand expression with the binary expression.
                left = new FunctionExpr(op, left, right);

                op = nextOp;
            }
            return left;
        }

        static BinaryOp GetBinaryOp(SymbolId id)
        {
            foreach (var op in FunctionExpr.BinaryOperators)
            {
                if (op.Symbol == id)
                {
                    return op;
                }
            }
            return null;
        }

        static Function GetUnaryPrefixOp(SymbolId id)
        {
            switch (id)
            {
                case SymbolId.Minus: return (double[] args) => -args[0];
                default: return null;
            }
        }

        // unary -> Number
        //          Identifier
        //          Identifier '(' expr ( ',' expr )* ')'
        //          '(' expr ')'
        //          '-' unary
        Expr ParseUnary()
        {
            if (m_lexer.TokenType == TokenType.Number)
            {
                var expr = new ConstExpr(m_lexer.TokenValue);
                Advance();
                return expr;
            }
            else if (m_lexer.TokenType == TokenType.Identifier)
            {
                string name = m_lexer.TokenString;
                Advance();

                if (m_lexer.TokenSymbol == SymbolId.LeftParen)
                {
                    Advance();

                    // It's a function
                    var args = new List<Expr>();

                    args.Add(ParseExpression());

                    while (m_lexer.TokenSymbol == SymbolId.Comma)
                    {
                        Advance();
                        args.Add(ParseExpression());
                    }

                    if (m_lexer.TokenSymbol != SymbolId.RightParen)
                        throw new ParseException(m_lexer, "Expected ).");

                    Advance();

                    return CreateFunctionExpression(name, args);
                }
                else
                {
                    // Is it a variable?
                    for (int i = 0; i < m_varNames.Length; i++)
                    {
                        if (m_varNames[i] == name)
                        {
                            return new VariableExpr(i);
                        }
                    }

                    // Is it a named constant?
                    ConstExpr constExpr;
                    if (ConstExpr.NameConstants.TryGetValue(name, out constExpr))
                    {
                        return constExpr;
                    }

                    var b = new StringBuilder();
                    b.AppendFormat("Variable or constant {0} not defined. Named constants are:", name);
                    foreach (var s in ConstExpr.NameConstants.Keys)
                    {
                        b.AppendFormat("\n - {0}", s);
                    }
                    throw new ParseException(m_lexer, b.ToString());
                }
            }
            else if (m_lexer.TokenSymbol == SymbolId.LeftParen)
            {
                Advance();

                var expr = ParseExpression();

                if (m_lexer.TokenSymbol != SymbolId.RightParen)
                    throw new ParseException(m_lexer, "Expected ).");

                Advance();
                return expr;
            }
            else
            {
                var op = GetUnaryPrefixOp(m_lexer.TokenSymbol);
                if (op == null)
                    throw new ParseException(m_lexer, "Invalid expression.");

                Advance();
                var expr = ParseUnary();
                return new FunctionExpr(op, Precedence.UnaryPrefix, new Expr[] { expr });
            }
        }

        Expr CreateFunctionExpression(string name, List<Expr> args)
        {
            foreach (var func in FunctionExpr.Functions)
            {
                if (func.Name == name)
                {
                    if (func.ParamCount != args.Count)
                    {
                        throw new ParseException(m_lexer, $"{func.ParamCount} arguments expected for {name}().");
                    }

                    return new FunctionExpr(func.Func, Precedence.Atomic, args);
                }
            }

            // Build the error message string.
            var b = new StringBuilder();
            b.AppendFormat("Unknown function: {0}. Known functions are:", name);
            foreach (var func in FunctionExpr.Functions)
            {
                b.AppendFormat("\n - {0}", func.Name);
            }
            throw new ParseException(m_lexer, b.ToString());
        }
    }
}
