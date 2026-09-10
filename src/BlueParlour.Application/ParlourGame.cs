using BlueParlour.Domain;

namespace BlueParlour.Application;

public sealed record Progress(int CompletedSessions = 0)
{
    public int Roses => Math.Clamp(CompletedSessions, 0, 3);
}
public interface IProgressStore
{
    Progress Load();
    void Save(Progress progress);
}

/// <summary>Coordinates sessions and rewards. Storage is an outward-facing port.</summary>
public sealed class ParlourGame(IProgressStore store, Random random)
{
    private AttentionSession? rewardedSession;
    public Progress Progress { get; private set; } = store.Load();
    public AttentionSession? Session { get; private set; }

    public AttentionSession Start(AttentionRule rule)
    {
        // Equal matching/conflicting trials; shuffle via injected RNG for repeatable tests.
        var prompts = Enumerable.Range(0, 12).Select(i =>
        {
            var ink = (Pigment)random.Next(4);
            var word = i < 6 ? ink : (Pigment)(((int)ink + random.Next(1, 4)) % 4);
            return new Prompt(word, ink);
        }).ToArray();
        random.Shuffle(prompts);
        return Session = new AttentionSession(prompts, rule);
    }

    public bool CollectRose()
    {
        if (Session is not { Complete: true } || ReferenceEquals(Session, rewardedSession)) return false;
        // Persist first: a failed write can be retried without awarding twice.
        var next = new Progress(Math.Min(Progress.CompletedSessions + 1, 1_000_000));
        store.Save(next);
        Progress = next;
        rewardedSession = Session;
        return true;
    }
}
