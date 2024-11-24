using Microsoft.UI.Xaml;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Numerics;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
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
        static readonly Windows.UI.Color BackColor = Windows.UI.Color.FromArgb(255, 220, 220, 220);
        static readonly Windows.UI.Color ErrorMessageColor = Windows.UI.Color.FromArgb(255, 128, 0, 0);
        static readonly Windows.UI.Color CurveColor = Windows.UI.Color.FromArgb(255, 255, 0, 0);
        static readonly Windows.UI.Color Curve2Color = Windows.UI.Color.FromArgb(255, 0, 0, 255);

        // Scale factor for the canvas.
        float m_scale = 50;

        // The origin is stored relative to the canvas size.
        Vector2 m_relativeOrigin = new Vector2(0.5f, 0.5f);
        Vector2 m_canvasSize;

        // Parser and expression state.
        struct Formula
        {
            public Expr m_expr;
            public string m_errorMessage;
        }
        Dictionary<string, UserFunctionDef> m_userFunctions = new Dictionary<string, UserFunctionDef>();
        string[] m_varNames = new string[] { "x" };
        Formula m_formula;
        Formula m_formula2;
        string m_userFunctionErrorHeading = null;
        string m_userFunctionsErrorMessage = null;
        bool m_haveUserFunctionsChanged = false;

        // Text formats for error text (not device-dependent).
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

        public MainWindow()
        {
            this.AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
            this.InitializeComponent();
        }

        bool HaveError => m_formula.m_errorMessage != null || m_formula2.m_errorMessage != null || m_userFunctionsErrorMessage != null;

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

        private void CanvasControl_CreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            m_axisRenderer?.Dispose();
            m_axisRenderer = new AxisRenderer(sender);
        }

        float DrawErrorText(CanvasDrawingSession drawingSession, float y, string message, CanvasTextFormat textFormat)
        {
            const float margin = 20;
            const float minWidth = 80;
            float formatWidth = float.Max(minWidth, CanvasSize.X - (margin * 2));

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
                if (m_userFunctionsErrorMessage != null)
                {
                    y += DrawErrorText(args.DrawingSession, y, m_userFunctionErrorHeading, m_errorHeadingFormat);
                    y += DrawErrorText(args.DrawingSession, y, m_userFunctionsErrorMessage, m_errorTextFormat);
                    y += paraGap;
                }

                if (m_formula.m_errorMessage != null)
                {
                    y += DrawErrorText(args.DrawingSession, y, "Error in first formula", m_errorHeadingFormat);
                    y += DrawErrorText(args.DrawingSession, y, m_formula.m_errorMessage, m_errorTextFormat);
                    y += paraGap;
                }

                if (m_formula2.m_errorMessage != null)
                {
                    y += DrawErrorText(args.DrawingSession, y, "Error in second formula", m_errorHeadingFormat);
                    y += DrawErrorText(args.DrawingSession, y, m_formula2.m_errorMessage, m_errorTextFormat);
                }
            }
            else
            {
                // Draw the axes.
                m_axisRenderer.DrawAxes(args.DrawingSession, sender, m_scale, PixelOrigin);

                // Draw the curves for each formula.
                DrawFormula(args.DrawingSession, m_formula.m_expr, CurveColor);
                DrawFormula(args.DrawingSession, m_formula2.m_expr, Curve2Color);
            }
        }

        void DrawFormula(CanvasDrawingSession drawingSession, Expr expr, Windows.UI.Color color)
        {
            if (expr != null)
            {
                using (var geometry = CurveBuilder.CreateGeometry(
                    Canvas,
                    expr,
                    m_scale,
                    PixelOrigin,
                    CanvasSize.ToSize()
                    ))
                {
                    drawingSession.DrawGeometry(geometry, new Vector2(), color, 2.0f);
                }
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

        void SetExpression(Expr newExpr, ref Formula formula)
        {
            if (formula.m_errorMessage == null)
            {
                // Do nothing if the expressions are equivalent.
                if (object.ReferenceEquals(newExpr, formula.m_expr))
                {
                    return;
                }
                if (newExpr != null && newExpr.IsEquivalent(formula.m_expr))
                {
                    return;
                }
            }
            else
            {
                formula.m_errorMessage = null;
            }

            formula.m_expr = newExpr;

            this.Canvas.Invalidate();
        }

        void SetFormulaError(string errorMessage, ref Formula formula)
        {
            if (errorMessage != formula.m_errorMessage)
            {
                formula.m_expr = null;
                formula.m_errorMessage = errorMessage;

                this.Canvas.Invalidate();
            }
        }

        void SetUserFunctionsError(string errorHeading, string errorMessage)
        {
            if (errorHeading != m_userFunctionErrorHeading ||
                errorMessage != m_userFunctionsErrorMessage)
            {
                m_userFunctionErrorHeading = errorHeading;
                m_userFunctionsErrorMessage = errorMessage;

                m_formula.m_errorMessage = null;
                m_formula2.m_errorMessage = null;

                this.Canvas.Invalidate();
            }
        }

        void ClearUserFunctionsError()
        {
            if (m_userFunctionsErrorMessage != null)
            {
                m_userFunctionErrorHeading = null;
                m_userFunctionsErrorMessage = null;
                this.Canvas.Invalidate();
            }
        }

        void ReparseFormula(TextBox textBox, ref Formula formula)
        {
            // Don't try parsing if we don't have user functions.
            if (m_userFunctions == null)
            {
                SetFormulaError(null, ref formula);
                return;
            }

            var parser = new Parser();

            try
            {
                string input = textBox.Text;
                if (!string.IsNullOrWhiteSpace(input))
                {
                    SetExpression(parser.ParseExpression(input, m_userFunctions, m_varNames), ref formula);
                }
                else
                {
                    SetExpression(null, ref formula);
                }
            }
            catch (ParseException x)
            {
                SetFormulaError($"Error: column {parser.ColumnNumber}: {x.Message}", ref formula);
            }
        }

        void ReparseUserFunctions()
        {
            var parser = new Parser();

            try
            {
                m_userFunctions = parser.ParseFunctionDefs(UserFunctionsTextBox.Text);
                ClearUserFunctionsError();

                ReparseFormula(FormulaTextBox, ref m_formula);
                ReparseFormula(Formula2TextBox, ref m_formula2);
            }
            catch (ParseException x)
            {
                m_userFunctions = null;

                string heading = string.IsNullOrEmpty(parser.FunctionName) ?
                    "Error in user function" :
                    $"Error in function: {parser.FunctionName}";

                string message = $"Error: line {parser.LineNumber} column {parser.ColumnNumber}: {x.Message}";

                SetUserFunctionsError(heading, message);
            }
        }

        private void Formula_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            ReparseFormula(FormulaTextBox, ref m_formula);
        }

        private void Formula2_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            ReparseFormula(Formula2TextBox, ref m_formula2);
        }

        private void UserFunctionsTextBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            m_haveUserFunctionsChanged = true;
            ReparseUserFunctions();
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            m_canvasSize = new Vector2((float)Canvas.ActualWidth, (float)Canvas.ActualHeight);
        }

        public Vector2 CanvasSize => m_canvasSize;

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
            get => RelativeOrigin * CanvasSize;

            set
            {
                RelativeOrigin = value / CanvasSize;
            }
        }

        public string FormulaText
        {
            get => FormulaTextBox.Text;

            set
            {
                FormulaTextBox.Text = value;
                ReparseFormula(FormulaTextBox, ref m_formula);
            }
        }

        public string Formula2Text
        {
            get => Formula2TextBox.Text;

            set
            {
                Formula2TextBox.Text = value;
                ReparseFormula(Formula2TextBox, ref m_formula2);
            }
        }

        public string UserFunctions
        {
            get => UserFunctionsTextBox.Text;

            set
            {
                UserFunctionsTextBox.Text = value;
                ReparseUserFunctions();
            }
        }

        public bool HaveUserFunctionsChanged => m_haveUserFunctionsChanged;

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
    }
}
