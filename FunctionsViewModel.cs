using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GraphEq
{
	public class FunctionsViewModel : INotifyPropertyChanged
	{
        public static readonly Dictionary<string, UserFunctionDef> EmptyFunctions = new Dictionary<string, UserFunctionDef>();
		public static readonly List<ParseError> NoErrors = new List<ParseError>();
			
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
					ParseFunctions();
                    OnPropertyChanged();
                }
            }
		}

        // Error property.
        List<ParseError> m_errors = NoErrors;
        public List<ParseError> Errors
        {
            get => m_errors;

            private set
            {
				if (!m_errors.SequenceEqual(value))
                {
                    m_errors = value;
                    OnPropertyChanged();
                }
            }
        }

		// Functions property.
        Dictionary<string, UserFunctionDef> m_userFunctions = EmptyFunctions;
        public Dictionary<string, UserFunctionDef> Functions
		{
			get => m_userFunctions;

			private set
			{
                m_userFunctions = value;
				OnPropertyChanged();
            }
        }

		// Called when Text changes to parse the parse the text and set
		// the Functions and Errors properties.
		void ParseFunctions()
		{
			var functions = new Dictionary<string, UserFunctionDef>();
			var errors = Parser.ParseFunctionDefs(Text, functions);

			this.Errors = errors.Count == 0 ? NoErrors : errors;
			this.Functions = functions;
        }

        void OnPropertyChanged([CallerMemberName] string name = "")
		{
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
