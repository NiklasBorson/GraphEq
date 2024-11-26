using Microsoft.UI.Xaml;
using System.Numerics;
using System.Collections.ObjectModel;
using Microsoft.UI.Windowing;
using System;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GraphEq
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.UserFunctions = new FunctionsViewModel();
            this.Formulas = new ObservableCollection<FormulaViewModel>();
            this.ErrorList = new ErrorListViewModel(UserFunctions, Formulas);
            this.AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;

            this.InitializeComponent();
        }

        internal FunctionsViewModel UserFunctions { get; }
        internal ObservableCollection<FormulaViewModel> Formulas { get; }
        internal ErrorListViewModel ErrorList { get; }

        internal float GraphScale
        {
            get => Graph.GraphScale;
            set { Graph.GraphScale = value; }
        }

        internal Vector2 RelativeOrigin
        {
            get => Graph.RelativeOrigin;
            set { Graph.RelativeOrigin = value; }
        }

        static readonly uint[] m_formulaColors = new uint[]
        {
            0xFF0000,   // red
            0x0023F5,   // blue
            0x007F00,   // green
            0xF09B59,   // orange
            0x732BF5,   // purple
            0xEA3FF7,   // lavender
            0x000C7B,   // dark blue
            0x7E84F7,   // light blue
        };

        public static uint ColorToUint(Windows.UI.Color color)
        {
            return ((uint)(color.R) << 16) |
                ((uint)(color.G) << 8) |
                ((uint)color.B);
        }

        public static Windows.UI.Color ColorFromUint(uint value)
        {
            return Windows.UI.Color.FromArgb(
                0xFF,
                (byte)(value >> 16),
                (byte)(value >> 8),
                (byte)(value)
            );
        }

        private void AddFormula_Click(object sender, RoutedEventArgs e)
        {
            AddFormula();
        }

        private void RemoveFormula_Click(object sender, RoutedEventArgs e)
        {
            var formula = (sender as Button)?.DataContext as FormulaViewModel;
            int i = Formulas.IndexOf(formula);
            if (i >= 0)
            {
                Formulas.RemoveAt(i);

                AddFormulaButton.ClearValue(Button.VisibilityProperty);
            }
        }

        public void AddFormula()
        {
            var usedColors = new uint[Formulas.Count];
            for (int i = 0; i < usedColors.Length; i++)
            {
                usedColors[i] = ColorToUint(Formulas[i].Color);
            }

            var newColor = m_formulaColors[0];
            foreach (var color in m_formulaColors)
            {
                if (Array.IndexOf(usedColors, color) < 0)
                {
                    newColor = color;
                    break;
                }
            }

            AddFormula(string.Empty, newColor);
        }

        public void AddFormula(string text, uint color)
        {
            int maxCount = m_formulaColors.Length;
            if (Formulas.Count < maxCount)
            {
                var formula = new FormulaViewModel(UserFunctions, ColorFromUint(color));

                if (!string.IsNullOrEmpty(text))
                {
                    formula.Text = text;
                }

                Formulas.Add(formula);

                if (Formulas.Count == maxCount)
                {
                    AddFormulaButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void CenterButton_Click(object sender, RoutedEventArgs e)
        {
            Graph.RelativeOrigin = new Vector2(0.5f, 0.5f);
        }

        private void DefaultScaleButton_Click(object sender, RoutedEventArgs e)
        {
            Graph.GraphScale = GraphControl.DefaultScale;
        }

        private void OpenSidePanel_Click(object sender, RoutedEventArgs e)
        {
            SidePanel.Visibility = Visibility.Visible;
            SidePanelOpenAnimation.Begin();
        }

        private void CloseSidePanel_Click(object sender, RoutedEventArgs e)
        {
            SidePanelCloseAnimation.Begin();
        }

        private void TabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (object.ReferenceEquals(((TabView) sender).SelectedItem, HelpItem))
            {
                var blocks = HelpControl.Blocks;
                if (blocks.Count == 0)
                {
                    HelpBuilder.InitializeHelp(blocks);
                }
            }
        }
    }
}
