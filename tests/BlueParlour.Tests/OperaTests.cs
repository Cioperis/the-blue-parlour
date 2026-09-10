using BlueParlour.Domain;
using Xunit;

namespace BlueParlour.Tests;
public class OperaTests
{
    private static OperaSession New() => new([
        OperaMotif.Mask, OperaMotif.Rose, OperaMotif.Candle, OperaMotif.Key,
        OperaMotif.Mask, OperaMotif.Rose, OperaMotif.Candle, OperaMotif.Key]);

    [Fact] public void CardsBeginConcealedAndFirstChoiceRevealsOne()
    {
        var session = New();
        Assert.All(Enumerable.Range(0, 8), index => Assert.False(session.IsVisible(index)));
        Assert.Equal(PairResult.Selected, session.Choose(0));
        Assert.True(session.IsVisible(0));
        Assert.Equal(PairResult.Deselected, session.Choose(0));
        Assert.False(session.IsVisible(0));
    }

    [Fact] public void MismatchRemainsVisibleUntilCurtainLowers()
    {
        var session = New(); session.Choose(0);
        Assert.Equal(PairResult.Different, session.Choose(1));
        Assert.True(session.AwaitingCurtain);
        Assert.True(session.IsVisible(0)); Assert.True(session.IsVisible(1));
        Assert.Equal(PairResult.Ignored, session.Choose(2));
        Assert.True(session.LowerCurtain());
        Assert.False(session.IsVisible(0)); Assert.False(session.IsVisible(1));
        Assert.False(session.LowerCurtain());
    }

    [Fact] public void MatchesRemainVisibleAndTurnsCountPairAttempts()
    {
        var session = New(); session.Choose(0);
        Assert.Equal(PairResult.Matched, session.Choose(4));
        Assert.Equal(1, session.Pairs); Assert.Equal(1, session.Turns);
        Assert.True(session.IsVisible(0)); Assert.True(session.IsVisible(4));
        Assert.Equal(PairResult.Ignored, session.Choose(0));
    }

    [Fact] public void FourPairsCompletePuzzle()
    {
        var session = New();
        for (var i = 0; i < 4; i++) { session.Choose(i); session.Choose(i + 4); }
        Assert.True(session.Complete); Assert.Equal(4, session.Pairs); Assert.Equal(4, session.Turns);
        Assert.Equal(PairResult.Ignored, session.Choose(0));
    }

    [Fact] public void AcceptsFourToSixPairsAndRejectsMalformedDecks()
    {
        Assert.Equal(5, new OperaSession(Enum.GetValues<OperaMotif>().Take(5).SelectMany(x => new[] { x, x })).TotalPairs);
        Assert.Equal(6, new OperaSession(Enum.GetValues<OperaMotif>().SelectMany(x => new[] { x, x })).TotalPairs);
        Assert.Throws<ArgumentException>(() => new OperaSession([OperaMotif.Mask]));
        Assert.Throws<ArgumentException>(() => new OperaSession(Enumerable.Repeat(OperaMotif.Mask, 8)));
        Assert.Throws<ArgumentOutOfRangeException>(() => New().Choose(8));
    }

    [Fact] public void CopiesDeckInsteadOfAllowingCallerMutation()
    {
        OperaMotif[] deck = [OperaMotif.Mask, OperaMotif.Rose, OperaMotif.Candle, OperaMotif.Key, OperaMotif.Mask, OperaMotif.Rose, OperaMotif.Candle, OperaMotif.Key];
        var session = new OperaSession(deck); deck[0] = OperaMotif.Moon;
        Assert.Equal(OperaMotif.Mask, session.Cards[0]);
    }
}
