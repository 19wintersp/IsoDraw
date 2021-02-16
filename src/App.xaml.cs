using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Threading;
using SystemThreading = System.Threading;

namespace WPFTest
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += (object s, DispatcherUnhandledExceptionEventArgs r) =>
            {
                try
                {
                    r.Handled = true;
                    string dat = "IsoDraw Exception details\n\n" + r.Exception.Message + "\n\n" + r.Exception.StackTrace;
                    System.IO.File.WriteAllText(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\isodrawexception.txt", dat);
                    MessageBoxResult restart = MessageBox.Show("Unfortunately, an unrecoverable error occurred. The full details of the exception have been dumped to %appdata%. Would you like to attempt a restart?", "Fatal exception", MessageBoxButton.YesNo, MessageBoxImage.Error);
                    if (restart == MessageBoxResult.Yes)
                    {
                        System.Diagnostics.Process.Start(ResourceAssembly.Location);
                    }
                    ((MainWindow)MainWindow).forceKill = true;
                    Shutdown();
                } catch
                {
                    MessageBox.Show("exception handler go oof.\n\nPlease terminate the application manually: it has been placed into a non-terminating (hung) state.", "crap, that shouldnt happen", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None, MessageBoxOptions.ServiceNotification);
                    while (true) { SystemThreading.Thread.Sleep(500); }
                }
            };
            List<string> args = new List<string>(e.Args);
            this.Properties.Add("testprop", "testval");
        }
    }
}
