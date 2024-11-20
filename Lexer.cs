using System.Text.RegularExpressions;

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

    internal class Lexer
    {
        // Input string and position.
        string m_input = string.Empty;
        Match m_match = Match.Empty;
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
        public int TokenPos => m_match.Index;
        public string TokenString => m_input.Substring(m_matchPos, m_matchLength);

        // Input string property.
        public string InputString
        {
            get
            {
                return m_input;
            }

            set
            {
                m_input = value;
                m_match = m_pattern.Match(m_input);
                InitializeToken();
            }
        }

        // Advance to the first or next token in the input string.
        public void Advance()
        {
            m_match = m_match.NextMatch();
            InitializeToken();
        }

        const string m_numberRegex = @"[0-9]*\.?[0-9]+(?:[Ee][+-]?[0-9]+)?";
        const string m_identifierRegex = @"[A-Za-z_][A-Za-z_0-9]*";
        const string m_symbolRegex = @"[+\-*/^(),=?:]|[!<>]=?|\|\||&&";

        // Regular expression that matches one token.
        // Each capture group cooresponds to a token type.
        static readonly Regex m_pattern = new Regex(
            " *(?:" +
            $"({m_numberRegex})" +          // 1 -> number
            $"|({m_identifierRegex})" +     // 2 -> identifier
            $"|({m_symbolRegex})" +         // 3 -> symbol
            "|($)" +                        // 4 -> end of input
            ")"
            );

        void InitializeToken()
        {
            // Clear the token properties.
            m_tokenType = TokenType.None;
            m_symbolId = SymbolId.None;
            m_value = double.NaN;

            // Initialize the match position to the end of the previous token.
            m_matchPos += m_matchLength;
            m_matchLength = 0;

            if (m_match.Index > m_matchPos)
            {
                // Unexpected characters between matches.
                m_tokenType = TokenType.Error;
                m_matchLength = m_match.Index - m_matchPos;
            }
            else if (IsGroupMatch(1, TokenType.Number))
            {
                // Number.
                if (!double.TryParse(this.TokenString, out m_value))
                {
                    m_tokenType = TokenType.Error;
                }
            }
            else if (IsGroupMatch(2, TokenType.Identifier))
            {
                // Identifier.
            }
            else if (IsGroupMatch(3, TokenType.Symbol))
            {
                // Symbol.
                m_symbolId = CharToSymbol(
                    m_input[m_matchPos],
                    m_matchLength > 1 ? m_input[m_matchPos + 1] : '\0'
                    );
            }
            else if (IsGroupMatch(4, TokenType.None))
            {
                // End of input.
            }
            else
            {
                // No match.
                m_tokenType = TokenType.Error;
                m_matchPos = m_match.Index;
                m_matchLength = 0;
            }
        }

        bool IsGroupMatch(int groupIndex, TokenType tokenType)
        {
            var group = m_match.Groups[groupIndex];
            if (group.Success)
            {
                m_tokenType = tokenType;
                m_matchPos = group.Index;
                m_matchLength = group.Length;
                return true;
            }
            else
            {
                return false;
            }
        }

        static SymbolId CharToSymbol(char ch, char ch2)
        {
            switch (ch)
            {
                case '+': return SymbolId.Plus;
                case '-': return SymbolId.Minus;
                case '*': return SymbolId.Multiply;
                case '/': return SymbolId.Divide;
                case '^': return SymbolId.Caret;
                case '(': return SymbolId.LeftParen;
                case ')': return SymbolId.RightParen;
                case ',': return SymbolId.Comma;
                case '=': return SymbolId.Equals;
                case '|': return SymbolId.BoolOr;
                case '&': return SymbolId.BoolAnd;
                case '!': return ch2 == '=' ? SymbolId.NotEqual : SymbolId.BoolNot;
                case '<': return ch2 == '=' ? SymbolId.LessThanOrEqual : SymbolId.LessThan;
                case '>': return ch2 == '=' ? SymbolId.GreaterThanOrEqual : SymbolId.GreaterThan;
                case '?': return SymbolId.QuestionMark;
                case ':': return SymbolId.Colon;
                default: return SymbolId.None;
            }
        }
    }
}
