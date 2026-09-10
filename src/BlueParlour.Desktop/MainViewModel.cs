using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using BlueParlour.Application;
using BlueParlour.Domain;

namespace BlueParlour.Desktop;

public sealed class RelayCommand(Action<object?> execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) { if (CanExecute(parameter)) execute(parameter); }
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ParlourGame game;
    private bool playing, feedback, summary;
    private bool opera, quiet, customize;
    private Prompt? shown;
    private string message = "Choose a small ritual. There is no timer, and every finished sitting earns a rose.";
    public MainViewModel(ParlourGame game)
    {
        this.game = game;
        Opera = new(Random.Shared);
        Quiet = new();
        StartOpera = new(_ => { ClearScreens(); opera = true; Opera.Replay.Execute(null); Refresh(); });
        StartQuiet = new(_ => { ClearScreens(); quiet = true; Quiet.Start(); Refresh(); });
        StartInk = new(_ => Start(AttentionRule.Ink));
        StartWord = new(_ => Start(AttentionRule.Word));
        StartShift = new(_ => Start(AttentionRule.Shift));
        StartCustomize = new(_ => { ClearScreens(); customize = true; Refresh(); });
        SetPalette = new(value => SavePreferences(Enum.Parse<ParlourPalette>((string)value!), game.Progress.Companion, game.Progress.FocusDepth));
        SetCompanion = new(value => SavePreferences(game.Progress.Palette, Enum.Parse<ParlourCompanion>((string)value!), game.Progress.FocusDepth));
        SetDepth = new(value => SavePreferences(game.Progress.Palette, game.Progress.Companion, Enum.Parse<FocusDepth>((string)value!)));
        Answer = new(p => Respond(Enum.Parse<Pigment>((string)p!)), () => playing && !feedback);
        Next = new(_ => Advance(), () => feedback);
        Home = new(_ => { ClearScreens(); message = "The kettle is on. Stay a little longer."; Refresh(); });
        RetrySave = new(_ => SaveReward());
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public RelayCommand StartInk { get; }
    public RelayCommand StartWord { get; }
    public RelayCommand StartShift { get; }
    public RelayCommand StartCustomize { get; }
    public RelayCommand SetPalette { get; }
    public RelayCommand SetCompanion { get; }
    public RelayCommand SetDepth { get; }
    public RelayCommand Answer { get; }
    public RelayCommand Next { get; }
    public RelayCommand Home { get; }
    public RelayCommand RetrySave { get; }
    public OperaViewModel Opera { get; }
    public QuietViewModel Quiet { get; }
    public RelayCommand StartOpera { get; }
    public RelayCommand StartQuiet { get; }
    public bool IsOpera => opera;
    public bool IsQuiet => quiet;
    public bool IsCustomize => customize;
    public bool IsHome => !playing && !summary && !opera && !quiet && !customize;
    public bool IsPlaying => playing;
    public bool IsSummary => summary;
    public bool HasFeedback => feedback;
    public bool SaveFailed { get; private set; }
    public bool HasRose => game.Progress.Roses >= 1;
    public bool HasCat => game.Progress.Roses >= 2;
    public bool HasPainting => game.Progress.Roses >= 3;
    public bool IsCat => game.Progress.Companion == ParlourCompanion.Cat;
    public bool IsPug => game.Progress.Companion == ParlourCompanion.Pug;
    public bool IsCorgi => game.Progress.Companion == ParlourCompanion.Corgi;
    public string PaletteName => game.Progress.Palette switch { ParlourPalette.RoseVelvet => "Rose velvet", ParlourPalette.SageGlass => "Sage glass", _ => "Midnight blue" };
    public string CompanionName => game.Progress.Companion.ToString();
    public string CornerTitle => $"YOUR {CompanionName.ToUpperInvariant()} CORNER";
    public string DepthName => game.Progress.FocusDepth == FocusDepth.Deep ? "Deep · 24/36 cards" : "Focused · 16/18 cards";
    public string Background => game.Progress.Palette switch { ParlourPalette.RoseVelvet => "#2B1923", ParlourPalette.SageGlass => "#19302E", _ => "#102C44" };
    public string SidePanel => game.Progress.Palette switch { ParlourPalette.RoseVelvet => "#573044", ParlourPalette.SageGlass => "#315650", _ => "#214F70" };
    public string Surface => game.Progress.Palette switch { ParlourPalette.RoseVelvet => "#FBF2F3", ParlourPalette.SageGlass => "#F1F5F0", _ => "#F7F2E8" };
    public string Accent => game.Progress.Palette switch { ParlourPalette.RoseVelvet => "#91445D", ParlourPalette.SageGlass => "#48756B", _ => "#28628E" };
    public string AccentSoft => game.Progress.Palette switch { ParlourPalette.RoseVelvet => "#F0DDE3", ParlourPalette.SageGlass => "#DCEAE4", _ => "#DDEAF1" };
    public string Collection => $"{game.Progress.Roses} / 3 parlour keepsakes";
    public string CollectionNote => game.Progress.Roses switch { 0 => "A rose, a quiet companion, a tiny painting.\nFinish a sitting to make this room yours.", 1 => "A rose for the windowsill.\nAnother sitting invites your companion.", 2 => "Someone has found the velvet cushion.\nOne more sitting completes your painting.", _ => "Your little parlour is complete.\nReturn whenever you fancy a quiet ritual." };
    public string Message => message;
    public string Word => shown?.Word.ToString().ToUpperInvariant() ?? "BLUE";
    public string Ink => shown?.Ink switch { Pigment.Rose => "#94364B", Pigment.Green => "#35715C", Pigment.Gold => "#926715", _ => "#215B9A" };
    public string Rule
    {
        get
        {
            if (game.Session is not { } session) return "";
            var source = session.CurrentRule == AttentionRule.Word ? "word" : "ink colour";
            if (!session.UsesOneBack) return $"{source.ToUpperInvariant()} spotlight — choose the {source}.";
            return session.Answered == 0 ? $"ANCHOR CARD — choose its {source}." : $"ONE-BACK — choose the previous card's {source}.";
        }
    }
    public string Counter => $"BRUSHSTROKE {Math.Min((game.Session?.Answered ?? 0) + (feedback ? 0 : 1), game.Session?.Total ?? 12):00} / {game.Session?.Total ?? 12}";
    public double ProgressPercent => (game.Session?.Answered ?? 0) / (double)(game.Session?.Total ?? 12) * 100;
    public string NextLabel => game.Session?.Complete == true ? "Finish this sitting  →" : "Next brushstroke  →";
    public string Result => $"{game.Session?.Correct} of {game.Session?.Total} colours found";
    public string Comparison
    {
        get
        {
            var answers = game.Session?.Answers;
            return answers is null ? "" : $"Matching: {answers.Count(a => a.Prompt.IsCongruent && a.Correct)} / {answers.Count(a => a.Prompt.IsCongruent)}     ·     Conflicting: {answers.Count(a => !a.Prompt.IsCongruent && a.Correct)} / {answers.Count(a => !a.Prompt.IsCongruent)}";
        }
    }
    private void Start(AttentionRule rule)
    {
        ClearScreens();
        shown = game.Start(rule).Current;
        playing = true; feedback = summary = SaveFailed = false;
        message = rule == AttentionRule.Shift ? "Watch the spotlight: the rule will change. Select a paint below, or use keys 1–4." : "The first card is your anchor. After that, answer from the card you just saw.";
        Refresh();
    }
    private void ClearScreens() => playing = feedback = summary = opera = quiet = customize = false;
    private void Respond(Pigment choice)
    {
        var target = game.Session!.Expected;
        var result = game.Session.Submit(choice);
        feedback = true;
        message = result.Correct ? "Held in mind. The next card may try to pull your attention elsewhere." : $"The answer held from before was {target.ToString().ToLowerInvariant()}. Let this card become the next memory.";
        Refresh();
    }
    private void Advance()
    {
        feedback = false;
        if (game.Session!.Complete)
        {
            playing = false; summary = true;
            SaveReward();
        }
        else
        {
            shown = game.Session.Current;
            message = game.Session.UsesOneBack ? "This new card is visible; answer from the card before it." : "A fresh brushstroke. Follow the spotlight above the card.";
        }
        Refresh();
    }
    private void SavePreferences(ParlourPalette palette, ParlourCompanion companion, FocusDepth depth)
    {
        try { game.Customize(palette, companion, depth); message = "Your parlour changed with you. The choice is saved."; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { message = "The new look could not be saved just now. Try the choice once more."; }
        Refresh();
    }
    private void SaveReward()
    {
        try { game.CollectRose(); SaveFailed = false; message = "A sitting well spent. Your keepsake has been saved."; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        { SaveFailed = true; message = "Your sitting is complete, but the keepsake could not be saved. You can retry below."; }
        Refresh();
    }
    private void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        Answer.Refresh(); Next.Refresh();
    }
}
