using Microsoft.UI.Xaml;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using System.Collections.Generic;

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
        static readonly Windows.UI.Color AxisColor = Windows.UI.Color.FromArgb(255, 20, 20, 20);
        static readonly Windows.UI.Color CurveColor = Windows.UI.Color.FromArgb(255, 255, 0, 0);

        float m_scale;
        Vector2 m_origin;

        Dictionary<float, CanvasTextLayout> m_labels = new Dictionary<float, CanvasTextLayout>();
        CanvasTextFormat m_labelFormat = new CanvasTextFormat
        {
            WordWrapping = CanvasWordWrapping.NoWrap,
            VerticalAlignment = CanvasVerticalAlignment.Center,
            FontSize = 12.0f,
            FontFamily = "Cambria"
        };

        public MainWindow()
        {
            this.InitializeComponent();
        }

        private void SetDefaultTransform(CanvasControl sender)
        {
            m_scale = 50;
            m_origin = new Vector2(
                (float)sender.ActualWidth / 2,
                (float)sender.ActualHeight / 2
                );
        }

        private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            if (m_scale == 0)
            {
                SetDefaultTransform(sender);
            }

            args.DrawingSession.Clear(BackColor);

            DrawAxes(sender, args.DrawingSession);
        }

        // Compute the "axis unit", which is the distance in logical units
        // between labeled graduations on an axis.
        private static float AxisUnitFromScale(float scale)
        {
            // Minimum spacing between graduations in DIPs.
            const float minSpacing = 50;

            // Choose the unit such that (scale * unit) is between
            // minSpacing and (2 * minSpacing). For example:
            //
            //      scale        unit    scale * unit
            //      ---------------------------------
            //      25..50       2       50..100
            //      50..100      1       50..100
            //      100..200     0.5     50..100
            //
            // Every doubling of the scale halves the unit.
            // The unit is always a power of 2.
            //
            float e = -float.Floor(float.Log2(scale / minSpacing));
            e = float.Max(-3, e); // limit the minimum unit
            return float.Pow(2, e);
        }

        private void DrawVerticalAxisLine(CanvasDrawingSession g, float x, float top, float bottom)
        {
            g.FillRectangle(x - 0.5f, top, 1, bottom - top, AxisColor);
        }

        private void DrawHorizontalAxisLine(CanvasDrawingSession g, float y, float left, float right)
        {
            g.FillRectangle(left, y - 0.5f, right - left, 1.0f, AxisColor);
        }

        private CanvasTextLayout GetLabel(CanvasControl canvas, float value)
        {
            CanvasTextLayout result;
            if (!m_labels.TryGetValue(value, out result))
            {
                result = new CanvasTextLayout(canvas, value.ToString(), m_labelFormat, 0.0f, 0.0f);
                m_labels.Add(value, result);
            }
            return result;
        }

        private void DrawAxes(CanvasControl canvas, CanvasDrawingSession g)
        {
            float right = (float)canvas.ActualWidth;
            float bottom = (float)canvas.ActualHeight;
            float unit = AxisUnitFromScale(m_scale);
            float spacing = unit * m_scale;
            const float tickSize = 5;
            const float labelOffset = 10;

            // Draw the X axis.
            DrawHorizontalAxisLine(g, m_origin.Y, 0, right);

            // Draw the graduations along the X axis.
            float iLeft = -float.Floor(m_origin.X / spacing);
            float iRight = float.Floor((right - m_origin.X) / spacing);
            for (float i = iLeft; i <= iRight; i += 1)
            {
                if (i != 0)
                {
                    float x = m_origin.X + (i * spacing);
                    DrawVerticalAxisLine(g, x, m_origin.Y - tickSize, m_origin.Y + tickSize);
                }
            }


            // Draw the Y axis.
            DrawVerticalAxisLine(g, m_origin.X, 0, bottom);

            // Draw graduations along the Y axis.
            float iTop = -float.Floor(m_origin.Y / spacing);
            float iBottom = float.Floor((bottom - m_origin.Y) / spacing);
            for (float i = iTop; i <= iBottom; i += 1)
            {
                if (i != 0)
                {
                    float y = m_origin.Y + (i * spacing);
                    DrawHorizontalAxisLine(g, y, m_origin.X - tickSize, m_origin.X + tickSize);
                }
            }

            // Draw labels along the X axis.
            var matrix = Matrix3x2.CreateRotation(float.Pi / 2);
            for (float i = iLeft; i <= iRight; i += 1)
            {
                if (i != 0)
                {
                    var label = GetLabel(canvas, i * unit);
                    matrix.M31 = m_origin.X + (i * spacing);
                    matrix.M32 = m_origin.Y + labelOffset;
                    g.Transform = matrix;
                    g.DrawTextLayout(label, 0, 0, AxisColor);
                }
            }
            g.Transform = Matrix3x2.Identity;

            // Draw labels along the Y axis.
            for (float i = iTop; i <= iBottom; i += 1)
            {
                if (i != 0)
                {
                    float y = m_origin.Y + (i * spacing);
                    var label = GetLabel(canvas, i * -unit);
                    g.DrawTextLayout(label, m_origin.X + labelOffset, y, AxisColor);
                }
            }
        }

        private void CanvasControl_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var delta = e.Delta;

            m_scale *= delta.Scale;
            m_origin += delta.Translation.ToVector2();

            (sender as CanvasControl)?.Invalidate();
        }
    }
}
