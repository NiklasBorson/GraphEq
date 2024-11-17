using System;
using System.Collections.Generic;

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

    internal class Parser
    {
        Lexer m_lexer = new Lexer();
        TryGetVariable m_tryGetVariable;

        public Parser(TryGetVariable tryGetVariable)
        {
            m_tryGetVariable = tryGetVariable;
        }

        public Expr Parse(string input)
        {
            m_lexer.InputString = input;
            CheckLexerError();

            var expr = ParseExpression();

            if (m_lexer.TokenType != TokenType.None)
                throw new ParseException(m_lexer, "Unexpected token.");

            return expr;
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
                left = new BinaryExpr(left, right, op);
                op = nextOp;
            }
            return left;
        }

        static BinaryOp GetBinaryOp(SymbolId id)
        {
            switch (id)
            {
                case SymbolId.Plus: return BinaryExpr.AddOp;
                case SymbolId.Minus: return BinaryExpr.SubtractOp;
                case SymbolId.Multiply: return BinaryExpr.MultiplyOp;
                case SymbolId.Divide: return BinaryExpr.DivideOp;
                case SymbolId.Caret: return BinaryExpr.PowerOp;
                default: return null;
            }
        }

        static UnaryOp GetUnaryPrefixOp(SymbolId id)
        {
            switch (id)
            {
                case SymbolId.Minus: return UnaryExpr.NegativeOp;
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
                    VariableExpr varExpr = m_tryGetVariable(name);
                    if (varExpr != null)
                        return varExpr;

                    // Is it a named constant?
                    foreach (var namedConst in NamedConstExpr.Constants)
                    {
                        if (namedConst.Name == name)
                        {
                            return namedConst;
                        }
                    }

                    throw new ParseException(m_lexer, $"Variable or constant {name} not defined.");
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
                    throw new ParseException(m_lexer, "Syntax error.");

                Advance();
                var expr = ParseUnary();
                return new UnaryExpr(expr, op);
            }
        }

        Expr CreateFunctionExpression(string name, List<Expr> args)
        {
            foreach (var func in UnaryFunctionExpr.Functions)
            {
                if (func.Key == name)
                {
                    if (args.Count != 1)
                    {
                        throw new ParseException(m_lexer, $"One argument expected for {name}().");
                    }

                    return new UnaryFunctionExpr(name, args[0], func.Value);
                }
            }

            throw new ParseException(m_lexer, $"Unknown function: {name}.");
        }
    }
}
