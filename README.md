# Barvaz The Dealer

A one-player Caribbean Stud Poker game against Barvaz, the dealer, in Hebrew. Barvaz sits behind a casino table (cut out from his real photo), there's lounge-jazz music, a card-swap house rule, and when you win a hand you get to slap him.

## Run it

1. In Unity Hub, create a new project with the **Universal 2D** (or **2D Core**) template. Unity 2021.3 LTS or newer works; Unity 6 is fine.
2. Copy the `Assets/BarvazTheDealer` folder from this package into your project's `Assets` folder.
3. Press **Play** in any scene, even the empty default one. The game builds the whole table itself.

No prefabs, scene setup, or extra packages are needed. It uses the built-in UI (uGUI) and works with either the old Input Manager or the new Input System.

## How a hand plays

1. Press **DEAL** (or Space). Your $5 ante goes on the table.
2. Barvaz deals five cards to you and five to himself. Only his last card is face up.
3. Optional house rule: click one of your cards and press **Swap** to trade it for a new one from the deck. First swap costs $5, second $10, max two per hand. Fees are not returned.
4. **FOLD** (F) and lose the ante, or **RAISE $10** (R) to play.
5. Barvaz flips his cards. He needs **Ace-King or better** to qualify.
   - He doesn't qualify: your ante pays 1:1 and your raise comes back.
   - He qualifies and you win: ante pays 1:1, raise pays by the paytable.
   - He qualifies and wins: you lose ante and raise.
   - Exact tie: everything is returned.

| Your hand        | Raise pays |
|------------------|-----------:|
| Royal Flush      | 100 : 1 |
| Straight Flush   |  50 : 1 |
| Four of a Kind   |  20 : 1 |
| Full House       |   7 : 1 |
| Flush            |   5 : 1 |
| Straight         |   4 : 1 |
| Three of a Kind  |   3 : 1 |
| Two Pair         |   2 : 1 |
| Pair or less     |   1 : 1 |

You start with a $500 bankroll and can rebuy when you run out.

**Slaps:** win a hand and you earn slaps (1 for a small win, 2 for two pair to trips, 3 for a straight or better). Click Barvaz's face (or press S).

Keys: Space = deal / next hand, F = fold, R = raise, S = slap, M = music on/off, Esc = quit (built app).

## Build the Mac app

Menu **Barvaz → Build Mac App** writes `Builds/Barvaz The Dealer.app`.

## Art and audio

Everything in `Assets/BarvazTheDealer/Resources/` was made for this game: Barvaz's head and body are cut out of his photo (`Art/barvaz_head.png`, `Art/barvaz_body.png`), the room, table, 52 cards, chips and buttons were rendered from HTML/CSS, and the music and sound effects were synthesized. Fonts: Secular One and Rubik (SIL Open Font License, see `Fonts/`). Emoji art from Noto Color Emoji (Apache 2.0).

## Code map

```
Assets/BarvazTheDealer/
  Scripts/Core/        Pure C# rules, no Unity dependency
    Card.cs              Card, Rank, Suit
    Deck.cs              52-card deck, Fisher-Yates shuffle
    HandEvaluator.cs     5-card hand ranking, comparison, dealer qualification
    CaribbeanStudRules.cs  Ante/raise amounts, paytable, round resolution
  Scripts/Game/        Unity side
    GameBootstrap.cs     Auto-creates the game when you press Play
    BarvazGame.cs        Hand flow, swap, slap, payouts, HUD
    DealerView.cs        Barvaz: breathing, talking, laughing, getting slapped
    CardView.cs          Card sprites, deal/flip/discard, click-to-select
    Rtl.cs, Heb.cs       Right-to-left Hebrew text and all Hebrew strings
    Sfx.cs               Music loop and sound effects
    UIParticles.cs       Confetti, stars, chip fountains
    ButtonJuice.cs       Springy buttons with click sounds
    GameAssets.cs        Loads sprites, fonts, audio from Resources
    ProceduralArt.cs     Small generated sprites (rounded rects, glows, vignette)
  Editor/              Build menu and import settings
  Resources/           Art, cards, chips, UI, FX, audio, fonts
LogicTests/LogicTests.cs   Standalone rules tests (outside Assets, Unity ignores it)
```

## Rules tests

The rules are checked outside Unity: every one of the 2,598,960 possible five-card hands is classified and the totals match the known counts, plus payout cases and a one-million-hand simulation (house edge about 5.5% of the ante when raising with Ace-King or better).

```
mcs -out:tests.exe LogicTests/LogicTests.cs Assets/BarvazTheDealer/Scripts/Core/*.cs && mono tests.exe
```
