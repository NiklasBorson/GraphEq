﻿using System;
using System.Collections.Generic;

namespace GraphEq
{
    delegate VariableExpr TryGetVariable(string name);

    class ParseException : Exception
    {
        public ParseException(string message) : base(message)
        {
        }
    }

    struct Parser
    {
        Lexer m_lexer = new Lexer();
        Dictionary<string, UserFunctionDef> m_userFunctions;
        IList<string> m_varNames;
        int m_lineNumber = 0;
        int m_lineStartPos = 0;
        string m_functionName = null;

        public Parser()
        {
        }

        static int FindNextLine(string input, int linePos)
        {
            for (int i = linePos; i < input.Length; i++)
            {
                switch (input[i])
                {
                    case '\r':
                        return (i + 1 < input.Length && input[i + 1] == '\n') ? i + 2 : i + 1;

                    case '\n':
                        return i + 1;
                }
            }
            return input.Length;
        }

        public Dictionary<string, UserFunctionDef> ParseFunctionDefs(string input)
        {
            m_userFunctions = new Dictionary<string, UserFunctionDef>();
            m_varNames = new List<string>();
            m_lineNumber = 0;
            m_functionName = null;

            for (m_lineStartPos = 0; m_lineStartPos < input.Length; m_lineStartPos = FindNextLine(input, m_lexer.TokenPos))
            {
                ++m_lineNumber;
                m_functionName = null;
                m_varNames.Clear();

                // Set the lexer input, trimming any comment.
                m_lexer.SetInput(input, m_lineStartPos);
                CheckLexerError();

                // Skip blank lilne.
                if (m_lexer.TokenType == TokenType.None)
                {
                    continue;
                }

                // Parse the function name.
                if (m_lexer.TokenType != TokenType.Identifier)
                {
                    throw new ParseException("Function name expected.");
                }
                m_functionName = m_lexer.TokenString;
                if (FunctionDefs.Functions.ContainsKey(m_functionName) || m_userFunctions.ContainsKey(m_functionName))
                {
                    throw new ParseException($"Function {m_functionName} is already defined.");
                }
                Advance();

                // Parse the '('
                if (m_lexer.TokenSymbol != SymbolId.LeftParen)
                {
                    throw new ParseException("'(' expected.");
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
                            throw new ParseException("Name expected after ','.");
                        }
                        m_varNames.Add(m_lexer.TokenString);
                        Advance();
                    }
                }

                // Parse the ')'
                if (m_lexer.TokenSymbol != SymbolId.RightParen)
                {
                    throw new ParseException("')' expected.");
                }
                Advance();

                // Parse the '='
                if (m_lexer.TokenSymbol != SymbolId.Equals)
                {
                    throw new ParseException("'=' expected.");
                }
                Advance();

                // Parse the expression.
                var expr = ParseFullExpression();

                // Add the function.
                m_userFunctions.Add(m_functionName, new UserFunctionDef(m_varNames.Count, expr));
            }

