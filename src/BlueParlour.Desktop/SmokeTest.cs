using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using BlueParlour.Application;

namespace BlueParlour.Desktop;

/// <summary>Opt-in, offscreen integration test; never touches the player's save.</summary>
internal static class SmokeTest
{
    public static void Run(string output)
    {
        Directory.CreateDirectory(output);
        var game = new ParlourGame(new MemoryStore(), new Random(42));
        var vm = new MainViewModel(game);
        var window = new MainWindow { DataContext = vm };
        Capture(window, output, "home");
        vm.StartInk.Execute(null);
        Capture(window, output, "play");
        for (var sitting = 0; sitting < 3; sitting++)
        {
            if (sitting > 0) vm.StartWord.Execute(null);
            while (!game.Session!.Complete)
            {
                vm.Answer.Execute(game.Session.Expected.ToString());
                if (vm.Answer.CanExecute("Blue")) throw new InvalidOperationException("Duplicate answer allowed.");
                vm.Next.Execute(null);
            }
            if (!vm.IsSummary || game.Session!.Correct != game.Session.Total) throw new InvalidOperationException("Summary transition failed.");
        }
        if (!vm.HasRose || !vm.HasCat || !vm.HasPainting) throw new InvalidOperationException("Keepsake progression failed.");
        Capture(window, output, "summary");
        vm.Home.Execute(null);
        Capture(window, output, "collected");
        vm.StartShift.Execute(null);
        while (!game.Session!.Complete)
        {
            vm.Answer.Execute(game.Session.Expected.ToString()); vm.Next.Execute(null);
        }
        if (!vm.IsSummary || game.Session.Correct != 18) throw new InvalidOperationException("Shifting spotlight failed.");
        vm.Home.Execute(null);
        vm.StartOpera.Execute(null);
        if (!vm.IsOpera || vm.IsHome || vm.Answer.CanExecute("Blue")) throw new InvalidOperationException("Opera navigation failed.");
        Capture(window, output, "opera");
        for (var act = 0; act < 3; act++)
        {
            foreach (var pair in vm.Opera.Cards.GroupBy(card => card.Motif))
                foreach (var card in pair) card.Choose.Execute(null);
            if (!vm.Opera.Complete) throw new InvalidOperationException("Opera completion failed.");
            if (act < 2) vm.Opera.NextAct.Execute(null);
        }
        Capture(window, output, "opera-complete");
        vm.Opera.Replay.Execute(null);
        if (vm.Opera.Complete || vm.Opera.Cards.Count != 8) throw new InvalidOperationException("Opera encore did not reset.");
        var different = vm.Opera.Cards.GroupBy(card => card.Motif).Select(group => group.First()).Take(2).ToArray();
        different[0].Choose.Execute(null); different[1].Choose.Execute(null);
        if (!vm.Opera.AwaitingCurtain || different[0].Choose.CanExecute(null)) throw new InvalidOperationException("Opera mismatch gating failed.");
        Capture(window, output, "opera-mismatch");
        vm.Opera.Continue.Execute(null);
        if (vm.Opera.AwaitingCurtain) throw new InvalidOperationException("Opera curtain failed.");
        vm.Home.Execute(null);
        vm.StartQuiet.Execute(null);
        Capture(window, output, "quiet-paths");
        vm.Quiet.ChoosePath.Execute("GentleVoice");
        Capture(window, output, "quiet");
        for (var i = 0; i < 3; i++) vm.Quiet.Next.Execute(null);
        if (!vm.Quiet.Complete || vm.Quiet.Next.CanExecute(null)) throw new InvalidOperationException("Quiet moment completion failed.");
        Capture(window, output, "quiet-complete");
        vm.Quiet.Restart.Execute(null);
        if (vm.Quiet.Complete) throw new InvalidOperationException("Quiet moment reset failed.");
        vm.Quiet.BackToPaths.Execute(null);
        if (!vm.Quiet.IsChoosing) throw new InvalidOperationException("Quiet path navigation failed.");
        vm.Home.Execute(null);
        vm.StartCustomize.Execute(null);
        vm.SetPalette.Execute("RoseVelvet");
        vm.SetCompanion.Execute("Pug");
        vm.SetDepth.Execute("Deep");
        if (!vm.IsCustomize || !vm.IsPug || vm.PaletteName != "Rose velvet" || !vm.DepthName.StartsWith("Deep")) throw new InvalidOperationException("Customization failed.");
        Capture(window, output, "customize");
        vm.SetPalette.Execute("SageGlass");
        vm.SetCompanion.Execute("Corgi");
        Capture(window, output, "customize-sage");
        if (window.Icon is null) throw new InvalidOperationException("Window icon is missing.");
        vm.Home.Execute(null);
        // Verify scaled window layout at minimum size.
        window.Width = 820; window.Height = 600;
        Capture(window, output, "small");
        vm.StartOpera.Execute(null);
        Capture(window, output, "opera-small");
        File.WriteAllText(Path.Combine(output, "success.txt"), "One-back attention sittings, shifting rules, three opera acts, mismatch curtain, encore, reflection paths, customization, icon, input gating, and fourteen WPF renders passed.");
        window.Close();
    }
    private static void Capture(MainWindow window, string output, string name)
    {
        var visual = (FrameworkElement)window.Content;
        visual.Measure(new Size(window.Width - 24, window.Height - 40));
        visual.Arrange(new Rect(0, 0, window.Width - 24, window.Height - 40));
        visual.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)window.Width - 24, (int)window.Height - 40, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(output, name + ".png")); png.Save(file);
    }
    private sealed class MemoryStore : IProgressStore
    {
        public Progress Load() => new();
        public void Save(Progress progress) { }
    }
}
