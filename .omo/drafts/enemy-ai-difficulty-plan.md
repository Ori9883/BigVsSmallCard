# Draft: Enemy AI Difficulty Plan

## Requirements (confirmed)
- Implement actual enemy AI logic for three strengths: 普通, 强力, 神级.
- Mapping from `Assets/ArtAsset/board_game_ai_design.md`: 普通 uses 入门 AI, 强力 uses 进阶 AI, 神级 uses 大师 AI.
- AI decides which card to play at the instant enemy plays a card.
- During Unity playtest, output concise readable logs showing AI decisions.
- Final game runs on Web, so performance must be considered.

## Technical Decisions
- Keep AI decision logic in gameplay C# classes, not UI or scene objects.
- Use one shared AI framework with difficulty profiles, as recommended by the design doc, rather than three unrelated implementations.
- Pass a lightweight decision context into `EnemyAI` from `FirstViewScene.EnemyPlay()` / `GameSession` instead of letting AI read Unity scene state.
- Keep 神级 bounded and deterministic-enough for Web: small candidate set, shallow simulation, no heavy Monte Carlo loops per frame.

## Research Findings
- Current AI is `Assets/FirstView/Scripts/Gameplay/EnemyAI.cs`, only random: `PickCardIndex(List<GameCard> hand)`.
- Current call site is `Assets/FirstView/Scripts/FirstViewScene.cs`, method `EnemyPlay()`, currently calls `EnemyAI.PickCardIndex(session.EnemyHand)`.
- `GameSession` exposes `PlayerHand`, `EnemyHand`, `RemovedCard`, `PlayerScore`, `EnemyScore`, `CurrentRound`, `Phase`, `PlayerIsFirst`, `PlayerPlayedIndex`, `EnemyPlayedIndex`.
- `ScoreSystem.Compare(playerNum, enemyNum)` supports current 1 beats 5 rule, but accepts player/enemy numeric positions, so AI should wrap/normalize comparison for candidate evaluation.
- `GameSession` currently removes played cards only during `Settle()`, so during enemy second-turn decision the player's played card is still available via `PlayerPlayedIndex` and `PlayerHand[PlayerPlayedIndex]`.
- Existing UI difficulty selector is currently UI-only; no code hook exists yet.

## Open Questions
- Whether difficulty selection should be persisted between sessions now or only read from the start screen for the current run.
- Whether AI logs should always be enabled in builds or editor-only / debug toggle.

## Scope Boundaries
- INCLUDE: AI decision framework, three difficulty profiles, UI selection bridge, concise AI decision logs, tests/validation plan.
- EXCLUDE: implementing look-card/exchange phase if game rules are not yet implemented; online services; player behavior learning persistence beyond current match unless explicitly chosen.
