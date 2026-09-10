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
        Choose = new(_ => choose(index), () => !session.IsMatched(index));
    }
    public OperaMotif Motif => session.Cards[index];
    public bool IsMask => Motif == OperaMotif.Mask;
    public bool IsRose => Motif == OperaMotif.Rose;
    public bool IsCandle => Motif == OperaMotif.Candle;
    public string Label => Motif.ToString();
    public string State => session.IsMatched(index) ? "Paired · lit" : session.Selected == index ? "Selected · choose its twin" : "Choose me";
    public string Background => session.IsMatched(index) ? "#E3D5AF" : session.Selected == index ? "#C8DFE8" : "#FFFAEF";
    public RelayCommand Choose { get; }
    public event PropertyChangedEventHandler? PropertyChanged;
    internal void Refresh() { PropertyChanged?.Invoke(this, new(null)); Choose.Refresh(); }
}

public sealed class OperaViewModel : INotifyPropertyChanged
{
    private readonly Random random;
    private OperaSession session = null!;
    private int variation;
    public OperaViewModel(Random random)
    {
        this.random = random;
        Replay = new(_ => Start());
        Start();
    }
    public event PropertyChangedEventHandler? PropertyChanged;
    public IReadOnlyList<OperaCard> Cards { get; private set; } = [];
    public RelayCommand Replay { get; }
    public bool Complete => session.Complete;
    public string Lights => $"{session.Pairs} / 3 lights glowing";
    public double Glow => 0.2 + session.Pairs * 0.26;
    public string Scene => variation switch { 0 => "The velvet stage", 1 => "The moonlit balcony", _ => "The candlelit lake" };
    public string Message { get; private set; } = "";
    public void Start()
    {
        var deck = Enum.GetValues<OperaMotif>().SelectMany(m => new[] { m, m }).ToArray();
        random.Shuffle(deck);
        session = new(deck);
        variation = random.Next(3);
        Cards = Enumerable.Range(0, 6).Select(i => new OperaCard(session, i, Choose)).ToArray();
        Message = "Choose any card, then its matching twin. Everything stays in view.";
        Refresh();
    }
    private void Choose(int index)
    {
        Message = session.Choose(index) switch
        {
            PairResult.Selected => "One small choice made. Now find the same picture; there is no hurry.",
            PairResult.Deselected => "You can change your mind. Choose whichever picture you like.",
            PairResult.Different => "Two different pictures. Nothing is lost; choose any pair when you are ready.",
            PairResult.Matched => session.Complete ? variation switch
            {
                0 => "The stage is ready. You do not have to plan the whole evening; this little scene is enough.",
                1 => "The balcony glows. A small choice can be enough for this moment.",
                _ => "Candlelight settles on the water. You can leave the next chapter for another time."
            } : "A little more light. Choose whichever pair you would like next.",
            _ => Message
        };
        Refresh();
    }
    private void Refresh()
    {
        foreach (var card in Cards) card.Refresh();
        PropertyChanged?.Invoke(this, new(null));
    }
}
