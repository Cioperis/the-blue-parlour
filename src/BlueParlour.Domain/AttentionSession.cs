namespace BlueParlour.Domain;

public enum Pigment { Blue, Rose, Green, Gold }
public enum AttentionRule { Ink, Word }
public sealed record Prompt(Pigment Word, Pigment Ink)
{
    public bool IsCongruent => Word == Ink;
}
public sealed record Answer(Prompt Prompt, Pigment Choice, bool Correct);

/// <summary>Pure game state. No clocks, UI, files, or framework dependencies.</summary>
public sealed class AttentionSession
{
    private readonly Prompt[] prompts;
    private readonly List<Answer> answers = [];
    public AttentionSession(IEnumerable<Prompt> prompts, AttentionRule rule)
    {
        this.prompts = prompts.ToArray();
        if (this.prompts.Length == 0) throw new ArgumentException("A session needs prompts.", nameof(prompts));
        if (!Enum.IsDefined(rule) || this.prompts.Any(p => !Enum.IsDefined(p.Ink) || !Enum.IsDefined(p.Word)))
            throw new ArgumentException("Unknown rule or pigment.");
        Rule = rule;
    }
    public AttentionRule Rule { get; }
    public int Total => prompts.Length;
    public int Answered => answers.Count;
    public bool Complete => Answered == Total;
    public Prompt? Current => Complete ? null : prompts[Answered];
    public IReadOnlyList<Answer> Answers => answers.AsReadOnly();
    public int Correct => answers.Count(a => a.Correct);
    public Answer Submit(Pigment choice)
    {
        if (!Enum.IsDefined(choice)) throw new ArgumentOutOfRangeException(nameof(choice));
        var prompt = Current ?? throw new InvalidOperationException("Session is complete.");
        var expected = Rule == AttentionRule.Ink ? prompt.Ink : prompt.Word;
        var answer = new Answer(prompt, choice, choice == expected);
        answers.Add(answer);
        return answer;
    }
}
