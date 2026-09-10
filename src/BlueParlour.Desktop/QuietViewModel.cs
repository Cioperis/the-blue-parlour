using System.ComponentModel;

namespace BlueParlour.Desktop;

public enum QuietPath { Notice, GentleVoice, OpenEnding }

/// <summary>Optional, untimed prompts. The selected path and private reflections are never stored.</summary>
public sealed class QuietViewModel : INotifyPropertyChanged
{
    private int step;
    private QuietPath? path;
    private static readonly IReadOnlyDictionary<QuietPath, (string[] Titles, string[] Prompts)> Paths =
        new Dictionary<QuietPath, (string[], string[])>
        {
            [QuietPath.Notice] = (
                ["Arrive where you are.", "Find one steady thing.", "Let the room hold you.", "This moment is complete."],
                [
                    "Choose one colour nearby and let your eyes rest there. There is nothing to solve.",
                    "Notice the support of your chair or the floor. Breathe in your usual comfortable way—no count, hold, or special rhythm.",
                    "Name three ordinary details in the room: a shape, a texture, and a sound. Ordinary is enough.",
                    "Stay a little, choose another path, or return to the parlour. You do not need to feel different to finish."
                ]),
            [QuietPath.GentleVoice] = (
                ["Begin with what is true.", "Speak as you would to someone dear.", "Keep the whole picture.", "Carry the kinder sentence."],
                [
                    "Recall one thing you met today, however ordinary. Showing up counts even when the result was unfinished.",
                    "If someone you cared for had your exact day, what warm and honest sentence would you offer them? You may borrow it for yourself.",
                    "A difficult moment is one part of a day, not a verdict on the person living it. Let effort, care, and recovery belong in the picture too.",
                    "Choose one sentence worth keeping. Nothing needs to be typed, proved, or earned here."
                ]),
            [QuietPath.OpenEnding] = (
                ["Set down the whole map.", "Choose one small circle.", "Leave a door unopened.", "Enough for this chapter."],
                [
                    "Imagine every unanswered thing written on separate cards. Choose only one card that truly needs your attention now.",
                    "For that one card, pick the smallest useful next step. It can be as small as deciding when to look again.",
                    "Let one other card remain unanswered on purpose. An open question can wait without becoming a failure.",
                    "The next step is chosen; the rest may stay unwritten. Return whenever another small circle feels useful."
                ])
        };

    public QuietViewModel()
    {
        ChoosePath = new(value => { path = Enum.Parse<QuietPath>((string)value!); step = 0; Refresh(); });
        BackToPaths = new(_ => { path = null; step = 0; Refresh(); });
        Next = new(_ => { if (step < 3) step++; Refresh(); }, () => path is not null && step < 3);
        Restart = new(_ => { step = 0; Refresh(); });
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public RelayCommand ChoosePath { get; }
    public RelayCommand BackToPaths { get; }
    public RelayCommand Next { get; }
    public RelayCommand Restart { get; }
    public bool IsChoosing => path is null;
    public bool IsMoment => path is not null;
    public string Title => path is null ? "What would feel useful?" : Paths[path.Value].Titles[step];
    public string Prompt => path is null ? "" : Paths[path.Value].Prompts[step];
    public string Counter => path is null ? "CHOOSE YOUR OWN PATH" : step == 3 ? "A MOMENT FOR YOURSELF" : $"OPTIONAL MOMENT {step + 1} OF 3";
    public bool Complete => path is not null && step == 3;
    public bool NotComplete => path is not null && !Complete;
    public void Start() { path = null; step = 0; Refresh(); }
    private void Refresh() { PropertyChanged?.Invoke(this, new(null)); Next.Refresh(); }
}
