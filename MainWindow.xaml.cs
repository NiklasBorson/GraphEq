using System;
using System.Numerics;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;

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
            var usedColors = new Color[Formulas.Count];
            for (int i = 0; i < usedColors.Length; i++)
            {
                usedColors[i] = Formulas[i].Color;
            }

            var newColor = FormulaViewModel.AllColors[0];
            foreach (var color in FormulaViewModel.AllColors)
            {
                if (Array.IndexOf(usedColors, color) < 0)
                {
                    newColor = color;
                    break;
                }
            }

            AddFormula(string.Empty, newColor);
        }

        public void AddFormula(string text, Color color)
        {
            int maxCount = FormulaViewModel.AllColors.Length;
            if (Formulas.Count < maxCount)
            {
                var formula = new FormulaViewModel(UserFunctions, color);

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
