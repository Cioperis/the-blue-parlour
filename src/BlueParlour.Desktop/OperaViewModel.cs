using System.ComponentModel;
using BlueParlour.Domain;

namespace BlueParlour.Desktop;

public sealed class OperaCard : INotifyPropertyChanged
{
    private readonly OperaSession session;
    private readonly int index;
    public OperaCard(OperaSession session, int index, Action<int> choose)
    {
        this.session = session; this.index = index;
        Choose = new(_ => choose(index), () => !session.Complete && !session.AwaitingCurtain && !session.IsMatched(index));
    }
    public OperaMotif Motif => session.Cards[index];
    public bool IsMask => Motif == OperaMotif.Mask;
    public bool IsRose => Motif == OperaMotif.Rose;
    public bool IsCandle => Motif == OperaMotif.Candle;
    public bool IsKey => Motif == OperaMotif.Key;
    public bool IsMusic => Motif == OperaMotif.Music;
    public bool IsMoon => Motif == OperaMotif.Moon;
    public bool IsVisible => session.IsVisible(index);
    public bool IsHidden => !IsVisible;
    public string AccessibleLabel => IsVisible ? Motif.ToString() : "Hidden opera card";
    public string State => session.IsMatched(index) ? "Paired · lit" : IsVisible ? "Revealed" : "Face down";
    public string Background => session.IsMatched(index) ? "#E3D5AF" : IsVisible ? "#FFFAEF" : "#244F73";
    public RelayCommand Choose { get; }
    public event PropertyChangedEventHandler? PropertyChanged;
    internal void Refresh() { PropertyChanged?.Invoke(this, new(null)); Choose.Refresh(); }
}

public sealed class OperaViewModel : INotifyPropertyChanged
{
    private readonly Random random;
    private OperaSession session = null!;
    private int act = 1;
    private int totalMastery;
    public OperaViewModel(Random random)
    {
        this.random = random;
        Continue = new(_ => { session.LowerCurtain(); Message = "The cards are hidden again. Keep what you noticed and choose once more."; Refresh(); }, () => session.AwaitingCurtain);
        NextAct = new(_ => { if (act < 3) StartAct(act + 1); }, () => session.Complete && act < 3);
        Replay = new(_ => { act = 1; totalMastery = 0; StartAct(1); });
        StartAct(1);
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public IReadOnlyList<OperaCard> Cards { get; private set; } = [];
    public RelayCommand Continue { get; }
    public RelayCommand NextAct { get; }
    public RelayCommand Replay { get; }
    public bool Complete => session.Complete;
    public bool AwaitingCurtain => session.AwaitingCurtain;
    public bool HasNextAct => Complete && act < 3;
    public bool IsCurtainCall => Complete && act == 3;
    public string Lights => $"{session.Pairs} / {session.TotalPairs} pairs · {session.Turns} turns";
    public double Glow => 0.15 + session.Pairs / (double)session.TotalPairs * 0.82;
    public string Scene => act switch { 1 => "Act I · The velvet stage", 2 => "Act II · The moonlit balcony", _ => "Act III · The candlelit lake" };
    public string Challenge => $"Reveal matching pairs from memory. Mastery: {MasteryTarget} turns or fewer.";
    public string Message { get; private set; } = "";
    private int MasteryTarget => session.TotalPairs + 2 + act;

    private void StartAct(int nextAct)
    {
        act = nextAct;
        var motifs = Enum.GetValues<OperaMotif>().Take(act + 3).ToArray();
        var deck = motifs.SelectMany(motif => new[] { motif, motif }).ToArray();
        random.Shuffle(deck);
        session = new(deck);
        Cards = Enumerable.Range(0, deck.Length).Select(index => new OperaCard(session, index, Choose)).ToArray();
        Message = "The cards are hidden. Turn over two and remember what the curtain conceals.";
        Refresh();
    }

    private void Choose(int index)
    {
        var result = session.Choose(index);
        Message = result switch
        {
            PairResult.Selected => "One symbol revealed. Choose a second card.",
            PairResult.Deselected => "That card is face down again. Choose any card when ready.",
            PairResult.Different => "Different symbols. Take a moment to remember them, then lower the curtain.",
            PairResult.Matched when session.Complete => CompleteAct(),
            PairResult.Matched => "A pair found. Its light stays on; the remaining cards stay where they are.",
            _ => Message
        };
        Refresh();
    }

    private string CompleteAct()
    {
        var mastered = session.Turns <= MasteryTarget;
        if (mastered) totalMastery++;
        if (act < 3) return mastered
            ? $"Act complete in {session.Turns} turns — mastery light earned. The next act adds another pair."
            : $"Act complete in {session.Turns} turns. The next act adds another pair.";
        return $"Curtain call: {totalMastery} of 3 mastery lights. Encore to reshuffle every act and try a new route.";
    }

    private void Refresh()
    {
        foreach (var card in Cards) card.Refresh();
        Continue.Refresh(); NextAct.Refresh();
        PropertyChanged?.Invoke(this, new(null));
    }
}
