using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPFTest
{
    /// <summary>
    /// Interaction logic for ViewSetup.xaml
    /// </summary>
    public partial class ViewSetup : Window
    {
        public event EventHandler<EventArgs> DialogClose;
        public event EventHandler<ViewSettingsEventArgs> DialogSubmit;

        Color selcol, delcol;
        int linthk, pntthk;

        public ViewSetup(Color selectionColour, Color deletionColour, int lineThickness, int pointThickness)
        {
            InitializeComponent();
            selcol = selectionColour;
            delcol = deletionColour;
            linthk = lineThickness;
            pntthk = pointThickness;
        }

        private void WindowLoad(object sender, RoutedEventArgs e)
        {
            UpdateUi();
        }
        private void UpdateUi()
        {
            if (!IsLoaded) return;
            ((DockPanel)FindName("SelcolPreview")).Background = new SolidColorBrush(selcol);
            ((DockPanel)FindName("DelcolPreview")).Background = new SolidColorBrush(delcol);
            ((Slider)FindName("LineThicknessSlider")).Value = linthk;
            ((Slider)FindName("PointThicknessSlider")).Value = pntthk;
            ((TextBlock)FindName("LineThicknessValue")).Text = linthk.ToString();
            ((TextBlock)FindName("PointThicknessValue")).Text = pntthk.ToString();
        }

        private void ValueChange(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return;
            linthk = (int)((Slider)FindName("LineThicknessSlider")).Value;
            pntthk = (int)((Slider)FindName("PointThicknessSlider")).Value;
            UpdateUi();
        }

        private void DelcolChoose(object sender, RoutedEventArgs e)
        {
            ColourChooser dcc = new ColourChooser(delcol.R, delcol.G, delcol.B);
            dcc.ColourChosen += (object s, ColourChooser.ColourChooserClosedEventArgs c) =>
            {
                delcol = Color.FromRgb(c.red, c.green, c.blue);
                UpdateUi();
            };
            dcc.ShowDialog();
        }
        private void SelcolChoose(object sender, RoutedEventArgs e)
        {
            ColourChooser scc = new ColourChooser(selcol.R, selcol.G, selcol.B);
            scc.ColourChosen += (object s, ColourChooser.ColourChooserClosedEventArgs c) =>
            {
                selcol = Color.FromRgb(c.red, c.green, c.blue);
                UpdateUi();
            };
            scc.ShowDialog();
        }

        private void DialogCloseBeh(object sender, RoutedEventArgs e)
        {
            Close();
            OnDialogClose(new EventArgs());
        }
        private void DialogSubmitBeh(object sender, RoutedEventArgs e)
        {
            Close();
            OnDialogSubmit(new ViewSettingsEventArgs() {
                selectionColour = selcol,
                deletionColour = delcol,
                lineThickness = linthk,
                pointThickness = pntthk
            });
        }

        private void OnDialogClose(EventArgs e)
        {
            DialogClose?.Invoke(this, e);
        }
        private void OnDialogSubmit(ViewSettingsEventArgs e)
        {
            DialogSubmit?.Invoke(this, e);
        }

        public class ViewSettingsEventArgs : EventArgs
        {
            public Color selectionColour, deletionColour;
            public int lineThickness, pointThickness;
        }
    }
}
