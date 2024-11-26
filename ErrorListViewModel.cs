using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            ObservableCollection<FormulaViewModel> formulas
            )
        {
            Functions = userFunctions;
            Formulas = formulas;

            Functions.PropertyChanged += Functions_PropertyChanged;

            Formulas.CollectionChanged += Formulas_CollectionChanged;

            foreach (var formula in formulas)
            {
                formula.PropertyChanged += Formula_PropertyChanged;
            }
        }

        private void Formulas_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    ((FormulaViewModel)item).PropertyChanged -= Formula_PropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    ((FormulaViewModel)item).PropertyChanged += Formula_PropertyChanged;
                }
            }
            Invalidate();
        }

        private void Functions_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FunctionsViewModel.Errors))
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
        ObservableCollection<FormulaViewModel> Formulas { get; }

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

            foreach (var error in Functions.Errors)
            {
                string heading = string.IsNullOrEmpty(error.FunctionName) ?
                    "Error in user defined function" :
                    $"Error in function: {error.FunctionName}";
                errors.Add(new ErrorItem(heading, FormatMessage(error)));
            }

            for (int i = 0; i < Formulas.Count; i++)
            {
                var error = Formulas[i].Error;
                if (error != Parser.NoError)
                {
                    string heading = $"Error in formula {i + 1}";
                    errors.Add(new ErrorItem(heading, FormatMessage(error)));
                }
            }

            m_isValid = true;
            this.Errors = errors;
            this.Visibility = errors.Count != 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        static string FormatMessage(ParseError error)
        {
            return error.LineNumber != 0 ?
                $"Error: line {error.LineNumber}, column {error.ColumnNumber}: {error.Message}" :
                $"Error: column {error.ColumnNumber}: {error.Message}";
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
