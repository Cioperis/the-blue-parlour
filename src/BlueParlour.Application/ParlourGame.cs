using BlueParlour.Domain;

namespace BlueParlour.Application;

public enum ParlourPalette { Midnight, RoseVelvet, SageGlass }
public enum ParlourCompanion { Cat, Pug, Corgi }
public enum FocusDepth { Focused, Deep }

public sealed record Progress(
    int CompletedSessions = 0,
    ParlourPalette Palette = ParlourPalette.Midnight,
    ParlourCompanion Companion = ParlourCompanion.Cat,
    FocusDepth FocusDepth = FocusDepth.Focused)
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

    public void Customize(ParlourPalette palette, ParlourCompanion companion, FocusDepth focusDepth)
    {
        if (!Enum.IsDefined(palette) || !Enum.IsDefined(companion) || !Enum.IsDefined(focusDepth))
            throw new ArgumentException("Unknown parlour preference.");
        var next = Progress with { Palette = palette, Companion = companion, FocusDepth = focusDepth };
        store.Save(next);
        Progress = next;
    }

    public AttentionSession Start(AttentionRule rule)
    {
        if (rule == AttentionRule.Shift) return StartShiftingSpotlight();
        // Mostly conflicting trials; shuffle via injected RNG for repeatable tests.
        var total = Progress.FocusDepth == FocusDepth.Deep ? 24 : 16;
        var prompts = Enumerable.Range(0, total).Select(i =>
        {
            var ink = (Pigment)random.Next(4);
            var word = i < total / 3 ? ink : (Pigment)(((int)ink + random.Next(1, 4)) % 4);
            return new Prompt(word, ink);
        }).ToArray();
        random.Shuffle(prompts);
        return Session = new AttentionSession(prompts, rule, oneBack: true);
    }

    private AttentionSession StartShiftingSpotlight()
    {
        var baseRules = new[]
        {
            AttentionRule.Ink, AttentionRule.Ink, AttentionRule.Word,
            AttentionRule.Word, AttentionRule.Ink, AttentionRule.Word,
            AttentionRule.Ink, AttentionRule.Ink, AttentionRule.Word,
            AttentionRule.Ink, AttentionRule.Word, AttentionRule.Word,
            AttentionRule.Ink, AttentionRule.Word, AttentionRule.Ink,
            AttentionRule.Word, AttentionRule.Word, AttentionRule.Ink
        };
        var rules = Progress.FocusDepth == FocusDepth.Deep ? baseRules.Concat(baseRules).ToArray() : baseRules;
        var prompts = rules.Select((rule, index) =>
        {
            var ink = (Pigment)random.Next(4);
            var word = index % 3 == 0 ? ink : (Pigment)(((int)ink + random.Next(1, 4)) % 4);
            return new Prompt(word, ink, rule);
        }).ToArray();
        return Session = new AttentionSession(prompts, AttentionRule.Shift);
    }

    public bool CollectRose()
    {
        if (Session is not { Complete: true } || ReferenceEquals(Session, rewardedSession)) return false;
        // Persist first: a failed write can be retried without awarding twice.
        var next = Progress with { CompletedSessions = Math.Min(Progress.CompletedSessions + 1, 1_000_000) };
        store.Save(next);
        Progress = next;
        rewardedSession = Session;
        return true;
    }
}
