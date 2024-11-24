using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GraphEq
{
    class FormulaViewModel : INotifyPropertyChanged
    {
        public static readonly Expr EmptyExpression = Constants.NaN;

        // Formulas always have one variable named "x".
        static readonly string[] m_varNames = new string[] { "x" };

        // Functions that may be referenced in a formula.
        FunctionsViewModel m_userFunctions;

        public FormulaViewModel(FunctionsViewModel userFunctions, Windows.UI.Color color)
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
                this.Expression = Parser.ParseExpression(
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
