using BlueParlour.Domain;
using Xunit;

namespace BlueParlour.Tests;
public class OperaTests
{
 private static OperaSession New() => new([OperaMotif.Mask, OperaMotif.Rose, OperaMotif.Candle, OperaMotif.Mask, OperaMotif.Rose, OperaMotif.Candle]);
 [Fact] public void DifferentCardsDoNotRemoveExistingPairs()
 {
  var session = New(); session.Choose(0); session.Choose(3);
  session.Choose(1); Assert.Equal(PairResult.Different,session.Choose(2));
  Assert.Equal(1,session.Pairs); Assert.Null(session.Selected);
  session.Choose(1); Assert.Equal(PairResult.Matched,session.Choose(4));
 }
 [Fact] public void SameCardDeselectsAndCannotMatchItself()
 {
  var session = New(); session.Choose(0);
  Assert.Equal(PairResult.Deselected,session.Choose(0));
  Assert.Null(session.Selected); Assert.Equal(0,session.Pairs);
 }
 [Fact] public void ThreePairsCompleteAndCannotBeCollectedTwice()
 {
  var session = New();
  for(var i=0;i<3;i++) { session.Choose(i); session.Choose(i+3); }
  Assert.True(session.Complete); Assert.Equal(3,session.Pairs);
  Assert.Equal(PairResult.Ignored,session.Choose(0)); Assert.Equal(3,session.Pairs);
 }
 [Fact] public void RejectsMalformedDeckAndOutOfRangeChoice()
 {
  Assert.Throws<ArgumentException>(()=>new OperaSession([OperaMotif.Mask]));
  Assert.Throws<ArgumentOutOfRangeException>(()=>New().Choose(6));
 }
 [Fact] public void CopiesDeckInsteadOfAllowingCallerMutation()
 {
  OperaMotif[] deck=[OperaMotif.Mask,OperaMotif.Mask,OperaMotif.Rose,OperaMotif.Rose,OperaMotif.Candle,OperaMotif.Candle];
  var session = new OperaSession(deck); deck[0]=OperaMotif.Candle;
  Assert.Equal(OperaMotif.Mask,session.Cards[0]);
 }
}
