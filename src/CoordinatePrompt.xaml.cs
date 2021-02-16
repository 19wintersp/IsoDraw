using System;
using System.Windows;
using System.Windows.Controls;

namespace WPFTest
{
    /// <summary>
    /// Interaction logic for CoordinatePrompt.xaml
    /// </summary>
    public partial class CoordinatePrompt : Window
    {
        public event EventHandler<CoordinateEventArgs> PromptSubmit;
        public event EventHandler<EventArgs> PromptClosed;
        private string title = "CoordinatePrompt", prompt = "{ERROR prompt not defined}";

        public CoordinatePrompt(string title, string prompt)
        {
            this.title = title;
            this.prompt = prompt;
            Init();
        }
        public CoordinatePrompt()
        {
            Init();
        }
        public void Init() { 
            InitializeComponent();
        }

        private void WindowLoad(object sender, RoutedEventArgs e)
        {
            Title = title;
            ((TextBlock)FindName("UserPrompt")).Text = prompt;
        }

        private void DialogClose(object sender, RoutedEventArgs e)
        {
            Close();
            OnPromptClosed(new EventArgs());
        }
        private void DialogSubmit(object sender, RoutedEventArgs e)
        {
            try
            {
                float xValue = float.Parse(XValue.Text);
                float yValue = float.Parse(YValue.Text);
                Close();
                OnPromptSubmit(new CoordinateEventArgs()
                {
                    x = xValue,
                    y = yValue
                });
            } catch(FormatException)
            {
                MessageBoxResult ep = MessageBox.Show("Please enter numerical values.", "Error", MessageBoxButton.OKCancel, MessageBoxImage.Error);
                if (ep == MessageBoxResult.Cancel) DialogClose(sender, e);
            }
        }

        public virtual void OnPromptSubmit(CoordinateEventArgs e)
        {
            PromptSubmit?.Invoke(this, e);
        }
        public virtual void OnPromptClosed(EventArgs e)
        {
            PromptClosed?.Invoke(this, e);
        }

        public class CoordinateEventArgs : EventArgs
        {
            public float x, y;
        }
    }
}
