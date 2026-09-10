namespace BlueParlour.Domain;

public enum OperaMotif { Mask, Rose, Candle }
public enum PairResult { Selected, Deselected, Matched, Different, Ignored }

/// <summary>A visible matching puzzle: no hidden information, clock, or penalty.</summary>
public sealed class OperaSession
{
    private readonly OperaMotif[] cards;
    private readonly HashSet<int> matched = [];
    public OperaSession(IEnumerable<OperaMotif> cards)
    {
        this.cards = cards.ToArray();
        if (this.cards.Length != 6 || this.cards.Any(c => !Enum.IsDefined(c)) ||
            Enum.GetValues<OperaMotif>().Any(m => this.cards.Count(c => c == m) != 2))
            throw new ArgumentException("Provide two of each of the three motifs.", nameof(cards));
    }
    public IReadOnlyList<OperaMotif> Cards => Array.AsReadOnly(cards);
    public int? Selected { get; private set; }
    public int Pairs => matched.Count / 2;
    public bool Complete => Pairs == 3;
    public bool IsMatched(int index) => matched.Contains(index);
    public PairResult Choose(int index)
    {
        if (index < 0 || index >= cards.Length) throw new ArgumentOutOfRangeException(nameof(index));
        if (matched.Contains(index)) return PairResult.Ignored;
        if (Selected == index) { Selected = null; return PairResult.Deselected; }
        if (Selected is not int previous) { Selected = index; return PairResult.Selected; }
        Selected = null;
        if (cards[previous] != cards[index]) return PairResult.Different;
        matched.Add(previous); matched.Add(index);
        return PairResult.Matched;
    }
}
