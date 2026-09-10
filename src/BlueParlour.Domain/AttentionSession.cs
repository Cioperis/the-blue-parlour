namespace BlueParlour.Domain;

public enum Pigment { Blue, Rose, Green, Gold }
public enum AttentionRule { Ink, Word, Shift }
public sealed record Prompt(Pigment Word, Pigment Ink, AttentionRule Rule = AttentionRule.Ink)
{
    public bool IsCongruent => Word == Ink;
}
public sealed record Answer(Prompt Prompt, Pigment Choice, bool Correct);

/// <summary>Pure game state. No clocks, UI, files, or framework dependencies.</summary>
public sealed class AttentionSession
{
    private readonly Prompt[] prompts;
    private readonly List<Answer> answers = [];
    public AttentionSession(IEnumerable<Prompt> prompts, AttentionRule rule, bool oneBack = false)
    {
        this.prompts = prompts.ToArray();
        if (this.prompts.Length == 0) throw new ArgumentException("A session needs prompts.", nameof(prompts));
        if (!Enum.IsDefined(rule) || this.prompts.Any(p => !Enum.IsDefined(p.Ink) || !Enum.IsDefined(p.Word) || p.Rule == AttentionRule.Shift))
            throw new ArgumentException("Unknown rule or pigment.");
        Rule = rule;
        UsesOneBack = oneBack;
    }
    public AttentionRule Rule { get; }
    public bool UsesOneBack { get; }
    public int Total => prompts.Length;
    public int Answered => answers.Count;
    public bool Complete => Answered == Total;
    public Prompt? Current => Complete ? null : prompts[Answered];
    public AttentionRule CurrentRule => Rule == AttentionRule.Shift ? Current?.Rule ?? AttentionRule.Ink : Rule;
    public Pigment Expected
    {
        get
        {
            var source = UsesOneBack && Answered > 0 ? prompts[Answered - 1] : Current ?? throw new InvalidOperationException("Session is complete.");
            return CurrentRule == AttentionRule.Ink ? source.Ink : source.Word;
        }
    }
    public IReadOnlyList<Answer> Answers => answers.AsReadOnly();
    public int Correct => answers.Count(a => a.Correct);
    public Answer Submit(Pigment choice)
    {
        if (!Enum.IsDefined(choice)) throw new ArgumentOutOfRangeException(nameof(choice));
        var prompt = Current ?? throw new InvalidOperationException("Session is complete.");
        var answer = new Answer(prompt, choice, choice == Expected);
        answers.Add(answer);
        return answer;
    }
}
