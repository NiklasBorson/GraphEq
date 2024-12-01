using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GraphEq
{
    public class FormulaViewModel : INotifyPropertyChanged
    {
        public FormulaViewModel(Windows.UI.Color color)
        {
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
