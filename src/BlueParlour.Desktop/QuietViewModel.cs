using System.ComponentModel;

namespace BlueParlour.Desktop;

/// <summary>Optional, untimed prompts, with no stored personal responses or scores.</summary>
public sealed class QuietViewModel : INotifyPropertyChanged
{
    private int step;
    private static readonly string[] Titles = ["Arrive where you are.", "Make a little room.", "Just the next small thing.", "This can be enough."];
    private static readonly string[] Prompts = [
        "If you like, notice one blue thing nearby. Let your eyes rest there for a moment. There is nothing to get right.",
        "Notice the support of your chair or the floor. Breathe in your usual comfortable way; there is no rhythm to follow or breath to hold.",
        "You do not need every answer right now. You might choose one small thing for later: a glass of water, a stretch, or simply a pause. No need to type it here.",
        "Stay a little, return to the parlour, or begin again. You do not have to feel any particular way to finish."
    ];
    public QuietViewModel()
    {
        Next = new(_ => { if (step < 3) step++; Refresh(); }, () => step < 3);
        Restart = new(_ => Start());
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public RelayCommand Next { get; }
    public RelayCommand Restart { get; }
    public string Title => Titles[step];
    public string Prompt => Prompts[step];
    public string Counter => step == 3 ? "A MOMENT FOR YOURSELF" : $"OPTIONAL MOMENT {step + 1} OF 3";
    public bool Complete => step == 3;
    public bool NotComplete => !Complete;
    public void Start() { step = 0; Refresh(); }
    private void Refresh() { PropertyChanged?.Invoke(this, new(null)); Next.Refresh(); }
}
