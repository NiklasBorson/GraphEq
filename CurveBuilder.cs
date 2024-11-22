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
        double m_canvasHeight;
        Point m_origin;
        Vector2 m_lastPoint;
        bool m_havePoint = false;
        bool m_inFigure = false;

        public static CanvasGeometry CreateGeometry(ICanvasResourceCreator resourceCreator, Expr expr, float scale, Vector2 origin, Size canvasSize)
        {
            using (var builder = new CurveBuilder(resourceCreator, expr, scale, origin, canvasSize))
            {
                return builder.CreatePath(canvasSize);
            }
        }

        private CurveBuilder(ICanvasResourceCreator resourceCreator, Expr expr, float scale, Vector2 origin, Size canvasSize)
        {
            m_expr = expr;
            m_scale = scale;
            m_inverseScale = 1.0 / m_scale;
            m_canvasHeight = canvasSize.Height;
            m_origin = origin.ToPoint();
            m_pathBuilder = new CanvasPathBuilder(resourceCreator);
        }

        private CanvasGeometry CreatePath(Size canvasSize)
        {
            AddIntervalPoints(
                new Point(0, GetY(0)),
                new Point(canvasSize.Width, GetY(canvasSize.Width))
                );
            EndFigure();

            return CanvasGeometry.CreatePath(m_pathBuilder);
        }

        // Gets the Y coordinate for a given X coordinate in Canvas coordinates.
        double GetY(double x)
        {
            // Convert X from canvas coordinates to the abstract
            // Cartesian coordinate space.
            m_paramValues[0] = (x - m_origin.X) * m_inverseScale;

            // Invoke the expression.
            double y = m_expr.Eval(m_paramValues);

            // Convert back to canvas coordinates.
            // Multiply by the negative scale to flip the Y axis.
            return (y * -m_scale) + m_origin.Y;
        }

        // Gets the point at a given X coordinate in Canvas coordinates.
        Point GetPointAt(double x)
        {
            return new Point(x, GetY(x));
        }

        // We build up a curve by sampling points at different X values.
        // This function determines whether we should sample additional
        // points between the two specified points.
        bool ShouldSplitInterval(Point left, Point right)
        {
            // The sample interval depends on factors. We want to
            // sample more frequently near a discontinuity like an
            // asymptote.
            const double maxSample = 1.0;
            const double minSample = 0.001;

            // Early out of the X values are very close or very far.
            double dx = right.X - left.X;
            if (dx > maxSample || dx <= minSample)
            {
                return dx > maxSample;
            }

            // Determine whether each point is on the curve.
            bool isLeftReal = double.IsRealNumber(left.Y);
            bool isRightReal = double.IsRealNumber(right.Y);

            if (isLeftReal && isRightReal)
            {
                // Both are real points.
                // The sample interval depends on the slope.
                double slope = double.Abs((right.Y - left.Y) / dx);

                if (slope < 1)
                {
                    // Shallow slope: use max sample frequency.
                    // A discontinuity is less likely here.
                    return dx > maxSample;
                }
                else
                {
                    // Step slope: sample period approaches minSample
                    // as the slop approaches infinity.
                    double sample = minSample + (maxSample - minSample) / slope;
                    return dx > sample;
                }
            }
            else if (isLeftReal != isRightReal)
            {
                // Only one point is real. Use the minimum sample frequency
                // so we can zero in on the discontinuity.
                return dx > minSample;
            }
            else
            {
                // Neither point is real: use the max sample frequency.
                // A discontinuity is less likely here.
                return dx > maxSample;
            }
        }

        bool IsPointVisible(double y) => y > 0 && y < m_canvasHeight;

        // Adds points in the interval from left to right. 
        void AddIntervalPoints(Point left, Point right)
        {
            // Split big intervals in half and recursively process the left half.
            while (ShouldSplitInterval(left, right))
            {
                var mid = GetPointAt((left.X + right.X) / 2);
                AddIntervalPoints(left, mid);
                left = mid;
            }

            // We now have a small enough interval that we're not going to
            // sample any more points between left and right.
            bool isLeftVisible = IsPointVisible(left.Y);
            bool isRightVisible = IsPointVisible(right.Y);

            if (isLeftVisible && isRightVisible)
            {
                AddPoint(left);
                AddPoint(right);
            }
            else if (isLeftVisible)
            {
                AddPoint(left);
                EndFigure();
            }
            else if (isRightVisible)
            {
                EndFigure();
                AddPoint(right);
            }
            else
            {
                EndFigure();
            }
        }

        void AddPoint(Point pt)
        {
            var point = pt.ToVector2();

            if (m_havePoint)
            {
                if (point != m_lastPoint)
                {
                    if (m_inFigure)
                    {
                        m_pathBuilder.AddLine(m_lastPoint);
                    }
                    else
                    {
                        m_pathBuilder.BeginFigure(m_lastPoint);
                        m_inFigure = true;
                    }
                    m_lastPoint = point;
                }
            }
            else
            {
                m_lastPoint = point;
                m_havePoint = true;
            }
        }

        void EndFigure()
        {
            if (m_inFigure)
            {
                m_pathBuilder.AddLine(m_lastPoint);
                m_pathBuilder.EndFigure(CanvasFigureLoop.Open);
                m_inFigure = false;
            }
            m_havePoint = false;
        }

        public void Dispose()
        {
            m_pathBuilder.Dispose();
        }
    }
}
