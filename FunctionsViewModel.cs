using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GraphEq
{
	record FunctionsError(string FunctionName, string Message);

	class FunctionsViewModel : INotifyPropertyChanged
	{
        public static readonly Dictionary<string, UserFunctionDef> EmptyFunctions = new Dictionary<string, UserFunctionDef>();
		public static readonly FunctionsError EmptyError = new FunctionsError("", "");

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
        FunctionsError m_error = EmptyError;
        public FunctionsError Error
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
		// the Functions and Error property.
		void ParseFunctions()
		{
			var parser = new Parser();

			try
			{
                this.Functions = parser.ParseFunctionDefs(Text);
                this.Error = EmptyError;
			}
			catch (ParseException e)
			{
				this.Functions = EmptyFunctions;
				this.Error = new FunctionsError(
					parser.FunctionName,
                    $"Error: line {parser.LineNumber} column {parser.ColumnNumber}: {e.Message}"
                    );
            }
        }

		void OnPropertyChanged([CallerMemberName] string name = "")
		{
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
