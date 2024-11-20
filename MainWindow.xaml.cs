using Microsoft.UI.Xaml;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Numerics;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;
using System.Text;

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
        static readonly Windows.UI.Color ErrorMessaegColor = Windows.UI.Color.FromArgb(255, 128, 0, 0);

        // Scale factor and origin for the canvas.
        float m_scale;
        Vector2 m_origin;

        // Parser and expression state.
        Dictionary<string, FunctionDef> m_userFunctions = new Dictionary<string, FunctionDef>();
        Parser m_parser = new Parser();
        string[] m_varNames = new string[] { "x" };
        Expr m_expr = null;
        string m_errorMessage = null;

        // Device-dependent resources.
        AxisRenderer m_axisRenderer;

        public MainWindow()
        {
            this.InitializeComponent();
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

        private void CanvasControl_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (m_expr != null)
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

            if (ctrl.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
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

        private void CanvasControl_CreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            m_axisRenderer?.Dispose();
            m_axisRenderer = new AxisRenderer(sender);
        }

        private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            args.DrawingSession.Clear(BackColor);

            if (m_expr != null)
            {
                // Lazily initialize the transform the first time we draw a graph.
                if (m_scale == 0)
                {
                    SetDefaultTransform();
                }

                // Draw the axes.
                m_axisRenderer.DrawAxes(args.DrawingSession, sender, m_scale, m_origin);

                CurveBuilder.Draw(
                    args.DrawingSession,
                    sender,
                    m_expr,
                    m_scale,
                    m_origin,
                    (float)sender.ActualWidth,
                    (float)sender.ActualHeight
                    );
            }
            else if (m_errorMessage != null)
            {
                args.DrawingSession.DrawText(m_errorMessage, 20, 80, ErrorMessaegColor);
            }
        }

        void SetExpression(Expr expr)
        {
            if (m_errorMessage == null)
            {
                if (object.ReferenceEquals(expr, m_expr))
                {
                    return;
                }

                if (expr != null && expr.IsEquivalent(m_expr))
                {
                    return;
                }
            }
            else
            {
                m_errorMessage = null;
            }

            m_expr = expr;

            this.Canvas.Invalidate();
        }

        void SetErrorMessage(string errorMessage)
        {
            if (errorMessage == m_errorMessage)
            {
                return;
            }

            m_expr = null;
            m_errorMessage = errorMessage;

            this.Canvas.Invalidate();
        }

        void ReparseFormula()
        {
            try
            {
                if (m_userFunctions != null)
                {
                    string input = FormulaTextBox.Text;
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        SetExpression(m_parser.ParseExpression(input, m_userFunctions, m_varNames));
                    }
                    else
                    {
                        SetExpression(null);
                    }
                }
            }
            catch (ParseException x)
            {
                SetErrorMessage($"Error in formula column {x.InputPos}:\n{x.Message}");
            }
        }

        private void TextBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            ReparseFormula();
        }

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


        private void UserFunctionsTextBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            try
            {
                m_userFunctions = m_parser.ParseFunctionDefs(UserFunctionsTextBox.Text);
                ReparseFormula();
            }
            catch (ParseException x)
            {
                m_userFunctions = null;
                if (!string.IsNullOrEmpty(m_parser.FunctionName))
                {
                    SetErrorMessage($"Error in {m_parser.FunctionName} function line {m_parser.LineNumber}, column {x.InputPos}:\n{x.Message}");
                }
                else
                {
                    SetErrorMessage($"Error in function definitions line {m_parser.LineNumber}:\n{x.Message}");
                }
            }
        }
    }
}
