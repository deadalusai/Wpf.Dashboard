using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Dashboard.Test.Annotations;

namespace Dashboard.Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();

            ViewModel = new DialViewModel {
                Max = 100,
                Min = 50,
                Value = 75,
                Notches = {
                    new Dial360Notch(label: "A", angle: -150),
                    new Dial360Notch(label: "B", angle: 150)
                }
            };

            _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.ApplicationIdle, OnTick, Dispatcher);

            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            _timer.Start();

            await Task.Delay(2000);

            //Test altering the Notches collection
            ViewModel.Notches.Add(new Dial360Notch(label: "C", angle: 0));

            await Task.Delay(2000);

            //Test swapping the Notches collection
            ViewModel.Notches = new ObservableCollection<Dial360Notch> {
                
                new Dial360Notch(label: "First", angle: -100),
                new Dial360Notch(label: "Last", angle: 100)

            };
        }

        private void OnTick(object sender, EventArgs e)
        {
            var value = ViewModel.Value + 2.15;

            if (value > ViewModel.Max + 5)
            {
                value = ViewModel.Min - 5;
            }

            ViewModel.Value = value;
        }

        public DialViewModel ViewModel
        {
            get { return (DialViewModel)DataContext; }
            set { DataContext = value; }
        }
    }

    public class DialViewModel : INotifyPropertyChanged
    {
        private double _value;
        private double _max;
        private double _min;
        private ObservableCollection<Dial360Notch> _notches = new ObservableCollection<Dial360Notch>();

        public double Min
        {
            get { return _min; }
            set
            {
                if (value.Equals(_min)) return;
                _min = value;
                OnPropertyChanged();
            }
        }

        public double Max
        {
            get { return _max; }
            set
            {
                if (value.Equals(_max)) return;
                _max = value;
                OnPropertyChanged();
            }
        }

        public double Value
        {
            get { return _value; }
            set
            {
                if (value.Equals(_value)) return;
                _value = value;
                OnPropertyChanged();
                OnPropertyChanged("LabelValue");
            }
        }

        public string LabelValue
        {
            get { return (Math.Round(Value)).ToString(CultureInfo.InvariantCulture); }
        }

        public ObservableCollection<Dial360Notch> Notches
        {
            get { return _notches; }
            set
            {
                if (Equals(value, _notches)) return;
                _notches = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
