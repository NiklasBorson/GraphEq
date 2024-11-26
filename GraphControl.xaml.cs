using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Numerics;
using System.Collections.ObjectModel;
using Microsoft.Graphics.Canvas;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace GraphEq
{
    public sealed partial class GraphControl : UserControl
    {
        static readonly Windows.UI.Color BackColor = Windows.UI.Color.FromArgb(255, 220, 220, 220);
        public const float DefaultScale = 50;

        // Canvas scale factor and origin.
        float m_scale = DefaultScale;
        Vector2 m_relativeOrigin = new Vector2(0.5f, 0.5f);
        Vector2 m_canvasSize;

        // Content to render.
        ObservableCollection<FormulaViewModel> m_formulas;

        // Device-dependent resources.
        AxisRenderer m_axisRenderer;

        public GraphControl()
        {
            this.InitializeComponent();
        }

        public ObservableCollection<FormulaViewModel> Formulas
        {
            get => m_formulas;

            set
            {
                // Remove event handlers for the old collection.
                if (m_formulas != null)
                {
                    m_formulas.CollectionChanged -= Formulas_CollectionChanged;
                    foreach (var formula in m_formulas)
                    {
                        formula.PropertyChanged -= Formula_PropertyChanged;
                    }
                }

                // Set the new collection.
                m_formulas = value;

                // Add event handlers for the new collection.
                m_formulas.CollectionChanged += Formulas_CollectionChanged;
                foreach (var formula in m_formulas)
                {
                    formula.PropertyChanged += Formula_PropertyChanged;
                }

                Canvas.Invalidate();
            }
        }

        private void Formulas_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    ((FormulaViewModel)item).PropertyChanged -= Formula_PropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    ((FormulaViewModel)item).PropertyChanged += Formula_PropertyChanged;
                }
            }
            Canvas.Invalidate();
        }

        private void Formula_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Repaint only if the Expression property changes.
            if (e.PropertyName == nameof(FormulaViewModel.Expression))
            {
                Canvas.Invalidate();
            }
        }

        public float GraphScale
        {
            get => m_scale;

            set
            {
                if (value > 0 && value != m_scale)
                {
                    m_scale = value;
                    Canvas.Invalidate();
                }
            }
        }

        public Vector2 RelativeOrigin
        {
            get => m_relativeOrigin;

            set
            {
                if (m_relativeOrigin != value)
                {
                    m_relativeOrigin = value;
                    Canvas.Invalidate();
                }
            }
        }

        public Vector2 PixelOrigin
        {
            get => RelativeOrigin * m_canvasSize;

            set
            {
                RelativeOrigin = value / m_canvasSize;
            }
        }

        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            m_canvasSize = new Vector2((float)Canvas.ActualWidth, (float)Canvas.ActualHeight);
        }

        private void CanvasControl_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            var delta = e.Delta;

            m_scale *= delta.Scale;
            PixelOrigin += delta.Translation.ToVector2();

            this.Canvas.Invalidate();
        }

        private void CanvasControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            var ctrl = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);

            if (ctrl.HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down))
            {
                var delta = e.GetCurrentPoint(Canvas).Properties.MouseWheelDelta;

                m_scale *= float.Pow(2.0f, delta * 0.001f);

                this.Canvas.Invalidate();
            }
        }

        private void CanvasControl_CreateResources(CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            m_axisRenderer?.Dispose();
            m_axisRenderer = new AxisRenderer(sender);
        }

        void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            args.DrawingSession.Clear(BackColor);

            // Draw the axes.
            m_axisRenderer.DrawAxes(args.DrawingSession, sender, m_scale, PixelOrigin);

            // Draw the formulas.
            if (m_formulas != null)
            {
                foreach (var formula in m_formulas)
                {
                    DrawFormula(args.DrawingSession, formula);
                }
            }
        }
        void DrawFormula(CanvasDrawingSession drawingSession, FormulaViewModel formula)
        {
            if (formula.Expression != FormulaViewModel.EmptyExpression)
            {
                using (var geometry = CurveBuilder.CreateGeometry(
                    Canvas,
                    formula.Expression,
                    m_scale,
                    PixelOrigin,
                    m_canvasSize.ToSize()
                    ))
                {
                    drawingSession.DrawGeometry(
                        geometry,
                        new Vector2(),
                        formula.Color,
                        2.0f
                        );
                }
            }
        }
    }
}
