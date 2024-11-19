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
                float y = (float)GetY(x);
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
        //  - P.X is between start.X and limitX
        //  - P.Y is off the canvas if possible (if an asymptote)
        Vector2 FindConnectedPoint(Vector2 start, double limitX, float canvasHeight)
        {
            int counter = 100;

            double startX = start.X;
            double startY = start.Y;

            while (startX != limitX && startY > 0 && startY < canvasHeight)
            {
                // Limit the maximum number of iterations.
                if (--counter == 0)
                    break;

                // Select an intermediate point.
                double x = (startX + limitX) / 2;
                double y = GetY(x);

                // If (x,y) is off the curve, move closer to start.
                if (!double.IsRealNumber(y))
                {
                    limitX = x;
                    continue;
                }

                // If (x,y) is not connected, move closer to start.
                if (!IsJoined(startX, startY, x, y))
                {
                    limitX = x;
                    continue;
                }

                // Narrow the search with (x,y) as the new start point.
                startX = x;
                startY = y;
            }
            return new Vector2((float)startX, (float)startY);
        }

        double GetY(double x)
        {
            // Convert X from pixels to logical units.
            m_paramValues[0] = (x - m_origin.X) * m_inverseScale;

            // Invoke the expression.
            double y = m_expr.Eval(m_paramValues);

            // Convert back to pixels.
            // Multiply by the negative scale to flip the Y axis.
            return (y * -m_scale) + m_origin.Y;
        }

        bool IsJoined(Vector2 point0, Vector2 point1)
        {
            return IsJoined(point0.X, point0.Y, point1.X, point1.Y);
        }

        bool IsJoined(double x1, double y1, double x2, double y2)
        {
            double dy = double.Abs(y2 - y1);
            if (dy <= 1.0)
                return true;

            if (dy > 1000.0)
                return false;

            double x = (x1 + x2) / 2;
            double y = GetY(x);
            if (!double.IsRealNumber(y))
                return false;

            return IsJoined(x1, y1, x, y) && IsJoined(x, y, x2, y2);
        }

        public void Dispose()
        {
            m_pathBuilder.Dispose();
        }
    }
}
