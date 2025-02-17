using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI;

namespace GraphEq
{
    public class FormulaViewModel : INotifyPropertyChanged
    {
        public static readonly Expr EmptyExpression = Constants.NaN;

        // Formulas always have one variable named "x".
        static readonly string[] m_varNames = new string[] { "x" };

        // Functions that may be referenced in a formula.
        FunctionsViewModel m_userFunctions;

        public FormulaViewModel(FunctionsViewModel userFunctions, Color color)
        {
            m_userFunctions = userFunctions;

            // Reparse the expression if the functions change.
            m_userFunctions.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(m_userFunctions.Functions))
                {
                    ParseExpression();
                }
            };

            this.Color = color;
        }

        public static readonly Color[] AllColors = new Color[]
        {
            Color.FromArgb(0xFF, 0xFF, 0, 0),       // red
            Color.FromArgb(0xFF, 0x00, 0x23, 0xF5), // blue
            Color.FromArgb(0xFF, 0x00, 0x7F, 0),    // green
            Color.FromArgb(0xFF, 0xF0, 0x9B, 0x59), // orange
            Color.FromArgb(0xFF, 0x73, 0x2B, 0xF5), // purple
            Color.FromArgb(0xFF, 0xEA, 0x3F, 0xF7), // lavender
            Color.FromArgb(0xFF, 0x00, 0x0C, 0x7B), // dark blue
            Color.FromArgb(0xFF, 0x7E, 0x84, 0xF7), // light blue
        };

        // Color property.
        public Windows.UI.Color Color { get; }

        // Text property.
        string m_text = string.Empty;
        public string Text
        {
            get => m_text;

            set
            {
                if (value != m_text)
                {
                    m_text = value;
                    ParseExpression();
                    OnPropertyChanged();
                }
            }
        }

        // Expression property.
        Expr m_expression = EmptyExpression;
        public Expr Expression
        {
            get => m_expression;

            private set
            {
                if (!value.IsEquivalent(m_expression))
                {
                    m_expression = value;
                    OnPropertyChanged();
                }
            }
        }

        // Error property.
        ParseError m_error = Parser.NoError;
        public ParseError Error
        {
            get => m_error;

            private set
            {
                if (value != m_error)
                {
                    m_error = value;
                    OnPropertyChanged();
                }
            }
        }

        // Called when the Text or user functions change to reparse the text
        // and set the Expression and Error properties.
        void ParseExpression()
        {
            // Special case for empty expression.
            if (string.IsNullOrWhiteSpace(m_text))
            {
                this.Expression = EmptyExpression;
                this.Error = Parser.NoError;
                return;
            }

            try
            {
                this.Expression = Parser.ParseFormula(
                    m_text,
                    m_userFunctions.Functions,
                    m_varNames
                    );
                this.Error = Parser.NoError;
            }
            catch (ParseException e)
            {
                this.Expression = EmptyExpression;
                this.Error = e.Error;
            }
        }

        void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
