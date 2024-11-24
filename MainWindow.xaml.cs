using Microsoft.UI.Xaml;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Numerics;
using System.Text;
using System.Collections.Generic;
using Microsoft.UI.Windowing;
using Microsoft.Graphics.Canvas;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GraphEq
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        #region Fields
        static readonly Windows.UI.Color BackColor = Windows.UI.Color.FromArgb(255, 220, 220, 220);
        static readonly Windows.UI.Color ErrorMessageColor = Windows.UI.Color.FromArgb(255, 128, 0, 0);

        // Canvas scale factor and origin.
        float m_scale = 50;
        Vector2 m_relativeOrigin = new Vector2(0.5f, 0.5f);
        Vector2 m_canvasSize;

        // Device-dependent resources.
        AxisRenderer m_axisRenderer;
        #endregion

        public MainWindow()
        {
            this.UserFunctions = new FunctionsViewModel();
            this.Formulas = new FormulaViewModel[]
            {
                new FormulaViewModel(UserFunctions, Windows.UI.Color.FromArgb(255, 255, 0, 0)),
                new FormulaViewModel(UserFunctions, Windows.UI.Color.FromArgb(255, 0, 0, 255))
            };

            this.ErrorList = new ErrorListViewModel(UserFunctions, Formulas);
            this.AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;

            this.InitializeComponent();

            UserFunctions.PropertyChanged += Functions_PropertyChanged;

            foreach (var formula in Formulas)
            {
                formula.PropertyChanged += Formula_PropertyChanged;
            }
        }

        #region Properties
        internal FunctionsViewModel UserFunctions { get; }
        internal IList<FormulaViewModel> Formulas { get; }
        internal ErrorListViewModel ErrorList { get; }

        public float Scale
        {
            get => m_scale;

            set
            {
                if (value > 0 && value != m_scale)
                {
                    m_scale = value;
                    Canvas.Invalidate();
                }
            }
        }

        public Vector2 RelativeOrigin
        {
            get => m_relativeOrigin;

            set
            {
                if (m_relativeOrigin != value)
                {
                    m_relativeOrigin = value;
                    Canvas.Invalidate();
                }
            }
        }

        public Vector2 PixelOrigin
        {
            get => RelativeOrigin * m_canvasSize;

            set
            {
                RelativeOrigin = value / m_canvasSize;
            }
        }

        string HelpText
        {
            get
            {
                var b = new StringBuilder();
                b.Append(
                    "An expression may use any of the operators and functions listed " +
                    "below, as well as user-defined functions in the My Functions tab. " +
                    "Any expression may be followed by a where clause as in the " +
                    "following example:\n" +
                    "\n" +
                    "    (x + 1) / x, where x > 0\n" +
                    "\n" +
                    "Unary operators:\n" +
                    " -   Negative\n" +
                    " !   Logical NOT\n" +
                    "\n" +
                    "Binary operators:\n"
                    );
                foreach (var op in BinaryOps.Operators.Values)
                {
                    b.Append(op.Description);
                    b.Append('\n');
                }
                b.Append(
                    "\n" +
                    "Ternary operator:\n" +
                    " a ? b : c\n" +
                    "     Returns b if a is true.\n" +
                    "     Otherwise returns c.\n" +
                    "\n" +
                    "Intrinsic functions:\n"
                    );
                foreach (var funcDef in FunctionDefs.Functions.Values)
                {
                    b.AppendFormat(" \x2022 {0}\n", funcDef.Signature);
                }
                b.Append(
                    "\n" +
                    "Boolean values:\n" +
                    " \x2022 True = 1.\n" +
                    " \x2022 False = NaN.\n" +
                    " \x2022 Any nonzero real number evaluates as True.\n" +
                    " \x2022 NaN, 0, inf, and -inf evaluate as False.\n" +
                    "\n" +
                    "Constants:\n"
                    );
                foreach (var s in Constants.NamedConstants.Keys)
                {
                    b.AppendFormat(" \x2022 {0}\n", s);
                }
                return b.ToString();
            }
        }
        #endregion

        #region Event handlers

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            m_canvasSize = new Vector2((float)Canvas.ActualWidth, (float)Canvas.ActualHeight);
        }

        private void CenterButton_Click(object sender, RoutedEventArgs e)
        {
            RelativeOrigin = new Vector2(0.5f, 0.5f);
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

        private void Functions_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Repaint if the Error property changes.
            if (e.PropertyName == nameof(FunctionsViewModel.Errors))
            {
                Canvas.Invalidate();
            }
        }

        private void Formula_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Repaint if the Expression or Error proeprty changes.
            if (e.PropertyName == nameof(FormulaViewModel.Error) ||
                e.PropertyName == nameof(FormulaViewModel.Expression))
            {
                Canvas.Invalidate();
            }
        }

        private void CanvasControl_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var delta = e.Delta;

            m_scale *= delta.Scale;
            PixelOrigin += delta.Translation.ToVector2();

            this.Canvas.Invalidate();
        }

        private void CanvasControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);

            if (ctrl.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
            {
                var delta = e.GetCurrentPoint(Canvas).Properties.MouseWheelDelta;

                m_scale *= float.Pow(2.0f, delta * 0.001f);

                this.Canvas.Invalidate();
            }
        }
        #endregion

        #region Canvas rendering
        private void CanvasControl_CreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            m_axisRenderer?.Dispose();
            m_axisRenderer = new AxisRenderer(sender);
        }

        private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            args.DrawingSession.Clear(BackColor);

            // Draw the axes.
            m_axisRenderer.DrawAxes(args.DrawingSession, sender, m_scale, PixelOrigin);

            // Draw the curves for each formula.
            foreach (var formula in Formulas)
            {
                if (formula.Expression != FormulaViewModel.EmptyExpression)
                {
                    using (var geometry = CurveBuilder.CreateGeometry(
                        Canvas,
                        formula.Expression,
                        m_scale,
                        PixelOrigin,
                        m_canvasSize.ToSize()
                        ))
                    {
                        args.DrawingSession.DrawGeometry(
                            geometry, 
                            new Vector2(), 
                            formula.Color, 
                            2.0f
                            );
                    }
                }
            }
        }
        #endregion
    }
}
