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

        // Scale factor and origin for the canvas.
        float m_scale;
        Vector2 m_origin;

        // Parser and expression state.
        struct Formula
        {
            public Expr m_expr;
            public string m_errorMessage;
        }
        Dictionary<string, FunctionDef> m_userFunctions = new Dictionary<string, FunctionDef>();
        Parser m_parser = new Parser();
        string[] m_varNames = new string[] { "x" };
        Formula m_formula;
        Formula m_formula2;
        string m_userFunctionsErrorMessage = null;
        bool m_haveUserFunctionsChanged = false;

        // Device-dependent resources.
        AxisRenderer m_axisRenderer;
        CanvasTextFormat m_errorHeadingFormat;
        CanvasTextFormat m_errorTextFormat;

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
                m_origin += delta.Translation.ToVector2();

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

        private void SetDefaultTransform()
        {
            m_scale = 50;
            m_origin = new Vector2(
                (float)this.Canvas.ActualWidth / 2,
                (float)this.Canvas.ActualHeight / 2
                );
        }

        private CanvasTextFormat MakeErrorTextFormat(ushort fontWeight)
        {
            var textFormat = new CanvasTextFormat();
            textFormat.FontSize = 16;
            textFormat.FontFamily = "Segoe UI";
            textFormat.FontWeight = new FontWeight(fontWeight);
            return textFormat;
        }

        private void CanvasControl_CreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            m_axisRenderer?.Dispose();
            m_axisRenderer = new AxisRenderer(sender);

            m_errorHeadingFormat?.Dispose();
            m_errorHeadingFormat = MakeErrorTextFormat(700);

            m_errorTextFormat?.Dispose();
            m_errorTextFormat = MakeErrorTextFormat(400);
        }

        float DrawErrorText(CanvasDrawingSession drawingSession, float y, string message, CanvasTextFormat textFormat)
        {
            const float margin = 10;
            float formatWidth = (float)Canvas.ActualWidth - (margin * 2);

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
                float y = 80;
                if (m_userFunctionsErrorMessage != null)
                {
                    y += DrawErrorText(args.DrawingSession, y, "Error in user function", m_errorHeadingFormat);
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
                // Lazily initialize the transform the first time we draw a graph.
                if (m_scale == 0)
                {
                    SetDefaultTransform();
                }

                // Draw the axes.
                m_axisRenderer.DrawAxes(args.DrawingSession, sender, m_scale, m_origin);

                // Draw the curves for each formula.
                DrawFormula(args.DrawingSession, m_formula.m_expr, CurveColor);
                DrawFormula(args.DrawingSession, m_formula2.m_expr, Curve2Color);
            }
        }

        void DrawFormula(CanvasDrawingSession drawingSession, Expr expr, Windows.UI.Color color)
        {
            if (expr != null)
            {
                CurveBuilder.Draw(
                    drawingSession,
                    Canvas,
                    expr,
                    m_scale,
                    m_origin,
                    (float)Canvas.ActualWidth,
                    (float)Canvas.ActualHeight,
                    color
                    );
            }
        }

        string HelpText
        {
            get
            {
                var b = new StringBuilder();
                b.Append(
                    "Operators:\n" +
                    " +  Plus\n" +
                    " -  Minus\n" +
                    " *  Multiply\n" +
                    " /  Divide\n" +
                    " ^  Power\n" +
                    "\n" +
                    "Intrinsic functions:"
                    );
                foreach (var funcDef in FunctionExpr.Functions.Values)
                {
                    b.AppendFormat("\n \x2022 {0}", funcDef.Signature);
                }
                b.Append("\n\nConstants:");
                foreach (var s in ConstExpr.NamedConstants.Keys)
                {
                    b.AppendFormat("\n \x2022 {0}", s);
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

        void SetUserFunctionsError(string errorMessage)
        {
            if (errorMessage != m_userFunctionsErrorMessage)
            {
                m_userFunctionsErrorMessage = errorMessage;

                if (errorMessage != null)
                {
                    m_formula.m_errorMessage = null;
                    m_formula2.m_errorMessage = null;
                }

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

            try
            {
                string input = textBox.Text;
                if (!string.IsNullOrWhiteSpace(input))
                {
                    SetExpression(m_parser.ParseExpression(input, m_userFunctions, m_varNames), ref formula);
                }
                else
                {
                    SetExpression(null, ref formula);
                }
            }
            catch (ParseException x)
            {
                SetFormulaError($"Error: column {x.InputPos}: {x.Message}", ref formula);
            }
        }

        void ReparseUserFunctions()
        {
            try
            {
                m_userFunctions = m_parser.ParseFunctionDefs(UserFunctionsTextBox.Text);

                SetUserFunctionsError(null);
                ReparseFormula(FormulaTextBox, ref m_formula);
                ReparseFormula(Formula2TextBox, ref m_formula2);
            }
            catch (ParseException x)
            {
                m_userFunctions = null;
                if (!string.IsNullOrEmpty(m_parser.FunctionName))
                {
                    SetUserFunctionsError($"Error in {m_parser.FunctionName}.\nError: line {m_parser.LineNumber}, column {x.InputPos}: {x.Message}");
                }
                else
                {
                    SetUserFunctionsError($"Error: line {m_parser.LineNumber}: {x.Message}");
                }
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
            get => new Vector2(
                m_origin.X / (float)Canvas.ActualWidth,
                m_origin.Y / (float)Canvas.ActualHeight
                );

            set
            {
                var origin = new Vector2(
                    value.X * (float)Canvas.ActualWidth,
                    value.Y * (float)Canvas.ActualHeight
                    );
                if (origin != m_origin)
                {
                    m_origin = origin;
                    Canvas.Invalidate();
                }
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
            SetDefaultTransform();
            Canvas.Invalidate();
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
