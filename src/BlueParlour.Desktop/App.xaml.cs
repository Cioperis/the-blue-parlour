using System.IO;
using System.Windows;
using BlueParlour.Application;
using BlueParlour.Infrastructure;

namespace BlueParlour.Desktop;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args is ["--smoke-test", var output])
        {
            try { SmokeTest.Run(output); Shutdown(0); }
            catch (Exception error) { Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "failure.txt"), error.ToString()); Shutdown(1); }
            return;
        }
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheBlueParlour", "progress.json");
        var game = new ParlourGame(new JsonProgressStore(path), Random.Shared);
        MainWindow = new MainWindow { DataContext = new MainViewModel(game) };
        MainWindow.Show();
    }
}
