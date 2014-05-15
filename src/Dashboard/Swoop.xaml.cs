using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Dashboard
{
    /// <summary>
    /// Interaction logic for PathTest.xaml
    /// </summary>
    public partial class Swoop : UserControl
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Fill", typeof(Brush), typeof(Swoop), new PropertyMetadata(Brushes.Red));

        public static readonly DependencyProperty StartAngleProperty     = DependencyProperty.Register("StartAngle",     typeof(double), typeof(Swoop), new PropertyMetadata(-90d, PathChanged));
        public static readonly DependencyProperty StartThicknessProperty = DependencyProperty.Register("StartThickness", typeof(double), typeof(Swoop), new PropertyMetadata(0d,   PathChanged));
        public static readonly DependencyProperty EndAngleProperty       = DependencyProperty.Register("EndAngle",       typeof(double), typeof(Swoop), new PropertyMetadata(90d,  PathChanged));
        public static readonly DependencyProperty EndThicknessProperty   = DependencyProperty.Register("EndThickness",   typeof(double), typeof(Swoop), new PropertyMetadata(10d,  PathChanged));

        private static void PathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = (Swoop) d;

            if (instance.IsLoaded)
            {
                instance.RedrawSwoop();
            }
        }

        public Swoop()
        {
            InitializeComponent();

            Loaded += (sender, e) => RedrawSwoop();
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == HeightProperty || e.Property == WidthProperty)
            {
                RedrawSwoop();
            }
        }

        public Brush Fill
        {
            get { return (Brush)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        public double StartAngle
        {
            get { return (double)GetValue(StartAngleProperty); }
            set { SetValue(StartAngleProperty, value); }
        }

        public double StartThickness
        {
            get { return (double)GetValue(StartThicknessProperty); }
            set { SetValue(StartThicknessProperty, value); }
        }

        public double EndAngle
        {
            get { return (double)GetValue(EndAngleProperty); }
            set { SetValue(EndAngleProperty, value); }
        }

        public double EndThickness
        {
            get { return (double)GetValue(EndThicknessProperty); }
            set { SetValue(EndThicknessProperty, value); }
        }

        private void RedrawSwoop()
        {
            //arc from left outer to right outer, line to right inner, arc to left inner with increasing radius, close path
            double outerRadius = Math.Max(Height, Width) / 2;

            Point center = new Point(Width / 2, Height / 2);
            
            //Convert angles to radians, shifted so that 0 degrees is north
            double startAngleRads = DegreesToRads(StartAngle) - (Math.PI / 2);
            double endAngleRads = DegreesToRads(EndAngle) - (Math.PI / 2);

            IEnumerable<Point> swoopPoints = PlotSwoop(center:                center,
                                                       outerRadius:           outerRadius, 
                                                       startAngleRads:        startAngleRads,
                                                       startAngleInnerRadius: outerRadius - StartThickness,
                                                       endAngleRads:          endAngleRads,
                                                       endAngleInnerRadius:   outerRadius - EndThickness);

            PathFigure figure = new PathFigure();
            Point? firstPoint = null;

            foreach (Point currentPoint in swoopPoints)
            {
                if (firstPoint == null)
                {
                    figure.StartPoint = currentPoint;
                    firstPoint = currentPoint;

                    continue;
                }

                figure.Segments.Add(new LineSegment(currentPoint, isStroked: true));
            }

            figure.IsClosed = true;

            SwoopPath.Data = new PathGeometry { 
                Figures = new PathFigureCollection(new[] { figure })
            };
        }

        private IEnumerable<Point> PlotSwoop(Point center, double outerRadius, double startAngleRads, double startAngleInnerRadius, double endAngleRads, double endAngleInnerRadius)
        {
            //Start from outer left, arc to outer right
            double angleDelta = endAngleRads - startAngleRads;
            double pointsOnArc = angleDelta / DegreesToRads(5); // the increment will be segments of approx 5 degrees
            double angleIncr = angleDelta / pointsOnArc;

            for (double a = startAngleRads; a < endAngleRads; a += angleIncr)
            {
                yield return FindPointOnArc(center, a, outerRadius);
            }

            //outer right
            yield return FindPointOnArc(center, endAngleRads, outerRadius);

            //start from inner right, arc to inner left with slowly changing radius
            double radiusDelta = endAngleInnerRadius - startAngleInnerRadius;
            double radiusIncr = radiusDelta / pointsOnArc; //NOTE: radius may increase or decrease

            for (double a = endAngleRads, r = endAngleInnerRadius; a > startAngleRads; a -= angleIncr, r -= radiusIncr)
            {
                yield return FindPointOnArc(center, a, r);
            }

            //inner left
            yield return FindPointOnArc(center, startAngleRads, startAngleInnerRadius);
        }

        private static double DegreesToRads(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

        private static Point FindPointOnArc(Point center, double angleRads, double radius)
        {
            return new Point(center.X + (radius * Math.Cos(angleRads)),
                             center.Y + (radius * Math.Sin(angleRads)));
        }
    }
}
