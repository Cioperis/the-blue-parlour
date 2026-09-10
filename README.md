# The Blue Parlour

A small, offline Windows game for an unhurried challenge: a three-act opera memory puzzle, optional quiet moments, and attention-switching cards. Blue, burgundy roses, and a sleeping cat. No accounts, timers, ads, telemetry, or purchases.

## New in v0.3.0

- **Midnight Opera is now a three-act memory game.** Act I has four hidden pairs, Act II five, and Act III six. Reveal two cards at a time; mismatches remain visible until you lower the curtain, so you control the pace. Completed pairs stay lit and fixed in place.
- Each act counts pair attempts and offers an optional mastery target. There is no countdown or fail state. An encore reshuffles all three acts.
- The deck now includes six original motifs: mask, rose, candle, key, music and moon.
- **Shifting Spotlight** adds an 18-card executive-attention challenge. The cue switches between naming ink and reading the word, with mostly conflicting stimuli. The current rule is always explicit.
- The quiet-moment activity, fixed-rule attention modes, game icon and existing saved keepsakes remain available.
- CI now uses current Node 24-based GitHub actions.

The opera interlude uses original geometric art and text inspired by the Phantom atmosphere. It contains no music, lyrics, recordings, or artwork from a stage or film production. It is silent. The optional pauses are general relaxation invitations, not a treatment or assessment of stress or uncertainty.

## Play

Download `TheBlueParlour-win-x64.zip` from **Releases** (or a successful **Actions** run), extract it, and double-click `TheBlueParlour.exe`. The .NET runtime is bundled. Windows x64 is the supported target. This prototype is unsigned, so Windows may show a publisher warning. Signing is a future distribution step.

- **The Midnight Opera:** reveal hidden cards using the mouse or Tab and Space. Complete three increasingly difficult acts, then try an encore.
- **A quiet moment:** take or skip three invitations at your own pace. Escape returns home.
- **Shifting Spotlight:** follow the rule shown above each card; it changes between ink and word across 18 trials.
- **Follow the ink:** choose the ink colour, ignoring the word.
- **Follow the word:** choose the written word, ignoring its ink.
- Click a paint or press **1–4**. Press **Enter** for the next card after feedback. Tab and Space also work.
- Finish a sitting to earn a keepsake regardless of accuracy: rose → cat → painting. Continue playing after collecting all three.
- Leaving a sitting abandons that sitting. Completed keepsakes are saved automatically to `%LOCALAPPDATA%/TheBlueParlour/progress.json`.

## Develop

Windows and the .NET 8 SDK are required. This first prototype uses the installed .NET 8 LTS toolchain; upgrade to .NET 10 LTS before .NET 8 support ends in November 2026.

```powershell
dotnet build BlueParlour.sln -c Release
dotnet test BlueParlour.sln -c Release
dotnet run --project src/BlueParlour.Desktop
./scripts/publish.ps1
```

No third-party runtime packages. WPF supplies vector graphics, layout, input, and hardware-accelerated rendering. The game is event-driven and has no continuous polling/render loop. This suits its quiet, card-based gameplay; it is not a general game engine.

## Architecture tour

```text
Desktop ───────> Application ───────> Domain
   │                 ↑
   └────────> Infrastructure
```

| Project | Responsibility | Start here |
|---|---|---|
| Domain | Prompts, answers, selected rule, scoring and completion | `AttentionSession.cs` |
| Application | Balanced deck generation, session lifecycle, once-only keepsakes, storage port | `ParlourGame.cs` |
| Infrastructure | JSON save loading, validation and atomic replacement | `JsonProgressStore.cs` |
| Desktop | Composition root, MVVM, commands, keyboard input, original vector room | `App.xaml.cs`, `MainViewModel.cs`, `MainWindow.xaml` |
| Tests | Rule behavior, balance, rewards, save recovery | `GameTests.cs` |

An answer moves from a WPF command → view model → domain session. The application awards a keepsake only after completion and persists it through `IProgressStore`. `OperaSession` owns concealed-card state, mismatch resolution, turns, pair selection and completion independently of WPF. `OperaViewModel` progresses the three acts, computes mastery targets and shuffles replay decks; `QuietViewModel` owns the optional pause sequence. `AttentionSession` resolves a per-card rule in shifting mode. The domain knows nothing about WPF or disk. The view model owns transient presentation states, including feedback and result screens. Dependency injection is explicit in the composition root; no service locator, mediator or container is needed here.

`--smoke-test <output-folder>` runs deterministic attention sittings against in-memory storage, completes shifting mode and all three opera acts, exercises a mismatch and encore, checks quiet-moment navigation and the window icon, and renders ten offscreen WPF screenshots. CI runs this against the **published executable**, so startup and bundling are checked as well as the domain tests. It never reads or changes the player's save. It is not a substitute for a manual keyboard/mouse check on the recipient's PC.

## GitHub Actions

Every main-branch push and pull request builds, tests, publishes a self-contained single EXE, smoke-tests it, and uploads a ZIP and SHA-256 checksum. Verification artifacts contain TRX results and screenshots. Manual runs are also supported.

Pushing a `v*` tag runs the same verification and then creates a GitHub Release with the ZIP. Only the release job receives `contents: write`; build jobs are read-only. No personal access token or custom secret is needed. Releases inherit this repository's private visibility. To give the game to someone without repository access, send them the downloaded ZIP.

```powershell
git tag v0.3.0
git push origin v0.3.0
```

## Educational basis and limits

The colour-naming mode is inspired by the **Stroop effect**: ink naming commonly takes longer when a colour word and its ink conflict. Fixed-rule sittings contain six matching and six conflicting cards. Shifting Spotlight contains 18 cards and adds explicit rule switching; it is a game mechanic rather than a validated cognitive task.

There is no response-time measurement, normative comparison, diagnostic scoring, or claim of therapeutic benefit. Accuracy counts are game feedback only; they do not demonstrate an individual's Stroop effect or assess attention, intelligence, mental health, or clinical competence. Content is original and paraphrased; no test instrument or APA artwork is reproduced.

- [APA: Color text task](https://www.apa.org/research-practice/conduct-research/infographic-color-text)
- [NHS: Breathing exercises for stress](https://www.nhs.uk/mental-health/self-help/guides-tools-and-activities/breathing-exercises-for-stress/) — background for the invitation to breathe comfortably without forcing; this game does not reproduce or prescribe the full exercise.
- [Microsoft: Single-file deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview)

## Next useful increments

More varied psychology-inspired mechanics, optional sound, richer illustration, independent colour-accessible puzzles, signed distribution, and .NET 10 migration. The present colour task inherently requires colour discrimination; labels support the answer controls but cannot remove that dependency from the stimulus.
