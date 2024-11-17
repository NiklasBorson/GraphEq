using System;
using System.Collections.Generic;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace GraphEq
{
    internal class AxisRenderer : IDisposable
    {
        static readonly Windows.UI.Color AxisColor = Windows.UI.Color.FromArgb(255, 20, 20, 20);

        ICanvasResourceCreator m_resourceCreator;
        Dictionary<float, CanvasTextLayout> m_labels = new Dictionary<float, CanvasTextLayout>();
        CanvasTextFormat m_labelFormat = new CanvasTextFormat
        {
            WordWrapping = CanvasWordWrapping.NoWrap,
            VerticalAlignment = CanvasVerticalAlignment.Center,
            FontSize = 12.0f,
            FontFamily = "Cambria"
        };

        public AxisRenderer(ICanvasResourceCreator resourceCreator)
        {
            m_resourceCreator = resourceCreator;
        }

        public void DrawAxes(
            CanvasDrawingSession g,
            CanvasControl canvas,
            float scale,
            Vector2 origin
            )
        {
            float unit = AxisUnitFromScale(scale);
            float spacing = unit * scale;
            float height = (float)canvas.ActualHeight;
            float width = (float)canvas.ActualWidth;

            // Draw the Y axis.
            g.Transform = Matrix3x2.CreateTranslation(origin.X, origin.Y);
            DrawAxis(g, scale, unit, -origin.Y, height - origin.Y);

            // Draw the X axis by rotating 90 degrees to the right.
            g.Transform = Matrix3x2.CreateRotation(float.Pi / 2) *
                Matrix3x2.CreateTranslation(origin.X, origin.Y);
            DrawAxis(g, scale, unit, origin.X - width, origin.X);

            g.Transform = Matrix3x2.Identity;
        }

        void DrawAxis(CanvasDrawingSession g, float scale, float unit, float minY, float maxY)
        {
            const float tickSize = 5;
            const float labelOffset = 10;

            // Draw the axis itself.
            g.FillRectangle(
                -0.5f,
                minY,
                1.0f,
                maxY - minY,
                AxisColor
                );

            float spacing = unit * scale;
            float iMin = float.Floor(minY / spacing);
            float iMax = float.Ceiling(maxY / spacing);

            // Draw the graduation tick marks.
            for (float i = iMin; i <= iMax; i += 1)
            {
                if (i != 0)
                {
                    float y = i * spacing;
                    g.FillRectangle(
                        -tickSize,
                        y - 0.5f,
                        tickSize * 2,
                        1.0f,
                        AxisColor
                        );
                }
            }

            // Draw the graduation labels.
            for (float i = iMin; i <= iMax; i += 1)
            {
                if (i != 0)
                {
                    float y = i * spacing;
                    var label = GetLabel(i * -unit);
                    g.DrawTextLayout(label, labelOffset, y, AxisColor);
                }
            }
        }

        // Compute the "axis unit", which is the distance in logical units
        // between labeled graduations on an axis.
        static float AxisUnitFromScale(float scale)
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

        private CanvasTextLayout GetLabel(float value)
        {
            CanvasTextLayout result;
            if (!m_labels.TryGetValue(value, out result))
            {
                result = new CanvasTextLayout(m_resourceCreator, value.ToString(), m_labelFormat, 0.0f, 0.0f);
                m_labels.Add(value, result);
            }
            return result;
        }

        public void Dispose()
        {
            foreach (var label in m_labels.Values)
            {
                label.Dispose();
            }
            m_labels.Clear();
        }
    }
}
