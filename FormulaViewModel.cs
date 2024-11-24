using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace GraphEq
{
    class FormulaViewModel : INotifyPropertyChanged
    {
        public static readonly Expr EmptyExpression = Constants.NaN;

        static readonly string[] m_varNames = new string[] { "x" };
        FunctionsViewModel m_userFunctions;
        string m_text = string.Empty;
        Expr m_expression = EmptyExpression;
        string m_error = string.Empty;

        public FormulaViewModel(FunctionsViewModel userFunctions)
        {
            m_userFunctions = userFunctions;

            m_userFunctions.PropertyChanged += (object sender, PropertyChangedEventArgs e) =>
            {
                if (e.PropertyName == nameof(m_userFunctions.Functions))
                {
                    ParseExpression();
                }
            };
        }

        public string Text
        {
            get => m_text;

            set
            {
                if (value != m_text)
                {
                    m_text = value;
                    OnPropertyChanged();
                    ParseExpression();
                }
            }
        }

        public Expr Expression
        {
            get => m_expression;

            private set
            {
                if (value.IsEquivalent(m_expression))
                {
                    m_expression = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Error
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

        void ParseExpression()
        {
            if (string.IsNullOrWhiteSpace(m_text))
            {
                this.Expression = EmptyExpression;
                this.Error = string.Empty;
            }
            else
            {
                var parser = new Parser();

                try
                {
                    this.Expression = parser.ParseExpression(
                        m_text,
                        m_userFunctions.Functions,
                        m_varNames
                        );
                    this.Error = string.Empty;
                }
                catch (ParseException e)
                {
                    this.Expression = EmptyExpression;
                    this.Error = $"Error: column {parser.ColumnNumber}: {e.Message}";
                }
            }
        }

        void OnPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
