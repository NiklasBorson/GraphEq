using System;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Windows.Foundation;

namespace GraphEq
{
    internal struct CurveBuilder : IDisposable
    {
        CanvasPathBuilder m_pathBuilder;
        Expr m_expr;
        double[] m_paramValues = new double[1];
        double m_scale;
        double m_inverseScale;
        Point m_origin;
        Point[] m_figurePoints; // buffer for points in the current figure

        public static void Draw(
            CanvasDrawingSession drawingSession,
            ICanvasResourceCreator resourceCreator,
            Expr expr,
            float scale,
            Vector2 origin,
            float canvasWidth,
            float canvasHeight,
            Windows.UI.Color color
            )
        {
            using (var builder = new CurveBuilder(resourceCreator, expr, scale, origin))
            {
                using (var geometry = builder.CreateGeometry(canvasWidth, canvasHeight))
                {
                    drawingSession.DrawGeometry(geometry, new Vector2(), color, 2.0f);
                }
            }
        }

        public CurveBuilder(ICanvasResourceCreator resourceCreator, Expr expr, float scale, Vector2 origin)
        {
            m_pathBuilder = new CanvasPathBuilder(resourceCreator);
            m_expr = expr;
            m_scale = scale;
            m_inverseScale = 1.0 / m_scale;
            m_origin = origin.ToPoint();
        }

        public CanvasGeometry CreateGeometry(double canvasWidth, double canvasHeight)
        {
            // Ensure we have a buffer big enough for the maximum number of points.
            int maxPoints = (int)double.Ceiling(canvasWidth) + 1;
            if (m_figurePoints == null || m_figurePoints.Length < maxPoints)
            {
                m_figurePoints = new Point[maxPoints];
            }
            int pointCount = 0;

            // Iterate over integral X coordinates from 0 through the width.
            for (int i = 0; i < maxPoints; i++)
            {
                double x = i;
                double y = GetY(x);
                bool isVisible = y >= 0 && y <= canvasHeight;

                if (pointCount != 0)
                {
                    // End the current figure if the point is off screen or the curve is not continuous.
                    var last = m_figurePoints[pointCount - 1];
                    bool isJoined = double.IsRealNumber(y) && IsJoined(last.X, last.Y, x, y);
                    if (!isVisible || !isJoined)
                    {
                        if (isJoined)
                        {
                            // Include this point, so the curve doesn't stop before the edge of the window.
                            m_figurePoints[pointCount++] = new Point(x, y);
                        }

                        // Add the current figure and begin a new one.
                        AddFigure(m_figurePoints.AsSpan(0, pointCount), canvasWidth, canvasHeight);
                        pointCount = 0;
                    }
                }

                // Add the point to the current figure if it's visible.
                if (isVisible)
                {
                    m_figurePoints[pointCount++] = new Point(x, y);
                }
            }

            if (pointCount != 0)
            {
                AddFigure(m_figurePoints.AsSpan(0, pointCount), canvasWidth, canvasHeight);
            }

            return CanvasGeometry.CreatePath(m_pathBuilder);
        }

        void AddFigure(Span<Point> points, double canvasWidth, double canvasHeight)
        {
            // Begin the figure and add the first point.
            var first = points[0];
            if (first.X > 0 && first.Y > 0 && first.Y < canvasHeight)
            {
                // The first point is visible. Interpolate to find a connected point to its
                // left so we can draw asymptotes correctly.
                var pt = FindFigureEndPoint(first, first.X - 1, canvasHeight);
                m_pathBuilder.BeginFigure(pt.ToVector2());
                m_pathBuilder.AddLine(first.ToVector2());
            }
            else
            {
                m_pathBuilder.BeginFigure(first.ToVector2());
            }

            // Add the remaining points.
            for (int i = 1; i < points.Length; i++)
            {
                m_pathBuilder.AddLine(points[i].ToVector2());
            }

            // If the last point is visible, interpolate to find a connected point to its
            // right so we can draw asymptotes correctly.
            var last = points[points.Length - 1];
            if (last.X < canvasWidth && last.Y > 0 && last.Y < canvasHeight)
            {
                var pt = FindFigureEndPoint(last, last.X + 1, canvasHeight);
                m_pathBuilder.AddLine(pt.ToVector2());
            }

            m_pathBuilder.EndFigure(CanvasFigureLoop.Open);
        }

        // Finds a point P, where:
        //  - P is on the curve and connected to figurePoint.
        //  - P.X is in the range figurePoint.X..limitX.
        //  - P.Y is off canvas if there is an asymptote in the range.
        Point FindFigureEndPoint(Point figurePoint, double limitX, double canvasHeight)
        {
            const int maxIterations = 16;

            // Use binary search to find a point between figurePoint.X and limitX.
            // Loop invariants:
            //  - pt is on the curve and connected to figurePoint.
            //  - The point we're looking for is in the range pt.X..limitX.
            var pt = figurePoint;
            for (int i = 0; i < maxIterations && pt.X != limitX; i++)
            {
                // If pt is off canvas then we've found our point.
                if (pt.Y <= 0 || pt.Y >= canvasHeight)
                {
                    return pt;
                }

                // Select an intermediate point at x.
                double x = (pt.X + limitX) / 2;
                double y = GetY(x);

                // Is x on the curve and connected to pt?
                if (double.IsRealNumber(y) && IsJoined(pt.X, pt.Y, x, y))
                {
                    // Connected: narrow the search to x..limitX.
                    pt = new Point(x, y);
                }
                else
                {
                    // Not connected: narrow the search to pt..x.
                    limitX = x;
                }
            }

            return pt;
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

        bool IsJoined(double x1, double y1, double x2, double y2)
        {
            double dy = double.Abs(y2 - y1);
            if (dy <= 0.25)
                return true;

            double dx = double.Abs(x2 - x1);
            if (dx < 0.00001)
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
