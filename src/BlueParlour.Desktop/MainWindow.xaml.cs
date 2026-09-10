using System.Windows;
using System.Windows.Input;

namespace BlueParlour.Desktop;
public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm || e.IsRepeat) return;
        if (e.Key == Key.Escape && !vm.IsHome) { vm.Home.Execute(null); e.Handled = true; return; }
        var pigment = e.Key switch
        {
            Key.D1 or Key.NumPad1 => "Blue", Key.D2 or Key.NumPad2 => "Rose",
            Key.D3 or Key.NumPad3 => "Green", Key.D4 or Key.NumPad4 => "Gold", _ => null
        };
        if (pigment is not null && vm.Answer.CanExecute(pigment)) { vm.Answer.Execute(pigment); e.Handled = true; }
        else if (e.Key == Key.Enter && vm.Next.CanExecute(null)) { vm.Next.Execute(null); e.Handled = true; }
    }
}