            return m_userFunctions;
        }

        public int LineNumber => m_lineNumber;
        public int ColumnNumber => (m_lexer.TokenPos - m_lineStartPos) + 1;
        public string FunctionName => m_functionName;

        public Expr ParseExpression(string input, Dictionary<string, UserFunctionDef> userFunctions, string[] varNames)
        {
            m_lexer.SetInput(input, 0);
            CheckLexerError();

            m_userFunctions = userFunctions;
            m_varNames = varNames;
            m_lineNumber = 0;
            m_lineStartPos = 0;
            m_functionName = null;

            return ParseFullExpression();
        }

        void CheckLexerError()
        {
            if (m_lexer.HaveError)
            {
                throw new ParseException("Invalid token.");
            }
        }

        void Advance()
        {
            m_lexer.Advance();
            CheckLexerError();
        }

        Expr ParseFullExpression()
        {
            var expr = ParseExpr();

            if (m_lexer.TokenSymbol == SymbolId.Comma)
            {
                m_lexer.Advance();
                if (m_lexer.TokenType == TokenType.Identifier && m_lexer.TokenString == "where")
                {
                    m_lexer.Advance();
                    var condition = ParseExpr();
                    expr = new TernaryExpr(condition, expr, Constants.NaN);
                }
                else
                {
                    throw new ParseException("Expected where after ','.");
                }
            }

            if (m_lexer.TokenType != TokenType.None)
            {
                throw new ParseException("Invalid expression.");
            }

            return expr.Simplify();
        }

        Expr ParseExpr()
        {
            var expr = ParseBinaryExpr();

            // Is it a ternary expression: expr ? first : second
            if (m_lexer.TokenSymbol == SymbolId.QuestionMark)
            {
                // Advance past the '?'.
                Advance();

                // Parse the first subexpression.
                var first = ParseBinaryExpr();

                // Advacne past the ':'.
                if (m_lexer.TokenSymbol != SymbolId.Colon)
                {
                    throw new ParseException("Expected ':'.");
                }
                Advance();

                // Parse the second subexpression.
                var second = ParseExpr();

                // Create the ternary expression.
                expr = new TernaryExpr(expr, first, second);
            }

            return expr;
        }

        // expr -> unary (BinOp expr)*
        Expr ParseBinaryExpr()
        {
            if (!m_lexer.HaveToken)
            {
                throw new ParseException("Expected expression.");
            }

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
                left = new BinaryExpr(op.Op, left, right);

                op = nextOp;
            }
            return left;
        }

        static BinaryOp GetBinaryOp(SymbolId id)
        {
            BinaryOp op;
            return BinaryOps.Operators.TryGetValue(id, out op) ? op : null;
        }

        static UnaryExpr.Op GetUnaryPrefixOp(SymbolId id)
        {
            switch (id)
            {
                case SymbolId.Minus: return UnaryOps.UnaryMinus;
                case SymbolId.BoolNot: return UnaryOps.UnaryNot;
            }
            return null;
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
                        throw new ParseException("Expected ).");

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
                    if (Constants.NamedConstants.TryGetValue(name, out constExpr))
                    {
                        return constExpr;
                    }
                    throw new ParseException($"Undefined variable or constant: {name}.");
                }
            }
            else if (m_lexer.TokenSymbol == SymbolId.LeftParen)
            {
                Advance();

                var expr = ParseExpr();

                if (m_lexer.TokenSymbol != SymbolId.RightParen)
                    throw new ParseException("Expected ).");

                Advance();
                return expr;
            }
            else
            {
                var op = GetUnaryPrefixOp(m_lexer.TokenSymbol);
                if (op == null)
                    throw new ParseException("Invalid expression.");

                Advance();
                return new UnaryExpr(op, ParseUnaryExpr());
            }
        }

        Expr CreateFunctionExpression(string name, List<Expr> args)
        {
            var expr = TryCreateIntrinsicFunction(name, args);
            if (expr != null)
            {
                return expr;
            }


            expr = TryCreateUserFunction(name, args);
            if (expr != null)
            {
                return expr;
            }

            throw new ParseException($"Unknown function: {name}.");
        }

        Expr TryCreateIntrinsicFunction(string name, List<Expr> args)
        {
            FunctionDef def;
            if (!FunctionDefs.Functions.TryGetValue(name, out def))
            {
                return null;
            }

            if (def.ParamCount != args.Count)
            {
                throw new ParseException($"{def.ParamCount} arguments expected for {name}().");
            }

            return new FunctionExpr(def.Op, args);
        }

        Expr TryCreateUserFunction(string name, List<Expr> args)
        {
            UserFunctionDef def;
            if (!m_userFunctions.TryGetValue(name, out def))
            {
                return null;
            }

            if (def.ParamCount != args.Count)
            {
                throw new ParseException($"{def.ParamCount} arguments expected for {name}().");
            }

            return new UserFunctionExpr(def.Body, args);
        }
    }
}
