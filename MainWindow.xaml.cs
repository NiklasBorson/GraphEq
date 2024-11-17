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

        // Scale factor and origin for the canvas.
        float m_scale;
        Vector2 m_origin;

        // Use a hard-coded function for test purposes.
        MathFn m_fn = (double x) => 1.0 / x;

        // Device-dependent resources.
        AxisRenderer m_axisRenderer;

        public MainWindow()
        {
            this.InitializeComponent();
        }
        
        private void CanvasControl_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var delta = e.Delta;

            m_scale *= delta.Scale;
            m_origin += delta.Translation.ToVector2();

            (sender as CanvasControl)?.Invalidate();
        }

        private void SetDefaultTransform(CanvasControl sender)
        {
            m_scale = 50;
            m_origin = new Vector2(
                (float)sender.ActualWidth / 2,
                (float)sender.ActualHeight / 2
                );
        }

        private void CanvasControl_CreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            m_axisRenderer?.Dispose();
            m_axisRenderer = new AxisRenderer(sender);
        }

        private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (m_scale == 0)
            {
                SetDefaultTransform(sender);
            }

            args.DrawingSession.Clear(BackColor);

            // Draw the axes.
            m_axisRenderer.DrawAxes(args.DrawingSession, sender, m_scale, m_origin);

            // Draw the curve.
            if (m_fn != null)
            {
                CurveBuilder.Draw(
                    args.DrawingSession,
                    sender,
                    m_fn,
                    m_scale,
                    m_origin,
                    (float)sender.ActualWidth
                    );
            }
        }
    }
}
