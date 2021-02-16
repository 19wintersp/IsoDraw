using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WPFTest
{
    /// <summary>
    /// Interaction logic for ColourChooser.xaml
    /// </summary>
    public partial class ColourChooser : Window
    {
        public event EventHandler<ColourChooserClosedEventArgs> ColourChosen;
        public event EventHandler<EventArgs> ChooserClosed;
        public byte redValue = 0, greenValue = 0, blueValue = 0;
        private bool loaded = false;
        private ColourChooserDialogStatus DialogStatus = ColourChooserDialogStatus.WAITING;

        public ColourChooser()
        {
            Init();
        }
        public ColourChooser(byte redValue, byte greenValue, byte blueValue)
        {
            this.redValue = redValue;
            this.greenValue = greenValue;
            this.blueValue = blueValue;
            Init();
        }
        private void Init()
        {
            if (DialogStatus == ColourChooserDialogStatus.CLOSED) return;
            InitializeComponent();
        }

        private void WindowLoad(object sender, RoutedEventArgs e)
        {
            ((Slider)FindName("RedSlider")).Value = redValue;
            ((Slider)FindName("GreenSlider")).Value = greenValue;
            ((Slider)FindName("BlueSlider")).Value = blueValue;
            UpdateUi();
            loaded = true;
        }
        private void ValueChange(object sender, RoutedPropertyChangedEventArgs<double> e) { ValueChange(); }
        private void ValueChange()
        {
            if (!loaded) return;
            redValue = (byte)(int)((Slider)FindName("RedSlider")).Value;
            greenValue = (byte)(int)((Slider)FindName("GreenSlider")).Value;
            blueValue = (byte)(int)((Slider)FindName("BlueSlider")).Value;
            UpdateUi();
        }

        private void UpdateUi()
        {
            ((TextBlock)FindName("RedVal")).Text = redValue.ToString();
            ((TextBlock)FindName("GreenVal")).Text = greenValue.ToString();
            ((TextBlock)FindName("BlueVal")).Text = blueValue.ToString();
            ((DockPanel)FindName("Preview")).Background = new SolidColorBrush(Color.FromRgb(redValue, greenValue, blueValue));
        }

        private void DialogClose(object sender, RoutedEventArgs e)
        {
            this.Close();
            DialogStatus = ColourChooserDialogStatus.CLOSED;
            EventArgs args = new EventArgs();
            OnChooserClosed(args);
        }
        private void DialogChoose(object sender, RoutedEventArgs e)
        {
            this.Close();
            DialogStatus = ColourChooserDialogStatus.CHOSEN;
            ColourChooserClosedEventArgs args = new ColourChooserClosedEventArgs()
            {
                red = redValue,
                green = greenValue,
                blue = blueValue,
                colour = System.Drawing.Color.FromArgb(redValue, greenValue, blueValue)
            };
            OnColourChosen(args);
        }

        protected virtual void OnColourChosen(ColourChooserClosedEventArgs e)
        {
            ColourChosen?.Invoke(this, e);
        }
        protected virtual void OnChooserClosed(EventArgs e)
        {
            ChooserClosed?.Invoke(this, e);
        }

        private enum ColourChooserDialogStatus
        {
            WAITING,
            CLOSED,
            CHOSEN
        }

        public class ColourChooserClosedEventArgs : EventArgs
        {
            public byte red, green, blue;
            public System.Drawing.Color colour;
        }
    }
}
