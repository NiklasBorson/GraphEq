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

        enum State
        {
            Empty,
            Pending,
            InFigure
        }
        State m_state = State.Empty;
        Vector2 m_lastPoint;

        public static void Draw(
            CanvasDrawingSession drawingSession,
            ICanvasResourceCreator resourceCreator,
            Expr expr,
            float scale,
            Vector2 origin,
            float canvasWidth
            )
        {
            using (var builder = new CurveBuilder(resourceCreator, expr, scale, origin))
            {
                using (var geometry = builder.CreateGeometry(canvasWidth))
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

        public CanvasGeometry CreateGeometry(float canvasWidth)
        {
            for (float x = 0; x < canvasWidth; x += 1)
            {
                float y = GetY(x);
                if (float.IsRealNumber(y))
                {
                    AddPoint(new Vector2(x, y));
                }
                else
                {
                    EndFigure();
                }
            }
            EndFigure();
            return CanvasGeometry.CreatePath(m_pathBuilder);
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

        void EndFigure()
        {
            if (m_state == State.InFigure)
            {
                m_pathBuilder.EndFigure(CanvasFigureLoop.Open);
            }
            m_state = State.Empty;
        }

        void AddPoint(Vector2 point)
        {
            // Check for discontinuity.
            if (m_state != State.Empty && !IsJoined(m_lastPoint, point))
            {
                EndFigure();
            }

            switch (m_state)
            {
                case State.Empty:
                    // No points have been added.
                    // Remember the new point, but don't begin a figure yet.
                    m_state = State.Pending;
                    break;

                case State.Pending:
                    // We now have two points, so begin the figure.
                    m_pathBuilder.BeginFigure(m_lastPoint);
                    m_pathBuilder.AddLine(point);
                    m_state = State.InFigure;
                    break;

                case State.InFigure:
                    // We're in a figure, so add the new point.
                    m_pathBuilder.AddLine(point);
                    break;
            }

            m_lastPoint = point;
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
