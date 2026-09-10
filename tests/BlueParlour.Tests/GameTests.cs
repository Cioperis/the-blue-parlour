using BlueParlour.Domain;
using BlueParlour.Application;
using BlueParlour.Infrastructure;
using Xunit;

namespace BlueParlour.Tests;
public class GameTests
{
 [Theory]
 [InlineData(AttentionRule.Ink, Pigment.Blue, true)]
 [InlineData(AttentionRule.Ink, Pigment.Rose, false)]
 [InlineData(AttentionRule.Word, Pigment.Rose, true)]
 [InlineData(AttentionRule.Word, Pigment.Blue, false)]
 public void ScoresAgainstSelectedRule(AttentionRule rule, Pigment choice, bool correct)
 {
  var session = new AttentionSession([new(Pigment.Rose, Pigment.Blue)], rule);
  Assert.Equal(correct, session.Submit(choice).Correct);
  Assert.True(session.Complete);
  Assert.Null(session.Current);
  Assert.Throws<InvalidOperationException>(() => session.Submit(choice));
 }
 [Fact] public void RejectsEmptyDeck() => Assert.Throws<ArgumentException>(() => new AttentionSession([], AttentionRule.Ink));
 [Fact] public void DeckContainsEqualMatchingAndConflictingTrials()
 {
  var game = new ParlourGame(new MemoryStore(), new Random(42));
  var session = game.Start(AttentionRule.Ink);
  while (!session.Complete) session.Submit(session.Current!.Ink);
  Assert.Equal(12, session.Correct);
  Assert.Equal(6, session.Answers.Count(a => a.Prompt.IsCongruent));
 }
 [Fact] public void OnlyCompletedSessionsEarnOneRewardRegardlessOfScore()
 {
  var store = new MemoryStore(); var game = new ParlourGame(store, new Random(1));
  var session = game.Start(AttentionRule.Ink);
  Assert.False(game.CollectRose());
  while (!session.Complete) session.Submit((Pigment)(((int)session.Current!.Ink + 1) % 4));
  Assert.Equal(0, session.Correct);
  Assert.True(game.CollectRose()); Assert.False(game.CollectRose());
  Assert.Equal(1, store.Value.CompletedSessions);
 }
 [Fact] public void FailedSaveCanBeRetriedWithoutLosingOrDuplicatingReward()
 {
  var store = new MemoryStore { Fail = true }; var game = new ParlourGame(store, new Random(2));
  var session = game.Start(AttentionRule.Word);
  while (!session.Complete) session.Submit(session.Current!.Word);
  Assert.Throws<IOException>(() => game.CollectRose());
  Assert.Equal(0, game.Progress.CompletedSessions);
  store.Fail = false;
  Assert.True(game.CollectRose()); Assert.False(game.CollectRose());
 }
 [Fact] public void AbandoningSessionDoesNotEarnReward()
 {
  var game = new ParlourGame(new MemoryStore(), new Random(3));
  game.Start(AttentionRule.Ink).Submit(Pigment.Blue);
  Assert.Equal(0, game.Start(AttentionRule.Word).Answered);
  Assert.False(game.CollectRose());
 }
 [Theory] [InlineData("{broken")] [InlineData("null")] [InlineData("{\"CompletedSessions\":-4}")]
 public void DamagedSaveRecovers(string json)
 {
  WithFile(path => { File.WriteAllText(path, json); Assert.Equal(0, new JsonProgressStore(path).Load().CompletedSessions); });
 }
 [Fact] public void ProgressRoundTripsAndOverwritesAtomically()
 {
  WithFile(path => { var store = new JsonProgressStore(path); Assert.Equal(0, store.Load().Roses); store.Save(new(2)); store.Save(new(7)); Assert.Equal(7, store.Load().CompletedSessions); Assert.Equal(3, store.Load().Roses); Assert.False(File.Exists(path + ".tmp")); });
 }
 private static void WithFile(Action<string> action)
 {
  var path = Path.Combine(Path.GetTempPath(), $"blue-parlour-test-{Guid.NewGuid()}.json");
  try { action(path); } finally { File.Delete(path); File.Delete(path + ".tmp"); }
 }
 private sealed class MemoryStore : IProgressStore
 {
  public Progress Value { get; private set; } = new(); public bool Fail { get; set; }
  public Progress Load() => Value;
  public void Save(Progress progress) { if (Fail) throw new IOException("Test failure"); Value = progress; }
 }
}
