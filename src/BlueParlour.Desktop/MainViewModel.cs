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
    private bool opera, quiet;
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
        Answer = new(p => Respond(Enum.Parse<Pigment>((string)p!)), () => playing && !feedback);
        Next = new(_ => Advance(), () => feedback);
        Home = new(_ => { ClearScreens(); message = "The kettle is on. Stay a little longer."; Refresh(); });
        RetrySave = new(_ => SaveReward());
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public RelayCommand StartInk { get; }
    public RelayCommand StartWord { get; }
    public RelayCommand StartShift { get; }
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
    public bool IsHome => !playing && !summary && !opera && !quiet;
    public bool IsPlaying => playing;
    public bool IsSummary => summary;
    public bool HasFeedback => feedback;
    public bool SaveFailed { get; private set; }
    public bool HasRose => game.Progress.Roses >= 1;
    public bool HasCat => game.Progress.Roses >= 2;
    public bool HasPainting => game.Progress.Roses >= 3;
    public string Collection => $"{game.Progress.Roses} / 3 parlour keepsakes";
    public string CollectionNote => game.Progress.Roses switch { 0 => "A rose, a resident cat, a tiny painting.\nFinish a sitting to make this room yours.", 1 => "A rose for the windowsill.\nAnother sitting invites a quiet companion.", 2 => "Someone has found the velvet cushion.\nOne more sitting completes your painting.", _ => "Your little parlour is complete.\nReturn whenever you fancy a quiet ritual." };
    public string Message => message;
    public string Word => shown?.Word.ToString().ToUpperInvariant() ?? "BLUE";
    public string Ink => shown?.Ink switch { Pigment.Rose => "#94364B", Pigment.Green => "#35715C", Pigment.Gold => "#926715", _ => "#215B9A" };
    public string Rule => game.Session?.CurrentRule == AttentionRule.Word ? "WORD spotlight — read the word. Ignore its ink." : "INK spotlight — choose the ink colour. Ignore the word.";
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
        message = rule == AttentionRule.Shift ? "Watch the spotlight: the rule will change. Select a paint below, or use keys 1–4." : "Take your time. Select a paint below, or use keys 1–4.";
        Refresh();
    }
    private void ClearScreens() => playing = feedback = summary = opera = quiet = false;
    private void Respond(Pigment choice)
    {
        var activeRule = game.Session!.CurrentRule;
        var result = game.Session.Submit(choice);
        feedback = true;
        var target = activeRule == AttentionRule.Ink ? result.Prompt.Ink : result.Prompt.Word;
        message = result.Correct ? "Lovely. A little moment of attention, caught in colour." : $"This one was {target.ToString().ToLowerInvariant()}. The word and its colour can pull in different directions.";
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
            message = "A fresh brushstroke. Follow the instruction above the card.";
        }
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
