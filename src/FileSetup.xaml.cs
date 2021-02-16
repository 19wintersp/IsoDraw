using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFTest
{
    /// <summary>
    /// Interaction logic for FileSetup.xaml
    /// </summary>
    public partial class FileSetup : Window
    {
        public event EventHandler<EventArgs> DialogClose;
        public event EventHandler<FileSettingsEventArgs> DialogSubmit;
        private FileSettingsEventArgs initData;

        public FileSetup(DrawingType type, Size size, int subDivision)
        {
            InitializeComponent();
            initData = new FileSettingsEventArgs()
            {
                size = size,
                type = type,
                stepDivision = subDivision
            };
        }

        private void WindowLoad(object sender, RoutedEventArgs e)
        {
            SubdivisionSlider.Value = initData.stepDivision;
            SubdivisionValue.Text = initData.stepDivision.ToString();
            DrawingTypeCB.SelectedIndex = initData.type == DrawingType.ISOMETRIC ? 1 : 0;
            SizeHeight.Text = initData.size.Height.ToString();
            SizeWidth.Text = initData.size.Width.ToString();
        }

        private void DialogCloseBeh(object sender, RoutedEventArgs e)
        {
            Close();
            OnDialogClose(new EventArgs());
        }
        private void DialogSubmitBeh(object sender, RoutedEventArgs e)
        {
            try
            {
                int sDiv = (int)Math.Round(((Slider)FindName("SubdivisionSlider")).Value);
                Size isz = new Size(
                    double.Parse(((TextBox)FindName("SizeWidth")).Text),
                    double.Parse(((TextBox)FindName("SizeHeight")).Text)
                );
                DrawingType dt = ((ComboBox)FindName("DrawingTypeCB")).SelectedIndex == 0 ? DrawingType.ORTHOGRAPHIC : DrawingType.ISOMETRIC;

                Close();
                OnDialogSubmit(new FileSettingsEventArgs()
                {
                    size = isz,
                    stepDivision = sDiv,
                    type = dt
                });
            } catch
            {
                MessageBox.Show("Please ensure that all values are valid.", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
        }

        private void ValueChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            SubdivisionValue.Text = ((int)e.NewValue).ToString();
        }

        private void OnDialogClose(EventArgs e)
        {
            DialogClose?.Invoke(this, e);
        }
        private void OnDialogSubmit(FileSettingsEventArgs e)
        {
            DialogSubmit?.Invoke(this, e);
        }

        public class FileSettingsEventArgs : EventArgs
        {
            public DrawingType type;
            public Size size;
            public int stepDivision;
        }
    }
}
