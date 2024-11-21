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
        static readonly Windows.UI.Color GridColor = Windows.UI.Color.FromArgb(255, 255, 255, 255);
        static readonly Windows.UI.Color AxisColor = Windows.UI.Color.FromArgb(255, 20, 20, 20);

        CanvasTextFormat m_labelFormat = new CanvasTextFormat
        {
            WordWrapping = CanvasWordWrapping.NoWrap,
            VerticalAlignment = CanvasVerticalAlignment.Center,
            FontSize = 12.0f,
            FontFamily = "Cambria"
        };

        // Dictionary of labels so we can reuse the same CanvasTextLayout object every
        // time we draw the same number.
        ICanvasResourceCreator m_resourceCreator;
        Dictionary<float, CanvasTextLayout> m_labels = new Dictionary<float, CanvasTextLayout>();

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
            // Determine how far about axis labels are in logical units.
            float unit = AxisUnitFromScale(scale);

            // Get the canvas width and height.
            float height = (float)canvas.ActualHeight;
            float width = (float)canvas.ActualWidth;

            // Draw both axes in three passes:
            //  1. Grid lines
            //  2. Axes and tick marks
            //  3. Text labels
            for (int layer = 0; layer < 3; layer++)
            {
                // Draw the Y axis.
                g.Transform = Matrix3x2.CreateTranslation(origin.X, origin.Y);
                DrawVerticalAxis(
                    g, 
                    scale, 
                    unit, 
                    /*minX*/ -origin.X,
                    /*minY*/ -origin.Y, 
                    /*maxX*/ width - origin.X,
                    /*maxY*/ height - origin.Y, 
                    layer
                    );

                // Draw the X axis.
                // This is just a vertical axis rotated right 90 degrees.
                // Note that:
                //  - minY/maxY are distances along the "vertical" axis. Because of
                //    rotation, these are computed from origin.X and width.
                //  - minX/maxX are distances perpendicular to the axis, so are
                //    computed from origin.Y and height.
                g.Transform = Matrix3x2.CreateRotation(float.Pi / 2) *
                    Matrix3x2.CreateTranslation(origin.X, origin.Y);
                DrawVerticalAxis(
                    g, 
                    scale, 
                    unit,
                    /*minX*/ -origin.Y,
                    /*minY*/ origin.X - width,
                    /*maxX*/ height + origin.Y,
                    /*maxY*/ origin.X, 
                    layer
                    );
            }

            // Restore the original transform.
            g.Transform = Matrix3x2.Identity;
        }

        // Draws the Y axis.
        //
        // This is also called with a rotate transform to draw the X axis, but
        // the logic inside this method is exactly the same. An X axis is just a
        // Y axis rotated 90 degrees to the right.
        //
        // In rendering coordinates:
        //
        //   - (0, 0) corresponds to the logical (Cartesian) origin
        //
        //   - The positive Y direction is down, corresponding to negative
        //     Y coordiantes in the Cartesian coordinate space.
        //
        //   - No scaling is applied so (0, scale) corresponds to (0, -1)
        //     in Cartesian coordinates.
        //
        //   - (minX, minY) is the top-left visible pixel.
        //
        //   - (maxX, maxY) is the bottom-right visible pixel.
        //
        void DrawVerticalAxis(
            CanvasDrawingSession g, 
            float scale, 
            float unit, 
            float minX,
            float minY,
            float maxX,
            float maxY, 
            int layer
            )
        {
            const float tickSize = 5;
            const float labelOffset = 10;

            float spacing = unit * scale;
            float iMin = float.Floor(minY / spacing);
            float iMax = float.Ceiling(maxY / spacing);

            switch (layer)
            {
                case 0:
                    // Draw the grid lines.
                    for (float i = iMin; i <= iMax; i += 1)
                    {
                        if (i != 0)
                        {
                            float y = i * spacing;
                            g.FillRectangle(
                                minX,
                                y - 0.5f,
                                maxX - minX,
                                1.0f,
                                GridColor
                                );
                        }
                    }
                    break;

                case 1:
                    // Draw the axis itself.
                    g.FillRectangle(
                        -0.5f,
                        minY,
                        1.0f,
                        maxY - minY,
                        AxisColor
                        );

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
                    break;

                case 2:
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
                    break;
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

        // Gets a CanvasTextLayout for a number.
        private CanvasTextLayout GetLabel(float value)
        {
            // Use a dictionary so we can reuse the same CanvasTextLayout object when
            // drawing the same number.
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
            m_labelFormat.Dispose();

            foreach (var label in m_labels.Values)
            {
                label.Dispose();
            }
            m_labels.Clear();
        }
    }
}
