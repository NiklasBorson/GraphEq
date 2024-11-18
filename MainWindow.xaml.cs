using Microsoft.UI.Xaml;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Numerics;

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
        ExpressionParser m_parser = new ExpressionParser();
        string[] m_varNames = new string[] { "x" };
        Expr m_expr = null;
        string m_errorMessage = null;

        // Device-dependent resources.
        AxisRenderer m_axisRenderer;

        public MainWindow()
        {
            this.InitializeComponent();
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
                    (float)sender.ActualWidth
                    );
            }
            else if (m_errorMessage != null)
            {
                args.DrawingSession.DrawText(m_errorMessage, 20, 80, ErrorMessaegColor);
            }
        }

        void SetExpression(Expr expr)
        {
            if (object.ReferenceEquals(expr, m_expr))
            {
                return;
            }

            if (expr != null && expr.IsEquivalent(m_expr))
            {
                return;
            }

            m_expr = expr;
            m_errorMessage = null;

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

        private void TextBox_TextChanged(object sender, Microsoft.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            try
            {
                string input = FormulaTextBox.Text;
                if (!string.IsNullOrWhiteSpace(input))
                {
                    SetExpression(m_parser.Parse(input, m_varNames));
                }
                else
                {
                    SetExpression(null);
                }
            }
            catch (ParseException x)
            {
                SetErrorMessage(x.Message);
            }
        }
    }
}
