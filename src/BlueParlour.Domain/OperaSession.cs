namespace BlueParlour.Domain;

public enum OperaMotif { Mask, Rose, Candle, Key, Music, Moon }
public enum PairResult { Selected, Deselected, Matched, Different, Ignored }

/// <summary>A memory-pair state machine. UI controls when a mismatch is concealed.</summary>
public sealed class OperaSession
{
    private readonly OperaMotif[] cards;
    private readonly HashSet<int> matched = [];

    public OperaSession(IEnumerable<OperaMotif> cards)
    {
        this.cards = cards.ToArray();
        var groups = this.cards.GroupBy(card => card).ToArray();
        if (this.cards.Length is < 8 or > 12 || this.cards.Length % 2 != 0 ||
            this.cards.Any(card => !Enum.IsDefined(card)) || groups.Any(group => group.Count() != 2))
            throw new ArgumentException("Provide four to six distinct pairs.", nameof(cards));
    }

    public IReadOnlyList<OperaMotif> Cards => Array.AsReadOnly(cards);
    public int? FirstSelected { get; private set; }
    public int? SecondSelected { get; private set; }
    public bool AwaitingCurtain => SecondSelected is not null;
    public int Pairs => matched.Count / 2;
    public int TotalPairs => cards.Length / 2;
    public int Turns { get; private set; }
    public bool Complete => Pairs == TotalPairs;

    public bool IsMatched(int index) => matched.Contains(index);
    public bool IsVisible(int index) => IsMatched(index) || FirstSelected == index || SecondSelected == index;

    public PairResult Choose(int index)
    {
        if (index < 0 || index >= cards.Length) throw new ArgumentOutOfRangeException(nameof(index));
        if (Complete || AwaitingCurtain || matched.Contains(index)) return PairResult.Ignored;
        if (FirstSelected == index) { FirstSelected = null; return PairResult.Deselected; }
        if (FirstSelected is not int previous) { FirstSelected = index; return PairResult.Selected; }

        SecondSelected = index;
        Turns++;
        if (cards[previous] != cards[index]) return PairResult.Different;

        matched.Add(previous);
        matched.Add(index);
        FirstSelected = SecondSelected = null;
        return PairResult.Matched;
    }

    public bool LowerCurtain()
    {
        if (!AwaitingCurtain) return false;
        FirstSelected = SecondSelected = null;
        return true;
    }
}
