using System.IO;
using System.Reflection;
using System.Windows;
using log4net.Config;

namespace IBDTools {
    /// <summary>
    ///     Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application {
        private void App_OnStartup(object sender, StartupEventArgs e) {
            XmlConfigurator.Configure(new FileInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "log4net.config")));
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}