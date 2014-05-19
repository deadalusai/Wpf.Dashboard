using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Dashboard
{
    public class Dial360Notch
    {
        public Dial360Notch() { }

        public Dial360Notch(string label, double angle)
        {
            Label = label;
            Angle = angle;
        }

        public string Label { get; set; }

        public double Angle { get; set; }
    }

    /// <summary>
    /// A Dial360  displays as a traditional circular gauge with numbers from 0 to 100. The
    /// needle sweeps through approximately 240 degrees.
    /// </summary>
    public partial class Dial360 : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(double), typeof(Dial360), new PropertyMetadata(0.0, ValuePropertyChanged));

        public static readonly DependencyProperty AnimationDurationProperty =
            DependencyProperty.Register("AnimationDuration", typeof(TimeSpan), typeof(Dial360), new PropertyMetadata(TimeSpan.FromSeconds(0.75), AnimationDurationChanged));

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(double), typeof(Dial360), new PropertyMetadata(0.0, NotchDisplayPropertyChanged));

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(double), typeof(Dial360), new PropertyMetadata(100.0, NotchDisplayPropertyChanged));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register("Label", typeof(string), typeof(Dial360), new PropertyMetadata(LabelPropertyChanged));

        public static readonly DependencyProperty NotchesProperty =
            DependencyProperty.Register("Notches", typeof(IEnumerable<Dial360Notch>), typeof(Dial360), new PropertyMetadata(null, NotchesPropertyChanged));

        public static readonly DependencyProperty DefaultNotchCountProperty =
            DependencyProperty.Register("DefaultNotchCount", typeof(int), typeof(Dial360), new PropertyMetadata(11, NotchDisplayPropertyChanged));

        private static void ValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = (Dial360)d;

            if (instance.IsLoaded)
            {
                instance.Animate();
            }
        }

        private static void AnimationDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        private static void NotchDisplayPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = (Dial360)d;

            if (instance.IsLoaded)
            {
                instance.RebuildDial();
                instance.Animate();
            }
        }

        private static void NotchesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var instance = (Dial360)d;

            //Unhook the old notch collection
            var oldNotches = e.OldValue as IEnumerable<Dial360Notch>;
            if (oldNotches != null)
            {
                instance.StopMonitoringNotchCollection(oldNotches);
            }

            //And hook up the new one!
            var newNotches = e.NewValue as IEnumerable<Dial360Notch>;
            if (newNotches != null)
            {
                instance.StartMonitoringNotchCollection(newNotches);
            }

            NotchDisplayPropertyChanged(d, e);
        }

        private static void LabelPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        private const double DEFAULT_MIN_ANGLE = -150, DEFAULT_MAX_ANGLE = 150;

        private readonly Storyboard _needleStoryboard;
        private readonly DoubleAnimation _needleAnimation;
        private readonly RotateTransform _needleTransform;

        private double _minAngle = DEFAULT_MIN_ANGLE, _maxAngle = DEFAULT_MAX_ANGLE;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dial360"/> class.
        /// </summary>
        public Dial360()
        {
            InitializeComponent();

            Loaded += ElementLoaded;
            Unloaded += ElementUnloaded;

            _needleStoryboard = (Storyboard)LayoutRoot.Resources["NeedleAnimation"];
            _needleAnimation = (DoubleAnimation)_needleStoryboard.Children[0];
            _needleTransform = (RotateTransform)NeedleAssembly.GetValue(RenderTransformProperty);
        }

        private void ElementUnloaded(object sender, RoutedEventArgs e)
        {
            StopMonitoringNotchCollection(Notches);
        }

        private void ElementLoaded(object sender, RoutedEventArgs e)
        {
            RebuildDial();
            Animate();
        }

        private void NotchesCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildDial();
        }

        private void StartMonitoringNotchCollection(IEnumerable<Dial360Notch> notches)
        {
            var notifiable = notches as INotifyCollectionChanged;
            if (notifiable != null)
            {
                notifiable.CollectionChanged += NotchesCollectionChanged;
            }
        }

        private void StopMonitoringNotchCollection(IEnumerable<Dial360Notch> notches)
        {
            var notifiable = notches as INotifyCollectionChanged;
            if (notifiable != null)
            {
                notifiable.CollectionChanged -= NotchesCollectionChanged;
            }
        }

        private void RebuildDial()
        {
            if (Notches == null)
            {
                //Build a best-guess collection of notches
                _minAngle = DEFAULT_MIN_ANGLE;
                _maxAngle = DEFAULT_MAX_ANGLE;

                int spaces = (DefaultNotchCount - 1);
                double notchSpacing = (_maxAngle - _minAngle) / spaces;

                double realMin = RealMinimum, realMax = RealMaximum;

                var notches = from i in Enumerable.Range(0, DefaultNotchCount)
                              let adjustedAngle = _minAngle + (i * notchSpacing)
                              let labelValue = realMin + (i * ((realMax - realMin) / spaces))
                              select new Dial360Notch {
                                  Angle = adjustedAngle,
                                  Label = labelValue.ToString(CultureInfo.InvariantCulture),
                              };

                DialPoints.ItemsSource = notches;
            }
            else
            {
                //Use the data-bound collection of notches
                var angles = Notches.Select(n => n.Angle).ToArray();

                _minAngle = angles.Min();
                _maxAngle = angles.Max();

                DialPoints.ItemsSource = Notches;
            }
        }

        /// <summary>
        /// Gets or sets the value. The value lies in the range Minimum &lt;= Value &lt;= Maximum
        /// </summary>
        public double Value
        {
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        /// <summary>
        /// Gets or sets the duration of the animation. The default is 0.75 seconds. Set
        /// the animation duration depending on the interval between the value changing.
        /// </summary>
        /// <value>The duration of the animation.</value>
        public TimeSpan AnimationDuration
        {
            get { return (TimeSpan)GetValue(AnimationDurationProperty); }
            set { SetValue(AnimationDurationProperty, value); }
        }

        /// <summary>
        /// Gets or sets the Minimum value the gauge will accept, values lower than this are clamped to this.
        /// </summary>
        public double Minimum
        {
            get { return (double)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        /// <summary>
        /// Gets or sets the maximum value the gauge will accept. Values above this are clamped to this.
        /// </summary>
        public double Maximum
        {
            get { return (double)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        /// <summary>
        /// Gets or sets the face text of the gauge.
        /// </summary>
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        /// <summary>
        /// A collection of Dial360Notch instances which will be data-bound as Notches on the dial.
        /// </summary>
        public IEnumerable<Dial360Notch> Notches
        {
            get { return (IEnumerable<Dial360Notch>)GetValue(NotchesProperty); }
            set { SetValue(NotchesProperty, value); }
        }

        /// <summary>
        /// Gets or sets the "default" notch count. 
        /// This is the number of notches generated if no notches are provided via the Notches property.
        /// </summary>
        public int DefaultNotchCount
        {
            get { return (int)GetValue(DefaultNotchCountProperty); }
            set { SetValue(DefaultNotchCountProperty, value); }
        }

        /// <summary>
        /// Gets the RealMinimum value. Since the user may set Minimum &gt; Maximum, we internally use RealMaximum which returns the largest of the two values.
        /// </summary>
        internal double RealMaximum
        {
            get { return (Maximum > Minimum) ? Maximum : Minimum; }
        }

        /// <summary>
        /// Gets the RealMaximum value. Since the user may set Minimum &gt; Maximum, we internally use RealMinimum which returns the smallest of the two values
        /// </summary>
        internal double RealMinimum
        {
            get { return (Maximum < Minimum) ? Maximum : Minimum; }
        }

        /// <summary>
        /// Gets a normalized Value which is clamped to Minimum and Maximum and then moved into the range 0.0 &lt;= n &lt;= 1.0.
        /// </summary>
        internal double NormalizedValue
        {
            get
            {
                double value = Value, max = RealMaximum, min = RealMinimum;

                //Clamp
                value = (value > max) ? max :
                        (value < min) ? min : value;

                //Move into 0.0 - 1.0 scale
                return (value - min) / (max - min);
            }
        }

        /// <summary>
        /// Animates the needle to the current Value.
        /// </summary>
        protected void Animate()
        {
            //Calculate the needle position...
            double angle = _minAngle + (NormalizedValue * (_maxAngle - _minAngle));

            //Update the animation
            _needleStoryboard.Stop(this);

            _needleAnimation.From = _needleTransform.Angle; //Current angle of needle
            _needleAnimation.To = angle;
            _needleAnimation.Duration = AnimationDuration;

            _needleStoryboard.Begin(this, true);
        }
    }
}
