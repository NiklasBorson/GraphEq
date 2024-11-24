using Microsoft.UI.Xaml;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Numerics;
using System.Text;
using Microsoft.UI.Windowing;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas;
using Windows.UI.Text;

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
        static readonly Windows.UI.Color CurveColor = Windows.UI.Color.FromArgb(255, 255, 0, 0);
        static readonly Windows.UI.Color Curve2Color = Windows.UI.Color.FromArgb(255, 0, 0, 255);

        // Canvas scale factor and origin.
        float m_scale = 50;
        Vector2 m_relativeOrigin = new Vector2(0.5f, 0.5f);
        Vector2 m_canvasSize;

        // Parser and expression state (initialized by ctor).
        FunctionsViewModel m_functions;
        FormulaViewModel m_formula1;
        FormulaViewModel m_formula2;

        // Text formats for error messages (not device-dependent).
        CanvasTextFormat m_errorHeadingFormat = new CanvasTextFormat
        {
            FontSize = 16,
            FontFamily = "Segoe UI",
            FontWeight = new FontWeight(700),
            WordWrapping = CanvasWordWrapping.Wrap
        };
        CanvasTextFormat m_errorTextFormat = new CanvasTextFormat
        {
            FontSize = 16,
            FontFamily = "Segoe UI",
            FontWeight = new FontWeight(400),
            WordWrapping = CanvasWordWrapping.Wrap
        };

        // Device-dependent resources.
        AxisRenderer m_axisRenderer;
        #endregion

        public MainWindow()
        {
            m_functions = new FunctionsViewModel();
            m_formula1 = new FormulaViewModel(m_functions);
            m_formula2 = new FormulaViewModel(m_functions);

            this.AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            this.InitializeComponent();

            m_functions.PropertyChanged += Functions_PropertyChanged;
            m_formula1.PropertyChanged += Formula_PropertyChanged;
            m_formula2.PropertyChanged += Formula_PropertyChanged;
        }

        #region Properties
        internal FunctionsViewModel UserFunctions => m_functions;
        internal FormulaViewModel Formula1 => m_formula1;
        internal FormulaViewModel Formula2 => m_formula2;

        bool HaveError =>
            m_functions.Error != FunctionsViewModel.EmptyError ||
            m_formula1.Error != string.Empty ||
            m_formula2.Error != string.Empty;

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
            if (e.PropertyName == nameof(FunctionsViewModel.Error))
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
            if (!HaveError)
            {
                var delta = e.Delta;

                m_scale *= delta.Scale;
                PixelOrigin += delta.Translation.ToVector2();

                this.Canvas.Invalidate();
            }
        }

        private void CanvasControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);

            if (ctrl.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down) && !HaveError)
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

        float DrawErrorText(CanvasDrawingSession drawingSession, float y, string message, CanvasTextFormat textFormat)
        {
            const float margin = 20;
            const float minWidth = 80;
            float formatWidth = float.Max(minWidth, m_canvasSize.X - (margin * 2));

            using (var textLayout = new CanvasTextLayout(Canvas, message, textFormat, formatWidth, 0))
            {
                drawingSession.DrawTextLayout(textLayout, margin, y, ErrorMessageColor);
                return (float)textLayout.LayoutBounds.Height;
            }
        }

        private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            args.DrawingSession.Clear(BackColor);

            if (HaveError)
            {
                const float paraGap = 10;
                float y = 100;
                if (m_functions.Error != FunctionsViewModel.EmptyError)
                {
                    var funcName = m_functions.Error.FunctionName;
                    string heading = string.IsNullOrEmpty(funcName) ?
                        "Error in user defined function" :
                        $"Error in function: {funcName}";

                    y += DrawErrorText(args.DrawingSession, y, heading, m_errorHeadingFormat);
                    y += DrawErrorText(args.DrawingSession, y, m_functions.Error.Message, m_errorTextFormat);
                    y += paraGap;
                }

                if (!string.IsNullOrEmpty(m_formula1.Error))
                {
                    y += DrawErrorText(args.DrawingSession, y, "Error in first formula", m_errorHeadingFormat);
                    y += DrawErrorText(args.DrawingSession, y, m_formula1.Error, m_errorTextFormat);
                    y += paraGap;
                }

                if (!string.IsNullOrEmpty(m_formula2.Error))
                {
                    y += DrawErrorText(args.DrawingSession, y, "Error in second formula", m_errorHeadingFormat);
                    y += DrawErrorText(args.DrawingSession, y, m_formula2.Error, m_errorTextFormat);
                }
            }
            else
            {
                // Draw the axes.
                m_axisRenderer.DrawAxes(args.DrawingSession, sender, m_scale, PixelOrigin);

                // Draw the curves for each formula.
                DrawFormula(args.DrawingSession, m_formula1.Expression, CurveColor);
                DrawFormula(args.DrawingSession, m_formula2.Expression, Curve2Color);
            }
        }

        void DrawFormula(CanvasDrawingSession drawingSession, Expr expr, Windows.UI.Color color)
        {
            if (expr != FormulaViewModel.EmptyExpression)
            {
                using (var geometry = CurveBuilder.CreateGeometry(
                    Canvas,
                    expr,
                    m_scale,
                    PixelOrigin,
                    m_canvasSize.ToSize()
                    ))
                {
                    drawingSession.DrawGeometry(geometry, new Vector2(), color, 2.0f);
                }
            }
        }
        #endregion
    }
}
