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
       
		string m_text = string.Empty;
        Dictionary<string, UserFunctionDef> m_userFunctions = EmptyFunctions;
		FunctionsError m_error = EmptyError;

        public FunctionsViewModel()
		{
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
					ParseUserFunctions();
				}
			}
		}

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

        public Dictionary<string, UserFunctionDef> Functions
		{
			get => m_userFunctions;

			private set
			{
                if (!IsEquivalent(value, m_userFunctions))
                {
                    m_userFunctions = value;
					OnPropertyChanged();
                }
            }
        }

		static bool IsEquivalent(
			Dictionary<string, UserFunctionDef> a,
            Dictionary<string, UserFunctionDef> b
			)
		{
			if (a.Count != b.Count)
			{
				return false;
			}

			foreach (var item in a)
			{
				UserFunctionDef funcB;
				if (!b.TryGetValue(item.Key, out funcB))
				{
					return false;
				}

				if (!item.Value.Body.IsEquivalent(funcB.Body))
				{
					return false;
				}
			}

			return true;
		}

		void ParseUserFunctions()
		{
			var parser = new Parser();

			try
			{
				var functions = parser.ParseFunctionDefs(Text);

                this.Functions = functions;
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
