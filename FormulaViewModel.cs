using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GraphEq
{
    public class FormulaViewModel : INotifyPropertyChanged
    {
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
