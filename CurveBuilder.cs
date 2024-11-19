using System;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;

namespace GraphEq
{
    internal struct CurveBuilder : IDisposable
    {
        static readonly Windows.UI.Color CurveColor = Windows.UI.Color.FromArgb(255, 255, 0, 0);

        CanvasPathBuilder m_pathBuilder;
        Expr m_expr;
        double[] m_paramValues = new double[1];
        double m_scale;
        double m_inverseScale;
        Vector2 m_origin;
        Vector2[] m_figurePoints; // buffer for points in the current figure

        public static void Draw(
            CanvasDrawingSession drawingSession,
            ICanvasResourceCreator resourceCreator,
            Expr expr,
            float scale,
            Vector2 origin,
            float canvasWidth,
            float canvasHeight
            )
        {
            using (var builder = new CurveBuilder(resourceCreator, expr, scale, origin))
            {
                using (var geometry = builder.CreateGeometry(canvasWidth, canvasHeight))
                {
                    drawingSession.DrawGeometry(geometry, new Vector2(), CurveColor, 2.0f);
                }
            }
        }

        public CurveBuilder(ICanvasResourceCreator resourceCreator, Expr expr, float scale, Vector2 origin)
        {
            m_pathBuilder = new CanvasPathBuilder(resourceCreator);
            m_expr = expr;
            m_scale = scale;
            m_inverseScale = 1.0 / m_scale;
            m_origin = origin;
        }

        public CanvasGeometry CreateGeometry(float canvasWidth, float canvasHeight)
        {
            // Ensure we have a buffer big enough for the maximum number of points.
            int maxPoints = (int)float.Ceiling(canvasWidth) + 1;
            if (m_figurePoints == null || m_figurePoints.Length < maxPoints)
            {
                m_figurePoints = new Vector2[maxPoints];
            }
            int pointCount = 0;

            // Iterate over integral X coordinates from 0 through the width.
            for (int i = 0; i < maxPoints; i++)
            {
                float x = (float)i;
                float y = GetY(x);
                bool isVisible = y >= 0 && y <= canvasHeight;

                if (pointCount != 0)
                {
                    // End the current figure if the point is off screen or the curve is not continuous.
                    bool isJoined = float.IsRealNumber(y) && IsJoined(m_figurePoints[pointCount - 1], new Vector2(x, y));
                    if (!isVisible || !isJoined)
                    {
                        if (isJoined)
                        {
                            // Include this point, so the curve doesn't stop before the edge of the window.
                            m_figurePoints[pointCount++] = new Vector2(x, y);
                        }

                        // Add the current figure and begin a new one.
                        AddFigure(m_figurePoints.AsSpan(0, pointCount), canvasWidth, canvasHeight);
                        pointCount = 0;
                    }
                }

                // Add the point to the current figure if it's visible.
                if (isVisible)
                {
                    m_figurePoints[pointCount++] = new Vector2(x, y);
                }
            }

            if (pointCount != 0)
            {
                AddFigure(m_figurePoints.AsSpan(0, pointCount), canvasWidth, canvasHeight);
            }

            return CanvasGeometry.CreatePath(m_pathBuilder);
        }

        void AddFigure(Span<Vector2> points, float canvasWidth, float canvasHeight)
        {
            // Begin the figure and add the first point.
            var first = points[0];
            if (first.X > 0 && first.Y > 0 && first.Y < canvasHeight)
            {
                // The first point is visible. Interpolate to find a connected point to its
                // left so we can draw asymptotes correctly.
                var pt = FindConnectedPoint(first, first.X - 1, canvasHeight);
                m_pathBuilder.BeginFigure(pt);
                m_pathBuilder.AddLine(first);
            }
            else
            {
                m_pathBuilder.BeginFigure(first);
            }

            // Add the remaining points.
            for (int i = 1; i < points.Length; i++)
            {
                m_pathBuilder.AddLine(points[i]);
            }

            // If the last point is visible, interpolate to find a connected point to its
            // right so we can draw asymptotes correctly.
            var last = points[points.Length - 1];
            if (last.X < canvasWidth && last.Y > 0 && last.Y < canvasHeight)
            {
                var pt = FindConnectedPoint(last, last.X + 1, canvasHeight);
                m_pathBuilder.AddLine(pt);
            }

            m_pathBuilder.EndFigure(CanvasFigureLoop.Open);
        }

        // Searches for a point P on the curve such that:
        //  - P is connected to start
        //  - P.X is between start.X and xLim
        //  - P.Y is out of view if possible (if near an asymptote)
        Vector2 FindConnectedPoint(Vector2 start, float xLim, float canvasHeight)
        {
            for (int i = 0; i < 10 && start.X != xLim; i++)
            {
                // Interpolate between start and xLim.
                float x = (start.X + xLim) / 2;
                float y = GetY(x);

                if (!float.IsRealNumber(y) || !IsJoined(start, new Vector2(x, y)))
                {
                    // (x, y) is not connected, so move closer to start.
                    xLim = x;
                }
                else
                {
                    // (x, y) is connected, so make it the new start point.
                    start = new Vector2(x, y);

                    // If we're off the canvas then this is our point.
                    if (y <= 0 || y >= canvasHeight)
                    {
                        return start;
                    }
                }
            }
            return start;
        }

        float GetY(float x)
        {
            double value = x;

            // Convert to logical units.
            value -= m_origin.X;
            value *= m_inverseScale;

            // Invoke the expression.
            m_paramValues[0] = value;
            value = m_expr.Eval(m_paramValues);

            // Convert back to DIPs and flip so positive Y is up.
            value *= -m_scale;
            value += m_origin.Y;

            return (float)value;
        }

        bool IsJoined(Vector2 point0, Vector2 point1)
        {
            return IsJoined(point0, point1, DistanceSquared(point0, point1));
        }

        bool IsJoined(Vector2 point0, Vector2 point1, float d)
        {
            if (d <= 1)
            {
                return true;
            }

            float x = (point0.X + point1.X) * 0.5f;
            float y = GetY(x);
            if (!float.IsRealNumber(y))
            {
                return false;
            }
            var mid = new Vector2(x, y);

            float d0 = DistanceSquared(point0, mid);
            float d1 = DistanceSquared(mid, point1);

            return d0 < d && d1 < d && 
                IsJoined(point0, mid, d0) && 
                IsJoined(point1, mid, d1);
        }

        static float DistanceSquared(Vector2 point0, Vector2 point1)
        {
            float dx = point0.X - point1.X;
            float dy = point0.Y - point1.Y;
            return dx * dx + dy * dy;
        }

        public void Dispose()
        {
            m_pathBuilder.Dispose();
        }
    }
}
