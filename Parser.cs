﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

    internal class Parser
    {
        Lexer m_lexer = new Lexer();
        Dictionary<string, FunctionDef> m_userFunctions;
        IList<string> m_varNames;
        int m_lineNumber = 0;
        string m_functionName = null;

        public Parser()
        {
        }

        public Dictionary<string, FunctionDef> ParseFunctionDefs(string input)
        {
            m_userFunctions = new Dictionary<string, FunctionDef>();
            m_varNames = new List<string>();
            m_lineNumber = 0;
            m_functionName = null;

            foreach (string line in input.Split('\r'))
            {
                ++m_lineNumber;
                m_functionName = null;
                m_varNames.Clear();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                m_lexer.InputString = line;
                CheckLexerError();

                // Parse the function name.
                if (m_lexer.TokenType != TokenType.Identifier)
                {
                    throw new ParseException(m_lexer, "Function name expected.");
                }
                m_functionName = m_lexer.TokenString;
                if (FunctionExpr.Functions.ContainsKey(m_functionName) || m_userFunctions.ContainsKey(m_functionName))
                {
                    throw new ParseException(m_lexer, $"Function {m_functionName} is already defined.");
                }
                Advance();

                // Parse the '('
                if (m_lexer.TokenSymbol != SymbolId.LeftParen)
                {
                    throw new ParseException(m_lexer, "'(' expected.");
                }
                Advance();

                // Parse the parameter names.
                if (m_lexer.TokenType == TokenType.Identifier)
                {
                    m_varNames.Add(m_lexer.TokenString);
                    Advance();

                    while (m_lexer.TokenSymbol == SymbolId.Comma)
                    {
                        Advance();
                        if (m_lexer.TokenType != TokenType.Identifier)
                        {
                            throw new ParseException(m_lexer, "Name expected after ','.");
                        }
                        m_varNames.Add(m_lexer.TokenString);
                        Advance();
                    }
                }

                // Parse the ')'
                if (m_lexer.TokenSymbol != SymbolId.RightParen)
                {
                    throw new ParseException(m_lexer, "')' expected.");
                }
                Advance();

                // Parse the '='
                if (m_lexer.TokenSymbol != SymbolId.Equals)
                {
                    throw new ParseException(m_lexer, "'=' expected.");
                }
                Advance();

                // Parse the expression.
                var expr = ParseExpr();

                if (m_lexer.TokenType != TokenType.None)
                    throw new ParseException(m_lexer, "Invalid expression.");

                // Add the function.
                expr = expr.Simplify();
                Function fn = (double[] paramValues) => expr.Eval(paramValues);
                m_userFunctions.Add(m_functionName, new FunctionDef("", m_varNames.Count, fn));
            }

            return m_userFunctions;
        }

        public int LineNumber => m_lineNumber;
        public string FunctionName => m_functionName;

        public Expr ParseExpression(string input, Dictionary<string, FunctionDef> userFunctions, string[] varNames)
        {
            m_lexer.InputString = input;
            CheckLexerError();

            m_userFunctions = userFunctions;
            m_varNames = varNames;
            m_lineNumber = 0;
            m_functionName = null;

            var expr = ParseExpr();

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
        Expr ParseExpr()
        {
            if (!m_lexer.HaveToken)
                throw new ParseException(m_lexer, "Expected expression.");

            var expr = ParseUnaryExpr();

            var op = GetBinaryOp(m_lexer.TokenSymbol);
            if (op == null)
                return expr;

            return ParseBinaryExpr(expr, op, Precedence.None);
        }

        Expr ParseBinaryExpr(Expr left, BinaryOp op, Precedence minPrecedence)
        {
            while (op != null && op.Precedence >= minPrecedence)
            {
                // Advance past the operator.
                Advance();

                // Parse the right-hand operand.
                var right = ParseUnaryExpr();

                // If the next token is a binary operator of higher precedence than the
                // current operator, recursively parse it and let the right-hand expression
                // be the result.
                var nextOp = GetBinaryOp(m_lexer.TokenSymbol);
                while (nextOp != null && nextOp.Precedence > op.Precedence)
                {
                    right = ParseBinaryExpr(right, nextOp, nextOp.Precedence);
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
        Expr ParseUnaryExpr()
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

                    args.Add(ParseExpr());

                    while (m_lexer.TokenSymbol == SymbolId.Comma)
                    {
                        Advance();
                        args.Add(ParseExpr());
                    }

                    if (m_lexer.TokenSymbol != SymbolId.RightParen)
                        throw new ParseException(m_lexer, "Expected ).");

                    Advance();

                    return CreateFunctionExpression(name, args);
                }
                else
                {
                    // Is it a variable?
                    for (int i = 0; i < m_varNames.Count; i++)
                    {
                        if (m_varNames[i] == name)
                        {
                            return new VariableExpr(i);
                        }
                    }

                    // Is it a named constant?
                    ConstExpr constExpr;
                    if (ConstExpr.NamedConstants.TryGetValue(name, out constExpr))
                    {
                        return constExpr;
                    }
                    throw new ParseException(m_lexer, $"Undefined variable or constant: {name}.");
                }
            }
            else if (m_lexer.TokenSymbol == SymbolId.LeftParen)
            {
                Advance();

                var expr = ParseExpr();

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
                var expr = ParseUnaryExpr();
                return new FunctionExpr(op, Precedence.UnaryPrefix, new Expr[] { expr });
            }
        }

        Expr CreateFunctionExpression(string name, List<Expr> args)
        {
            FunctionDef func;
            if (FunctionExpr.Functions.TryGetValue(name, out func) ||
                m_userFunctions.TryGetValue(name, out func))
            {
                if (func.ParamCount != args.Count)
                {
                    throw new ParseException(m_lexer, $"{func.ParamCount} arguments expected for {name}().");
                }

                return new FunctionExpr(func.Func, Precedence.Atomic, args);
            }
            throw new ParseException(m_lexer, $"Unknown function: {name}.");
        }
    }
}
