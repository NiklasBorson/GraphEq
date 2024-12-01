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
            this.Formulas = new ObservableCollection<FormulaViewModel>();
            this.AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;

            this.InitializeComponent();
        }

        internal ObservableCollection<FormulaViewModel> Formulas { get; }
    }
}
