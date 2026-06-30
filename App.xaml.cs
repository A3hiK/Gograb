using System.Configuration;
using System.Data;
using System.Windows;

namespace EasyDL;

public partial class App : Application
{
    private void OnStartup(object sender, StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            MessageBox.Show($"Error: {args.Exception.Message}\n\n{args.Exception}", "Gograb", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}

