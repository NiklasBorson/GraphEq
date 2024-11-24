using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GraphEq
{
    record ErrorItem(string Heading, string Message);

    internal class ErrorListViewModel : INotifyPropertyChanged
    {
        bool m_isValid = true;
        Visibility m_visibility = Visibility.Collapsed;
        List<ErrorItem> m_errors = new List<ErrorItem>();

        public ErrorListViewModel(
            FunctionsViewModel userFunctions,
            IList<FormulaViewModel> formulas
            )
        {
            Functions = userFunctions;
            Formulas = formulas;

            Functions.PropertyChanged += Functions_PropertyChanged;

            foreach (var formula in formulas)
            {
                formula.PropertyChanged += Formula_PropertyChanged;
            }
        }

        private void Functions_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FunctionsViewModel.Error))
            {
                Invalidate();
            }
        }

        private void Formula_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FormulaViewModel.Error))
            {
                Invalidate();
            }
        }

        FunctionsViewModel Functions { get; }
        IList<FormulaViewModel> Formulas { get; }

        void Invalidate()
        {
            if (m_isValid)
            {
                DispatcherQueue.GetForCurrentThread().TryEnqueue(
                    () => { Update(); }
                    );
                m_isValid = false;
            };
        }

        void Update()
        {
            var errors = new List<ErrorItem>();

            if (Functions.Error != FunctionsViewModel.EmptyError)
            {
                var funcName = Functions.Error.FunctionName;
                string heading = string.IsNullOrEmpty(funcName) ?
                    "Error in user defined function" :
                    $"Error in function: {funcName}";
                errors.Add(new ErrorItem(heading, Functions.Error.Message));
            }

            for (int i = 0; i < Formulas.Count; i++)
            {
                var error = Formulas[i].Error;
                if (!string.IsNullOrEmpty(error))
                {
                    string heading = $"Error in formula {i + 1}";
                    errors.Add(new ErrorItem(heading, error));
                }
            }

            m_isValid = true;
            this.Errors = errors;
            this.Visibility = errors.Count != 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public List<ErrorItem> Errors
        {
            get => m_errors;

            set
            {
                if (!m_errors.SequenceEqual(value))
                {
                    m_errors = value;
                    OnPropertyChanged();
                }
            }
        }

        public Visibility Visibility
        {
            get => m_visibility;

            private set
            {
                if (value != m_visibility)
                {
                    m_visibility = value;
                    OnPropertyChanged();
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
