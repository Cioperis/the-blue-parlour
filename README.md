# The Blue Parlour

A small, offline Windows game for an unhurried challenge: a three-act opera memory puzzle, optional quiet moments, and attention-switching cards in a parlour the player can personalize. No accounts, timers, ads, telemetry, or purchases.

## New in v0.4.0

- **Make it yours** lets the player switch among midnight blue, rose velvet and sage glass palettes; choose a cat, pug or corgi companion; and select Focused or Deep challenge depth. Choices save immediately.
- **Ink in memory** and **Words in memory** are now one-back challenges. The first card is an anchor; every later answer comes from the previous card while the current card supplies interference. Focused has 16 cards and Deep has 24.
- **Shifting Spotlight** remains an explicit ink-or-word switching challenge and expands from 18 cards in Focused mode to 36 in Deep mode.
- **A quiet moment** now offers three replayable paths. The player chooses the route, can return to the chooser at any time, and never types or saves a response.
- The interface has a clearer dashboard, larger cards, softer depth, persistent palette accents, and room artwork that changes with the selected companion.
- Midnight Opera retains its three acts, mastery targets, deliberate mismatch curtain, and reshuffled encore.

The opera interlude uses original geometric art and text inspired by the Phantom atmosphere. It contains no music, lyrics, recordings, or artwork from a stage or film production. It is silent. The optional pauses are general reflection prompts, not treatment or assessment.

## Play

Download `TheBlueParlour-win-x64.zip` from **Releases** (or a successful **Actions** run), extract it, and double-click `TheBlueParlour.exe`. The .NET runtime is bundled. Windows x64 is the supported target. This prototype is unsigned, so Windows may show a publisher warning. Signing is a future distribution step.

- **The Midnight Opera:** reveal hidden cards using the mouse or Tab and Space. Complete three increasingly difficult acts, then try an encore.
- **A quiet moment:** choose one of three short reflection paths, move at your own pace, or return and choose differently. Escape returns home.
- **Shifting Spotlight:** follow the rule shown above each card; it changes between ink and word across 18 or 36 trials.
- **Ink in memory:** choose the ink colour from the previous card while looking at the current card.
- **Words in memory:** choose the word from the previous card while looking at the current card.
- **Make it yours:** choose a room palette, companion and challenge depth. Preferences save immediately.
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
| Application | Deck generation, challenge depth, preferences, session lifecycle, once-only keepsakes, storage port | `ParlourGame.cs` |
| Infrastructure | JSON save loading, preference validation and atomic replacement | `JsonProgressStore.cs` |
| Desktop | Composition root, MVVM, commands, keyboard input, original vector room | `App.xaml.cs`, `MainViewModel.cs`, `MainWindow.xaml` |
| Tests | Rule behavior, balance, rewards, save recovery | `GameTests.cs` |

An answer moves from a WPF command → view model → domain session. The application awards a keepsake only after completion and persists it through `IProgressStore`. The same port saves palette, companion and challenge-depth choices. `OperaSession` owns concealed-card state, mismatch resolution, turns, pair selection and completion independently of WPF. `OperaViewModel` progresses the three acts, computes mastery targets and shuffles replay decks; `QuietViewModel` owns its path chooser and optional prompts. `AttentionSession` owns one-back expectations and resolves a per-card rule in shifting mode. The domain knows nothing about WPF or disk. The view model owns transient presentation states, including feedback and result screens. Dependency injection is explicit in the composition root; no service locator, mediator or container is needed here.

`--smoke-test <output-folder>` runs deterministic one-back sittings against in-memory storage, completes shifting mode and all three opera acts, exercises a mismatch and encore, checks reflection-path navigation, customization and the window icon, and renders fourteen offscreen WPF screenshots. CI runs this against the **published executable**, so startup and bundling are checked as well as the domain tests. It never reads or changes the player's save. It is not a substitute for a manual keyboard/mouse check on the recipient's PC.

## GitHub Actions

Every main-branch push and pull request builds, tests, publishes a self-contained single EXE, smoke-tests it, and uploads a ZIP and SHA-256 checksum. Verification artifacts contain TRX results and screenshots. Manual runs are also supported.

Pushing a `v*` tag runs the same verification and then creates a GitHub Release with the ZIP. Only the release job receives `contents: write`; build jobs are read-only. No personal access token or custom secret is needed. Releases inherit this repository's private visibility. To give the game to someone without repository access, send them the downloaded ZIP.

```powershell
git tag v0.4.0
git push origin v0.4.0
```

## Educational basis and limits

The colour-naming mode is inspired by the **Stroop effect**: ink naming commonly takes longer when a colour word and its ink conflict. The fixed-rule modes add a one-back working-memory mechanic and use mostly conflicting cards: 5 matching plus 11 conflicting in Focused mode, or 8 plus 16 in Deep mode. Shifting Spotlight contains 18 or 36 cards and adds explicit rule switching. These are game mechanics rather than validated cognitive tasks.

There is no response-time measurement, normative comparison, diagnostic scoring, or claim of therapeutic benefit. Accuracy counts are game feedback only; they do not demonstrate an individual's Stroop effect or assess attention, intelligence, mental health, or clinical competence. Content is original and paraphrased; no test instrument or APA artwork is reproduced.

- [APA: Color text task](https://www.apa.org/research-practice/conduct-research/infographic-color-text)
- [NHS: Breathing exercises for stress](https://www.nhs.uk/mental-health/self-help/guides-tools-and-activities/breathing-exercises-for-stress/) — background for the invitation to breathe comfortably without forcing; this game does not reproduce or prescribe the full exercise.
- [Microsoft: Single-file deployment](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview)

## Next useful increments

Optional sound, richer illustration, independent colour-accessible puzzles, signed distribution, and .NET 10 migration. The present colour task inherently requires colour discrimination; labels support the answer controls but cannot remove that dependency from the stimulus.
