using System;

namespace GraphEq
{
    enum TokenType
    {
        Error = -1,
        None = 0,
        Number,
        Identifier,
        Symbol
    }

    enum SymbolId
    {
        None,
        Plus,
        Minus,
        Multiply,
        Divide,
        Percent,
        Caret,
        LeftParen,
        RightParen,
        Comma,
        Equals,
        BoolOr,
        BoolAnd,
        BoolNot,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        NotEqual,
        QuestionMark,
        Colon
    }

    struct Lexer
    {
        // Input string and position.
        string m_input = string.Empty;
        int m_matchPos = 0;
        int m_matchLength = 0;

        // Properties of the current token.
        TokenType m_tokenType = TokenType.None;
        SymbolId m_symbolId = SymbolId.None;
        double m_value = double.NaN;

        // Getters for the current token.
        public bool HaveToken => m_tokenType > TokenType.None;
        public bool HaveError => m_tokenType == TokenType.Error;
        public TokenType TokenType => m_tokenType;
        public SymbolId TokenSymbol => m_symbolId;
        public double TokenValue => m_value;
        public int TokenPos => m_matchPos;
        public string TokenString => m_input.Substring(m_matchPos, m_matchLength);
        public ReadOnlySpan<char> TokenSpan => m_input.AsSpan().Slice(m_matchPos, m_matchLength);

        public Lexer()
        {
        }

        // Sets the input and advances to the first token.
        public void SetInput(string input, int startPos)
        {
            m_input = input;
            m_matchPos = startPos;
            m_matchLength = 0;
            Advance();
        }

        // Advance to the first or next token in the input string.
        public void Advance()
        {
            // Reset the current token.
            m_tokenType = TokenType.None;
            m_symbolId = SymbolId.None;
            m_value = double.NaN;

            // Advance to the next non-whitespace character after the current match.
            m_matchPos = SkipSpaces(m_input, m_matchPos + m_matchLength);
            m_matchLength = 0;

            if (m_matchPos == m_input.Length)
            {
                return;
            }

            // Get the first two characters of the token.
            char ch = m_input[m_matchPos];
            char next = m_matchPos + 1 < m_input.Length ? m_input[m_matchPos + 1] : '\0';

            if (IsNumberStart(ch, next))
            {
                m_matchLength = FindNumberEnd(m_input, m_matchPos) - m_matchPos;

                if (double.TryParse(TokenSpan, out m_value))
                {
                    m_tokenType = TokenType.Number;
                }
                else
                {
                    m_tokenType = TokenType.Error;
                }
            }
            else if (IsNameChar(ch))
            {
                m_matchLength = FindNameEnd(m_input, m_matchPos) - m_matchPos;
                m_tokenType = TokenType.Identifier;
            }
            else
            {
                var (id, length) = TryMatchSymbol(ch, next);
                if (length > 0)
                {
                    // Symbol token
                    m_matchLength = length;
                    m_symbolId = id;
                    m_tokenType = TokenType.Symbol;
                }
                else if (ch == '\r' || ch == '\n' || ch == ';')
                {
                    // End of line or comment to end of line
                    m_tokenType = TokenType.None;
                }
                else
                {
                    // Invalid token
                    m_tokenType = TokenType.Error;
                }
            }
        }

        static bool IsSpaceOrTab(char ch) => ch == ' ' || ch == '\t';

        static int SkipSpaces(string input, int pos)
        {
            while (pos < input.Length && IsSpaceOrTab(input[pos]))
            {
                ++pos;
            }
            return pos;
        }

        static bool IsNameStartChar(char ch) => char.IsLetter(ch) || ch == '_';
        static bool IsNameChar(char ch) => char.IsLetterOrDigit(ch) || ch == '_';

        static int FindNameEnd(string input, int pos)
        {
            while (pos < input.Length && IsNameChar(input[pos]))
            {
                ++pos;
            }
            return pos;
        }

        static bool IsNumberStart(char ch, char next) => char.IsAsciiDigit(ch) || (ch == '.' && char.IsAsciiDigit(next));

        static int SkipDigits(string input, int pos)
        {
            while (pos < input.Length && char.IsAsciiDigit(input[pos]))
            {
                ++pos;
            }
            return pos;
        }

        static int FindNumberEnd(string input, int startPos)
        {
            // Advance past leading digits.
            int pos = SkipDigits(input, startPos);

            // Advance past decimal point and subsequent digits.
            if (pos + 1 < input.Length && input[pos] == '.' && char.IsAsciiDigit(input[pos + 1]))
            {
                pos = SkipDigits(input, pos + 2);
            }

            // Advance past [Ee][+-]?[0-9]+
            if (pos + 1 < input.Length && (input[pos] == 'e' || input[pos] == 'E'))
            {
                int i = pos + 1;
                if (input[i] == '+' || input[i] == '-')
                {
                    ++i;
                }
                if (i < input.Length && char.IsAsciiDigit(input[i]))
                {
                    pos = SkipDigits(input, i + 1);
                }
            }
            return pos;
        }

        static (SymbolId, int) TryMatchSymbol(char ch, char next)
        {
            switch (ch)
            {
                case '+': return (SymbolId.Plus, 1);
                case '-': return (SymbolId.Minus, 1);
                case '*': return (SymbolId.Multiply, 1);
                case '/': return (SymbolId.Divide, 1);
                case '%': return (SymbolId.Percent, 1);
                case '^': return (SymbolId.Caret, 1);
                case '(': return (SymbolId.LeftParen, 1);
                case ')': return (SymbolId.RightParen, 1);
                case ',': return (SymbolId.Comma, 1);
                case '=': return (SymbolId.Equals, 1);
                case '|': return next == '|' ? (SymbolId.BoolOr, 2) : (SymbolId.None, 0);
                case '&': return next == '&' ? (SymbolId.BoolAnd, 2) : (SymbolId.None, 0);
                case '!': return next == '=' ? (SymbolId.NotEqual, 2) : (SymbolId.BoolNot, 1);
                case '<': return next == '=' ? (SymbolId.LessThanOrEqual, 2) : (SymbolId.LessThan, 1);
                case '>': return next == '=' ? (SymbolId.GreaterThanOrEqual, 2) : (SymbolId.GreaterThan, 1);
                case '?': return (SymbolId.QuestionMark, 1);
                case ':': return (SymbolId.Colon, 1);
            }
            return (SymbolId.None, 0);
        }
    }
}
