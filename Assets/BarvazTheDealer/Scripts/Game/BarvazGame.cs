using System.Collections;
using System.Collections.Generic;
using BarvazTheDealer.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BarvazTheDealer.Game
{
    /// <summary>
    /// Barvaz The Dealer: one player against Barvaz at a Caribbean Stud table, in Hebrew.
    /// Builds the hall from code at Play, then runs the hand loop:
    /// ante -> deal -> (swap up to 2 cards) -> fold or raise -> reveal -> payout -> slap Barvaz if you won.
    /// </summary>
    public class BarvazGame : MonoBehaviour
    {
        private enum Phase { Betting, Dealing, Deciding, Busy, Slapping, RoundOver }

        [SerializeField] private int startingBankroll = 500;
        [SerializeField] private float dealCardTime = 0.3f;
        [SerializeField] private float dealStagger = 0.11f;

        // ---------- layout (stage is 1920 x 1080, origin at centre; matches the rendered table art) ----------
        private static readonly Vector2 DealerPos = new Vector2(0, 291);
        private static readonly Vector2 DealerCardSize = new Vector2(108, 151);
        private static readonly Vector2 PlayerCardSize = new Vector2(136, 190);
        private const float DealerRowY = -50f;
        private const float PlayerRowY = -320f;
        private static readonly Vector2 ShoePos = new Vector2(450, -40);
        private static readonly Vector2 TrayPos = new Vector2(0, 68);
        private static readonly Vector2 MuckPos = new Vector2(-430, 30);
        private static readonly Vector2 AnteSpot = new Vector2(-520, -300);
        private static readonly Vector2 RaiseSpot = new Vector2(520, -300);
        private static readonly Vector2 BankrollPos = new Vector2(-770, -462);
        private static readonly Vector2 PaytablePos = new Vector2(790, 170);
        private const float ButtonY = -497f;

        private static readonly Color Gold = new Color(0.93f, 0.78f, 0.42f);
        private static readonly Color Cream = new Color(0.99f, 0.95f, 0.87f);
        private static readonly Color Ink = new Color(0.14f, 0.09f, 0.07f);
        private static readonly Color WinGreen = new Color(0.55f, 1f, 0.6f);
        private static readonly Color LoseRed = new Color(1f, 0.5f, 0.45f);

        // ---------- state ----------
        private readonly Deck _deck = new Deck();
        private readonly List<Card> _playerCards = new List<Card>(5);
        private readonly List<Card> _dealerCards = new List<Card>(5);
        private int _bankroll;
        private float _shownBankroll;
        private Phase _phase;
        private int _swapsUsed;
        private int _swapFees;
        private int _slapsLeft;
        private int _slapCount;
        private bool _slapAnimating;
        private CardView _selected;

        // ---------- views ----------
        private RectTransform _stage;
        private RectTransform _fx;
        private UIParticles _particles;
        private DealerView _dealer;
        private RectTransform _cardLayer;
        private RectTransform _chipLayer;
        private readonly CardView[] _playerViews = new CardView[5];
        private readonly CardView[] _dealerViews = new CardView[5];
        private readonly List<RectTransform> _anteChips = new List<RectTransform>();
        private readonly List<RectTransform> _raiseChips = new List<RectTransform>();
        private Text _speech;
        private RectTransform _speechBubble;
        private Text _bankrollText;
        private Text _lastHandText;
        private Text _playerHandLabel;
        private Text _dealerHandLabel;
        private GameObject _playerHandPill;
        private GameObject _dealerHandPill;
        private RectTransform _banner;
        private Text _bannerTitle;
        private Text _bannerDetail;
        private Button _dealButton, _foldButton, _swapButton, _raiseButton, _rebuyButton, _slapButton, _nextButton, _musicButton;
        private readonly Dictionary<HandCategory, Image> _paytableRows = new Dictionary<HandCategory, Image>();
        private Image _slapHand;
        private float _shake;

        // ======================================================================
        // Lifecycle
        // ======================================================================

        private void Start()
        {
            _bankroll = startingBankroll;
            _shownBankroll = _bankroll;
            if (Application.isMobilePlatform)
            {
                // phone: keep the screen awake, landscape only, smooth 60fps
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
                Application.targetFrameRate = 60;
                Screen.orientation = ScreenOrientation.AutoRotation;
                Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = Screen.autorotateToLandscapeRight = true;
            }
            EnsureCameraAndEventSystem();
            gameObject.AddComponent<Sfx>();
            BuildHall();
            EnterBetting();
        }

        private void Update()
        {
            HandleKeys();

            // bankroll counts up/down
            if (Mathf.Abs(_shownBankroll - _bankroll) > 0.01f)
            {
                float speed = Mathf.Max(40f, Mathf.Abs(_bankroll - _shownBankroll) * 3f);
                _shownBankroll = Mathf.MoveTowards(_shownBankroll, _bankroll, speed * Time.deltaTime);
                _bankrollText.text = "$" + Mathf.RoundToInt(_shownBankroll);
                _bankrollText.rectTransform.localScale = Vector3.one * (1f + (_bankroll > _shownBankroll ? 0.12f : 0.04f));
            }
            else
            {
                _bankrollText.rectTransform.localScale = Vector3.Lerp(_bankrollText.rectTransform.localScale, Vector3.one, Time.deltaTime * 10f);
            }

            // screen shake
            if (_shake > 0.01f)
            {
                _stage.anchoredPosition = Random.insideUnitCircle * _shake;
                _shake = Mathf.MoveTowards(_shake, 0f, Time.unscaledDeltaTime * 90f);
            }
            else
            {
                _stage.anchoredPosition = Vector2.zero;
            }

            // slap hand follows the pointer while it's slapping time
            bool showHand = _phase == Phase.Slapping && _slapsLeft > 0;
            if (showHand && !_slapAnimating)
            {
                Vector2 local = Vector2.zero, screen;
                bool aimed = PointerScreen(out screen) && RectTransformUtility.ScreenPointToLocalPointInRectangle(_fx, screen, null, out local);
                if (!aimed)
                {
                    // no mouse (phone): the hand hovers next to his cheek, waiting
                    local = _dealer.CheekIn(_fx) + new Vector2(170f + Mathf.Sin(Time.time * 2.2f) * 25f, -60f + Mathf.Sin(Time.time * 3.1f) * 15f);
                }
                _slapHand.rectTransform.anchoredPosition = Vector2.Lerp(_slapHand.rectTransform.anchoredPosition, local + new Vector2(60, -40), 1f - Mathf.Exp(-Time.deltaTime * 25f));
                _slapHand.rectTransform.localRotation = Quaternion.Euler(0, 0, -18f + Mathf.Sin(Time.time * 6f) * 6f);
            }
            if (!_slapAnimating) _slapHand.gameObject.SetActive(showHand);
        }

        private void HandleKeys()
        {
            if (KeyDown(KeyName.Deal))
            {
                if (_dealButton.gameObject.activeInHierarchy && _dealButton.interactable) OnDeal();
                else if (_nextButton.gameObject.activeInHierarchy) OnNextHand();
            }
            if (KeyDown(KeyName.Fold) && _foldButton.gameObject.activeInHierarchy && _foldButton.interactable) OnFold();
            if (KeyDown(KeyName.Raise) && _raiseButton.gameObject.activeInHierarchy && _raiseButton.interactable) OnRaise();
            if (KeyDown(KeyName.Slap) && _phase == Phase.Slapping) OnSlap();
            if (KeyDown(KeyName.Music)) OnMusic();
            if (KeyDown(KeyName.Quit) && !Application.isEditor) Application.Quit();
        }

        // ======================================================================
        // Game flow
        // ======================================================================

        private void EnterBetting()
        {
            _phase = Phase.Betting;
            RefreshButtons();
            Say(CanAffordHand() ? Heb.Welcome : Heb.Broke);
        }

        private bool CanAffordHand()
        {
            return _bankroll >= CaribbeanStudRules.AnteAmount + CaribbeanStudRules.RaiseAmount;
        }

        private void OnDeal()
        {
            if (_phase != Phase.Betting && _phase != Phase.RoundOver) return;
            if (!CanAffordHand()) return;
            StartCoroutine(DealRoutine());
        }

        private void OnNextHand()
        {
            if (_phase != Phase.Slapping && _phase != Phase.RoundOver) return;
            EndSlapTime();
            if (CanAffordHand()) StartCoroutine(DealRoutine());
            else EnterBetting();
        }

        private void OnFold()
        {
            if (_phase != Phase.Deciding) return;
            StartCoroutine(ResolveRoutine(false));
        }

        private void OnRaise()
        {
            if (_phase != Phase.Deciding || _bankroll < CaribbeanStudRules.RaiseAmount) return;
            StartCoroutine(ResolveRoutine(true));
        }

        private void OnSwap()
        {
            if (_phase != Phase.Deciding || _selected == null || !CanSwap()) return;
            StartCoroutine(SwapRoutine(_selected));
        }

        private void OnRebuy()
        {
            _bankroll = startingBankroll;
            Sfx.Play("coins");
            _particles.Fountain(GameAssets.Chip(100), new Vector2(0, 300), BankrollPos, 14, 70);
            EnterBetting();
        }

        private void OnMusic()
        {
            Sfx.Instance.ToggleMusic();
            UIFactory.SetLabel(_musicButton, Sfx.Instance.MusicOn ? "מוזיקה: פועלת" : "מוזיקה: כבויה");
        }

        private bool CanSwap()
        {
            return _swapsUsed < CaribbeanStudRules.MaxSwaps &&
                   _bankroll >= CaribbeanStudRules.SwapCost(_swapsUsed) + CaribbeanStudRules.RaiseAmount;
        }

        private void OnCardClicked(CardView view)
        {
            if (_phase != Phase.Deciding || _swapsUsed >= CaribbeanStudRules.MaxSwaps) return;
            Sfx.Play("card_flip", 0.5f, 0.2f);
            if (_selected == view)
            {
                view.SetSelected(false);
                _selected = null;
            }
            else
            {
                if (_selected != null) _selected.SetSelected(false);
                _selected = view;
                view.SetSelected(true);
            }
            RefreshButtons();
        }

        private IEnumerator DealRoutine()
        {
            _phase = Phase.Dealing;
            RefreshButtons();
            HideBanner();
            ClearTable();
            _swapsUsed = 0;
            _swapFees = 0;

            _bankroll -= CaribbeanStudRules.AnteAmount;
            yield return ThrowChips(_anteChips, AnteSpot, CaribbeanStudRules.AnteAmount);
            Say(Heb.AnteIn);

            _deck.Reset();
            _playerCards.Clear();
            _dealerCards.Clear();
            for (int i = 0; i < 5; i++)
            {
                _playerCards.Add(_deck.Draw());
                _dealerCards.Add(_deck.Draw());
            }

            for (int i = 0; i < 5; i++)
            {
                _playerViews[i].SetCard(_playerCards[i]);
                StartCoroutine(_playerViews[i].DealFrom(ShoePos, PlayerSlot(i), true, dealCardTime));
                yield return new WaitForSeconds(dealStagger);
                _dealerViews[i].SetCard(_dealerCards[i]);
                StartCoroutine(_dealerViews[i].DealFrom(ShoePos, DealerSlot(i), i == 4, dealCardTime));
                yield return new WaitForSeconds(dealStagger);
            }
            yield return new WaitForSeconds(dealCardTime + 0.25f);

            UpdatePlayerHandLabel();
            _phase = Phase.Deciding;
            SetCardsSelectable(true);
            RefreshButtons();
            Say(Heb.Decide(_dealerCards[4]));
        }

        private IEnumerator SwapRoutine(CardView view)
        {
            _phase = Phase.Busy;
            RefreshButtons();
            int idx = System.Array.IndexOf(_playerViews, view);
            int cost = CaribbeanStudRules.SwapCost(_swapsUsed);
            _bankroll -= cost;
            _swapFees += cost;
            _swapsUsed++;
            _selected = null;

            // the fee goes straight to Barvaz's tray
            var fee = MakeChip(cost, BankrollPos);
            Sfx.Play("chip");
            StartCoroutine(MoveAndDestroy(fee, TrayPos, 0.45f));

            yield return view.Discard(MuckPos, 0.3f);
            _playerCards[idx] = _deck.Draw();
            view.SetCard(_playerCards[idx]);
            yield return view.DealFrom(ShoePos, PlayerSlot(idx), true, dealCardTime);

            UpdatePlayerHandLabel();
            _playerHandPill.transform.localScale = Vector3.one * 1.2f;
            Say(_swapsUsed < CaribbeanStudRules.MaxSwaps ? Heb.SwapDone + " עוד החלפה: $" + CaribbeanStudRules.SwapCost(_swapsUsed) : Heb.SwapDone);
            _phase = Phase.Deciding;
            SetCardsSelectable(_swapsUsed < CaribbeanStudRules.MaxSwaps);
            RefreshButtons();
        }

        private IEnumerator ResolveRoutine(bool raised)
        {
            _phase = Phase.Busy;
            SetCardsSelectable(false);
            if (_selected != null)
            {
                _selected.SetSelected(false);
                _selected = null;
            }
            RefreshButtons();

            if (raised)
            {
                _bankroll -= CaribbeanStudRules.RaiseAmount;
                yield return ThrowChips(_raiseChips, RaiseSpot, CaribbeanStudRules.RaiseAmount);
                Say(Heb.Raised);
            }
            else
            {
                Say(Heb.Folded);
            }
            yield return new WaitForSeconds(0.3f);

            for (int i = 0; i < 4; i++)
            {
                yield return _dealerViews[i].Flip(true, 0.24f);
                yield return new WaitForSeconds(0.14f);
            }

            RoundResult result = raised
                ? CaribbeanStudRules.ResolveRaise(_playerCards, _dealerCards, CaribbeanStudRules.AnteAmount, CaribbeanStudRules.RaiseAmount)
                : CaribbeanStudRules.ResolveFold(_playerCards, _dealerCards, CaribbeanStudRules.AnteAmount);

            UIFactory.SetHeb(_dealerHandLabel, Heb.BarvazPrefix + Heb.HandName(result.DealerHand) + (result.DealerQualified ? "" : Heb.NotQualified));
            _dealerHandPill.SetActive(true);
            yield return new WaitForSeconds(0.45f);

            yield return PayOut(result);
            _bankroll += result.TotalReturned;

            int net = result.Net - _swapFees;
            UIFactory.SetHeb(_lastHandText, Heb.LastHand + Heb.Signed(net));
            _lastHandText.color = net > 0 ? WinGreen : net < 0 ? LoseRed : Cream;

            int slaps = 0;
            if (result.Outcome == RoundOutcome.PlayerWins) slaps = result.RaiseMultiplier >= 4 ? 3 : result.RaiseMultiplier >= 2 ? 2 : 1;
            else if (result.Outcome == RoundOutcome.DealerDidNotQualify) slaps = 1;

            if (slaps > 0) EnterSlapTime(slaps);
            else
            {
                _phase = Phase.RoundOver;
                RefreshButtons();
                if (!CanAffordHand()) Say(Heb.Broke);
            }
        }

        private IEnumerator PayOut(RoundResult r)
        {
            switch (r.Outcome)
            {
                case RoundOutcome.PlayerFolded:
                    ShowBanner(Heb.BannerFold, "האנטה הלכה  " + Heb.Signed(r.Net), new Color(0.8f, 0.8f, 0.85f));
                    Say(Pick(Heb.FoldLines));
                    _dealer.Laugh();
                    Sfx.Play("rimshot", 0.7f, 0f);
                    yield return SweepChips(_anteChips, TrayPos, false);
                    break;

                case RoundOutcome.DealerDidNotQualify:
                    ShowBanner(Heb.BannerNoQualify, "האנטה משלמת 1:1, ההעלאה חוזרת  " + Heb.Signed(r.Net), Gold);
                    Say(Pick(Heb.NoQualifyLines));
                    _dealer.Worry();
                    Sfx.Play("win", 0.8f, 0f);
                    _particles.Confetti(50);
                    yield return PayChips(_anteChips, AnteSpot, r.Ante);
                    yield return new WaitForSeconds(0.3f);
                    yield return SweepChips(_anteChips, BankrollPos, true);
                    yield return SweepChips(_raiseChips, BankrollPos, true);
                    break;

                case RoundOutcome.PlayerWins:
                {
                    bool big = r.RaiseMultiplier >= 3;
                    HighlightWinners(_playerViews, _playerCards);
                    HighlightPaytable(r.PlayerHand.Category);
                    ShowBanner((big ? Heb.BannerBigWin : Heb.BannerWin) + "  " + Heb.Signed(r.Net),
                        Heb.HandName(r.PlayerHand) + " מנצח. ההעלאה משלמת " + r.RaiseMultiplier + ":1", WinGreen);
                    Say(Pick(Heb.PlayerWinLines));
                    _dealer.Worry();
                    Sfx.Play("win", 1f, 0f);
                    _particles.Confetti(big ? 220 : 90);
                    if (big) _shake = 14f;
                    yield return PayChips(_anteChips, AnteSpot, r.Ante);
                    yield return PayChips(_raiseChips, RaiseSpot, r.Raise * r.RaiseMultiplier);
                    yield return new WaitForSeconds(0.5f);
                    yield return SweepChips(_anteChips, BankrollPos, true);
                    yield return SweepChips(_raiseChips, BankrollPos, true);
                    break;
                }

                case RoundOutcome.DealerWins:
                    HighlightWinners(_dealerViews, _dealerCards);
                    ShowBanner(Heb.BannerLose + "  " + Heb.Signed(r.Net), Heb.HandName(r.DealerHand) + " מנצח את " + Heb.HandName(r.PlayerHand), LoseRed);
                    Say(Pick(Heb.DealerWinLines));
                    _dealer.Laugh();
                    Sfx.Play("lose", 0.8f, 0f);
                    yield return new WaitForSeconds(0.3f);
                    yield return SweepChips(_anteChips, TrayPos, false);
                    yield return SweepChips(_raiseChips, TrayPos, false);
                    break;

                case RoundOutcome.Push:
                    ShowBanner(Heb.BannerPush, "ידיים זהות. הכסף חוזר.", Cream);
                    Say(Heb.Push);
                    yield return SweepChips(_anteChips, BankrollPos, true);
                    yield return SweepChips(_raiseChips, BankrollPos, true);
                    break;
            }
        }

        // ======================================================================
        // The slap
        // ======================================================================

        private void EnterSlapTime(int slaps)
        {
            _phase = Phase.Slapping;
            _slapsLeft = slaps;
            UIFactory.SetHeb(_bannerDetail, Heb.SlapsEarned(slaps));
            _dealer.SetClickable(true, OnSlap);
            RefreshButtons();
        }

        private void EndSlapTime()
        {
            _slapsLeft = 0;
            _dealer.SetClickable(false, null);
            _slapHand.gameObject.SetActive(false);
        }

        private void OnSlap()
        {
            if (_phase != Phase.Slapping || _slapsLeft <= 0 || _slapAnimating) return;
            StartCoroutine(SlapRoutine());
        }

        private IEnumerator SlapRoutine()
        {
            _slapAnimating = true;
            _slapsLeft--;
            _slapCount++;
            RefreshButtons();

            var hand = _slapHand.rectTransform;
            _slapHand.gameObject.SetActive(true);
            _slapHand.color = Color.white;
            Vector2 cheek = _dealer.CheekIn(_fx);
            Vector2 windup = cheek + new Vector2(470, 90);
            Vector2 from = hand.anchoredPosition;

            // wind up
            yield return Tween(0.12f, k =>
            {
                hand.anchoredPosition = Vector2.Lerp(from, windup, Smooth(k));
                hand.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-18f, -40f, k));
                hand.localScale = Vector3.one * Mathf.Lerp(1f, 1.25f, k);
            });

            // swing
            Sfx.Play("whoosh", 0.9f);
            yield return Tween(0.085f, k =>
            {
                hand.anchoredPosition = Vector2.Lerp(windup, cheek + new Vector2(30, 0), k * k);
                hand.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(-40f, 12f, k));
            });

            // IMPACT
            float power = 1f + 0.3f * (_slapCount % 3);
            Sfx.Play("slap", 1f, 0.08f);
            Sfx.Instance.Duck(0.25f, 0.9f);
            _dealer.Impact(power);
            _shake = 26f * power;
            _particles.Burst(GameAssets.Sprite("FX/star"), cheek, 18, 900f, 56f, 0.9f);
            _particles.Burst(GameAssets.Sprite("FX/sweat"), cheek + new Vector2(-60, 40), 6, 600f, 44f, 0.7f);
            StartCoroutine(Pop(GameAssets.Sprite("FX/boom"), cheek + new Vector2(10, 0), 260f, 0.45f));
            StartCoroutine(ComicWord(cheek + new Vector2(-40, 170)));

            // hit-stop: freeze for a heartbeat so it lands
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(0.07f);
            Time.timeScale = 1f;

            // follow through
            yield return Tween(0.16f, k =>
            {
                hand.anchoredPosition = Vector2.Lerp(cheek + new Vector2(30, 0), cheek + new Vector2(-330, -70), k);
                hand.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(12f, 40f, k));
                _slapHand.color = new Color(1, 1, 1, 1f - k);
            });
            _slapHand.color = Color.white;
            hand.localScale = Vector3.one;

            yield return new WaitForSeconds(0.2f);
            Sfx.Play("dizzy", 0.6f, 0.1f);
            Say(Heb.OuchLines[(_slapCount - 1) % Heb.OuchLines.Length]);

            _slapAnimating = false;
            if (_slapsLeft <= 0)
            {
                yield return new WaitForSeconds(1.6f);
                if (_phase == Phase.Slapping)
                {
                    EndSlapTime();
                    _phase = Phase.RoundOver;
                    Say(Heb.RecoverLine);
                    RefreshButtons();
                }
            }
        }

        private static readonly string[] ComicWords = { "טראח!", "בום!", "פלאק!" };

        private IEnumerator ComicWord(Vector2 at)
        {
            var t = UIFactory.Text("Comic", _fx, ComicWords[Random.Range(0, ComicWords.Length)], 96, new Color(1f, 0.92f, 0.3f), at, new Vector2(500, 140), TextAnchor.MiddleCenter, GameAssets.Display);
            UIFactory.AddOutline(t, new Color(0.55f, 0.05f, 0.05f), 5f);
            UIFactory.AddShadow(t, 0.8f, 6f);
            var rt = t.rectTransform;
            rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-16f, 8f));
            yield return Tween(0.85f, k =>
            {
                float s = k < 0.15f ? Mathf.Lerp(0.2f, 1.35f, k / 0.15f) : Mathf.Lerp(1.35f, 1.1f, (k - 0.15f) / 0.85f);
                rt.localScale = Vector3.one * s;
                rt.anchoredPosition = at + new Vector2(0, k * 60f);
                var c = t.color;
                c.a = k > 0.7f ? 1f - (k - 0.7f) / 0.3f : 1f;
                t.color = c;
            });
            Destroy(rt.gameObject);
        }

        private IEnumerator Pop(Sprite sprite, Vector2 at, float size, float duration)
        {
            var img = UIFactory.Image("Pop", _fx, at, new Vector2(size, size), sprite, Color.white);
            img.type = Image.Type.Simple;
            var rt = img.rectTransform;
            rt.localRotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));
            yield return Tween(duration, k =>
            {
                rt.localScale = Vector3.one * (k < 0.25f ? Mathf.Lerp(0.2f, 1.3f, k / 0.25f) : Mathf.Lerp(1.3f, 1.0f, k));
                img.color = new Color(1, 1, 1, k > 0.5f ? 1f - (k - 0.5f) / 0.5f : 1f);
            });
            Destroy(rt.gameObject);
        }

        // ======================================================================
        // Chips
        // ======================================================================

        private RectTransform MakeChip(int value, Vector2 pos)
        {
            var img = UIFactory.Image("Chip", _chipLayer, pos, new Vector2(80, 80), GameAssets.Chip(value), Color.white);
            img.type = Image.Type.Simple;
            var shadow = img.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(2, -4);
            return img.rectTransform;
        }

        private IEnumerator ThrowChips(List<RectTransform> stack, Vector2 spot, int amount)
        {
            foreach (int v in GameAssets.BreakIntoChips(amount))
            {
                var chip = MakeChip(v, BankrollPos);
                Vector2 target = spot + new Vector2(Random.Range(-3f, 3f), stack.Count * 8f);
                stack.Add(chip);
                StartCoroutine(Arc(chip, BankrollPos, target, 0.38f, 120f, true));
                yield return new WaitForSeconds(0.08f);
            }
            yield return new WaitForSeconds(0.32f);
        }

        private IEnumerator PayChips(List<RectTransform> stack, Vector2 spot, int amount)
        {
            if (amount <= 0) yield break;
            foreach (int v in GameAssets.BreakIntoChips(amount))
            {
                var chip = MakeChip(v, TrayPos);
                Vector2 target = spot + new Vector2(Random.Range(-3f, 3f), stack.Count * 8f);
                stack.Add(chip);
                StartCoroutine(Arc(chip, TrayPos, target, 0.42f, 160f, true));
                yield return new WaitForSeconds(0.09f);
            }
            yield return new WaitForSeconds(0.35f);
        }

        private IEnumerator SweepChips(List<RectTransform> stack, Vector2 destination, bool toPlayer)
        {
            if (stack.Count == 0) yield break;
            var moving = new List<RectTransform>(stack);
            stack.Clear();
            if (toPlayer) Sfx.Play("coins", 0.8f);
            else Sfx.Play("chips_stack", 0.8f);
            for (int i = moving.Count - 1; i >= 0; i--)
            {
                StartCoroutine(MoveAndDestroy(moving[i], destination, 0.4f));
                yield return new WaitForSeconds(0.05f);
            }
            yield return new WaitForSeconds(0.42f);
            if (toPlayer) _bankrollText.rectTransform.localScale = Vector3.one * 1.3f;
        }

        private IEnumerator MoveAndDestroy(RectTransform rt, Vector2 to, float duration)
        {
            if (rt == null) yield break;
            yield return Arc(rt, rt.anchoredPosition, to, duration, 80f, false);
            if (rt != null) Destroy(rt.gameObject);
        }

        private static IEnumerator Arc(RectTransform rt, Vector2 from, Vector2 to, float duration, float height, bool clack)
        {
            float t = 0f;
            while (t < duration && rt != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                float e = 1f - Mathf.Pow(1f - k, 3f);
                rt.anchoredPosition = Vector2.Lerp(from, to, e) + new Vector2(0, Mathf.Sin(k * Mathf.PI) * height);
                rt.localScale = Vector3.one * (1f + 0.25f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            if (rt == null) yield break;
            rt.anchoredPosition = to;
            rt.localScale = Vector3.one;
            if (clack) Sfx.Play("chip", 0.7f, 0.15f);
        }

        // ======================================================================
        // UI state helpers
        // ======================================================================

        private void RefreshButtons()
        {
            bool deciding = _phase == Phase.Deciding;
            bool between = _phase == Phase.Betting || _phase == Phase.RoundOver;
            bool slapping = _phase == Phase.Slapping;

            _dealButton.gameObject.SetActive(between && CanAffordHand());
            UIFactory.SetLabel(_dealButton, (_phase == Phase.RoundOver ? Heb.NextHand : Heb.Deal) + "  ·  אנטה $" + CaribbeanStudRules.AnteAmount);
            _rebuyButton.gameObject.SetActive(between && !CanAffordHand());

            _foldButton.gameObject.SetActive(deciding);
            _raiseButton.gameObject.SetActive(deciding);
            _raiseButton.interactable = _bankroll >= CaribbeanStudRules.RaiseAmount;
            _swapButton.gameObject.SetActive(deciding);
            if (deciding)
            {
                if (_swapsUsed >= CaribbeanStudRules.MaxSwaps)
                {
                    UIFactory.SetLabel(_swapButton, Heb.NoMoreSwaps);
                    _swapButton.interactable = false;
                }
                else if (_selected == null)
                {
                    UIFactory.SetLabel(_swapButton, Heb.PickCard);
                    _swapButton.interactable = false;
                }
                else
                {
                    UIFactory.SetLabel(_swapButton, Heb.SwapButton(CaribbeanStudRules.SwapCost(_swapsUsed)));
                    _swapButton.interactable = CanSwap();
                }
            }

            _slapButton.gameObject.SetActive(slapping && _slapsLeft > 0);
            if (slapping) UIFactory.SetLabel(_slapButton, Heb.SlapHim + (_slapsLeft > 1 ? "  (" + _slapsLeft + ")" : ""));
            _nextButton.gameObject.SetActive(slapping);
        }

        private void SetCardsSelectable(bool on)
        {
            foreach (var v in _playerViews) v.Selectable = on;
        }

        private void ClearTable()
        {
            for (int i = 0; i < 5; i++)
            {
                _playerViews[i].gameObject.SetActive(false);
                _dealerViews[i].gameObject.SetActive(false);
                _playerViews[i].SetHighlight(false);
                _playerViews[i].SetSelected(false);
                _playerViews[i].Selectable = false;
                _dealerViews[i].SetHighlight(false);
            }
            _selected = null;
            foreach (var c in _anteChips) if (c != null) Destroy(c.gameObject);
            foreach (var c in _raiseChips) if (c != null) Destroy(c.gameObject);
            _anteChips.Clear();
            _raiseChips.Clear();
            _playerHandPill.SetActive(false);
            _dealerHandPill.SetActive(false);
            HighlightPaytable(null);
        }

        private void UpdatePlayerHandLabel()
        {
            UIFactory.SetHeb(_playerHandLabel, Heb.You + Heb.HandName(HandEvaluator.Evaluate(_playerCards)));
            _playerHandPill.SetActive(true);
        }

        private void Say(string line)
        {
            UIFactory.SetHeb(_speech, line, 30);
            _speechBubble.localScale = Vector3.one * 0.85f;
            if (_dealer != null) _dealer.Talk(Mathf.Clamp(line.Length * 0.04f, 0.6f, 2.2f));
        }

        private void ShowBanner(string title, string detail, Color titleColor)
        {
            UIFactory.SetHeb(_bannerTitle, title);
            _bannerTitle.color = titleColor;
            UIFactory.SetHeb(_bannerDetail, detail);
            _banner.gameObject.SetActive(true);
            _banner.localScale = Vector3.one * 0.3f;
        }

        private void HideBanner()
        {
            _banner.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            // springy pops for the speech bubble, banner and hand pill
            _speechBubble.localScale = Vector3.Lerp(_speechBubble.localScale, Vector3.one, 1f - Mathf.Exp(-Time.deltaTime * 14f));
            if (_banner.gameObject.activeSelf)
            {
                float s = _banner.localScale.x;
                _banner.localScale = Vector3.one * Mathf.Lerp(s, s < 0.98f ? 1.08f : 1f, 1f - Mathf.Exp(-Time.deltaTime * 16f));
            }
            _playerHandPill.transform.localScale = Vector3.Lerp(_playerHandPill.transform.localScale, Vector3.one, 1f - Mathf.Exp(-Time.deltaTime * 10f));
        }

        private void HighlightWinners(CardView[] views, List<Card> cards)
        {
            var hand = HandEvaluator.Evaluate(cards);
            bool allFive = hand.Category == HandCategory.Straight || hand.Category == HandCategory.Flush ||
                           hand.Category == HandCategory.FullHouse || hand.Category >= HandCategory.StraightFlush;
            var counts = new Dictionary<Rank, int>();
            foreach (var c in cards)
            {
                int n;
                counts.TryGetValue(c.Rank, out n);
                counts[c.Rank] = n + 1;
            }
            int hop = 0;
            for (int i = 0; i < cards.Count; i++)
            {
                bool on = allFive || counts[cards[i].Rank] >= 2 ||
                          (hand.Category == HandCategory.HighCard && (int)cards[i].Rank == hand.Tiebreak[0]);
                views[i].SetHighlight(on);
                if (on) StartCoroutine(views[i].Hop(0.06f * hop++));
            }
        }

        private void HighlightPaytable(HandCategory? category)
        {
            foreach (var kv in _paytableRows)
            {
                bool on = category.HasValue &&
                          (kv.Key == category.Value || (kv.Key == HandCategory.OnePair && category.Value <= HandCategory.OnePair));
                kv.Value.enabled = on;
            }
        }

        private static string Pick(string[] lines)
        {
            return lines[Random.Range(0, lines.Length)];
        }

        private static Vector2 PlayerSlot(int i)
        {
            return new Vector2((i - 2) * 165f, PlayerRowY);
        }

        private static Vector2 DealerSlot(int i)
        {
            return new Vector2((i - 2) * 124f, DealerRowY);
        }

        private delegate void TweenStep(float k);

        private static IEnumerator Tween(float duration, TweenStep step)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                step(Mathf.Clamp01(t / duration));
                yield return null;
            }
            step(1f);
        }

        private static float Smooth(float k)
        {
            return k * k * (3f - 2f * k);
        }

        // ======================================================================
        // Input (old Input Manager or new Input System)
        // ======================================================================

        private enum KeyName { Deal, Fold, Raise, Slap, Music, Quit }

        private static bool KeyDown(KeyName k)
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null) return false;
            switch (k)
            {
                case KeyName.Deal: return kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
                case KeyName.Fold: return kb.fKey.wasPressedThisFrame;
                case KeyName.Raise: return kb.rKey.wasPressedThisFrame;
                case KeyName.Slap: return kb.sKey.wasPressedThisFrame;
                case KeyName.Music: return kb.mKey.wasPressedThisFrame;
                default: return kb.escapeKey.wasPressedThisFrame;
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            switch (k)
            {
                case KeyName.Deal: return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);
                case KeyName.Fold: return Input.GetKeyDown(KeyCode.F);
                case KeyName.Raise: return Input.GetKeyDown(KeyCode.R);
                case KeyName.Slap: return Input.GetKeyDown(KeyCode.S);
                case KeyName.Music: return Input.GetKeyDown(KeyCode.M);
                default: return Input.GetKeyDown(KeyCode.Escape);
            }
#else
            return false;
#endif
        }

        /// <summary>Where the mouse is, or the finger while it touches the screen. False when there's neither.</summary>
        private static bool PointerScreen(out Vector2 pos)
        {
            pos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            var ts = UnityEngine.InputSystem.Touchscreen.current;
            if (ts != null && ts.primaryTouch.press.isPressed) { pos = ts.primaryTouch.position.ReadValue(); return true; }
            if (Application.isMobilePlatform) return false;
            var m = UnityEngine.InputSystem.Mouse.current;
            if (m == null) return false;
            pos = m.position.ReadValue();
            return true;
#elif ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0) { pos = Input.GetTouch(0).position; return true; }
            if (Application.isMobilePlatform || !Input.mousePresent) return false;
            pos = Input.mousePosition;
            return true;
#else
            return false;
#endif
        }

        // ======================================================================
        // Scene construction
        // ======================================================================

        private static void EnsureCameraAndEventSystem()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            if (FindAny<AudioListener>() == null) cam.gameObject.AddComponent<AudioListener>();

            if (EventSystem.current == null && FindAny<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
        }

        private static T FindAny<T>() where T : Object
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindFirstObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }

        private void BuildHall()
        {
            var canvasGo = new GameObject("Poker Hall Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)canvasGo.transform;

            // The room fills the whole window; everything else lives on a fixed 1920x1080 stage.
            var room = UIFactory.Image("Room", root, Vector2.zero, new Vector2(1920, 1080), GameAssets.Sprite("Art/room"), Color.white);
            room.type = Image.Type.Simple;
            var fitter = room.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = 16f / 9f;

            _stage = UIFactory.Rect("Stage", root, Vector2.zero, new Vector2(1920, 1080));

            _dealer = DealerView.Create(_stage, DealerPos);
            var table = UIFactory.Image("Table", _stage, Vector2.zero, new Vector2(1920, 1080), GameAssets.Sprite("Art/table"), Color.white);
            table.type = Image.Type.Simple;

            BuildPaytable();
            BuildDealerHud();

            _cardLayer = UIFactory.Stretch("Cards", _stage);
            _chipLayer = UIFactory.Stretch("Chips", _stage);
            for (int i = 0; i < 5; i++)
            {
                _dealerViews[i] = CardView.Create(_cardLayer, DealerSlot(i), DealerCardSize);
                _dealerViews[i].gameObject.SetActive(false);
                _playerViews[i] = CardView.Create(_cardLayer, PlayerSlot(i), PlayerCardSize);
                _playerViews[i].Clicked = OnCardClicked;
                _playerViews[i].gameObject.SetActive(false);
            }

            // darken the edges of the hall, above the table and cards but under the HUD
            var vignette = UIFactory.Image("Vignette", _stage, Vector2.zero, Vector2.zero, ProceduralArt.Vignette, new Color(1, 1, 1, 0.7f));
            vignette.rectTransform.anchorMin = Vector2.zero;
            vignette.rectTransform.anchorMax = Vector2.one;
            vignette.rectTransform.offsetMin = new Vector2(-200, -120);
            vignette.rectTransform.offsetMax = new Vector2(200, 120);

            BuildHud();

            _fx = UIFactory.Stretch("FX", _stage);
            _slapHand = UIFactory.Image("Slap Hand", _fx, Vector2.zero, new Vector2(270, 270), GameAssets.Sprite("FX/hand"), Color.white);
            _slapHand.type = Image.Type.Simple;
            _slapHand.gameObject.SetActive(false);
            _particles = UIParticles.Create(_stage);
        }

        private void BuildPaytable()
        {
            var sign = UIFactory.Image("Paytable", _stage, PaytablePos, new Vector2(340, 440), GameAssets.Sprite("UI/paytable"), Color.white);
            sign.type = Image.Type.Simple;
            var rows = CaribbeanStudRules.PaytableRows;
            for (int i = 0; i < rows.Length; i++)
            {
                var bar = UIFactory.Image("Row Highlight", _stage, PaytablePos + new Vector2(0, 115 - 34 * i), new Vector2(300, 32), ProceduralArt.RoundedRect, new Color(0.95f, 0.8f, 0.35f, 0.3f));
                bar.enabled = false;
                _paytableRows[rows[i].Key] = bar;
            }
        }

        private void BuildDealerHud()
        {
            // Name plate
            var plate = UIFactory.Image("Name Plate", _stage, new Vector2(-265, 400), new Vector2(230, 92), GameAssets.Sprite("UI/panel"), Color.white);
            UIFactory.Text("Name", plate.transform, Heb.Dealer, 44, Gold, new Vector2(0, 10), new Vector2(220, 56), TextAnchor.MiddleCenter, GameAssets.Display);
            UIFactory.Text("Role", plate.transform, Heb.YourDealer, 18, Cream, new Vector2(0, -26), new Vector2(220, 24), TextAnchor.MiddleCenter, GameAssets.Medium);

            // Speech bubble with a tail pointing at his face
            _speechBubble = UIFactory.Rect("Speech Bubble", _stage, new Vector2(390, 420), new Vector2(470, 150));
            _speechBubble.pivot = new Vector2(0f, 0.5f);
            _speechBubble.anchoredPosition = new Vector2(150, 420);
            var bubble = UIFactory.Image("Bubble", _speechBubble, Vector2.zero, new Vector2(470, 150), GameAssets.Sprite("UI/bubble"), Color.white);
            var tail = UIFactory.Image("Tail", _speechBubble, new Vector2(-249, -18), new Vector2(56, 56), GameAssets.Sprite("UI/bubble_tail"), Color.white);
            tail.type = Image.Type.Simple;
            _speech = UIFactory.Text("Line", bubble.transform, "", 25, Ink, Vector2.zero, new Vector2(430, 130), TextAnchor.MiddleCenter, GameAssets.Medium);
            _speech.lineSpacing = 1.1f;
        }

        private void BuildHud()
        {
            // hand labels
            _dealerHandPill = Pill(new Vector2(0, -140), out _dealerHandLabel);
            _playerHandPill = Pill(new Vector2(0, -436), out _playerHandLabel);

            // result banner
            var banner = UIFactory.Image("Banner", _stage, new Vector2(0, -212), new Vector2(940, 112), GameAssets.Sprite("UI/panel"), Color.white);
            _bannerTitle = UIFactory.Text("Title", banner.transform, "", 50, Gold, new Vector2(0, 18), new Vector2(920, 64), TextAnchor.MiddleCenter, GameAssets.Display);
            UIFactory.AddShadow(_bannerTitle, 0.9f, 3f);
            _bannerDetail = UIFactory.Text("Detail", banner.transform, "", 23, Cream, new Vector2(0, -30), new Vector2(920, 30), TextAnchor.MiddleCenter, GameAssets.Medium);
            _banner = banner.rectTransform;
            _banner.gameObject.SetActive(false);

            // bankroll
            var bank = UIFactory.Image("Bankroll Panel", _stage, BankrollPos, new Vector2(340, 130), GameAssets.Sprite("UI/panel"), Color.white);
            var stackIcon = UIFactory.Image("Chips Icon", bank.transform, new Vector2(110, 2), new Vector2(84, 84), GameAssets.Chip(100), Color.white);
            stackIcon.type = Image.Type.Simple;
            UIFactory.Text("Caption", bank.transform, Heb.Bankroll, 20, new Color(1, 1, 1, 0.65f), new Vector2(-40, 36), new Vector2(200, 26), TextAnchor.MiddleRight, GameAssets.Medium);
            _bankrollText = UIFactory.Text("Amount", bank.transform, "$" + _bankroll, 50, Gold, new Vector2(-40, 0), new Vector2(200, 56), TextAnchor.MiddleRight, GameAssets.Display);
            _bankrollText.rectTransform.pivot = new Vector2(1f, 0.5f);
            _bankrollText.rectTransform.anchoredPosition = new Vector2(60, 0);
            _lastHandText = UIFactory.Text("Last", bank.transform, "", 19, Cream, new Vector2(-40, -38), new Vector2(200, 26), TextAnchor.MiddleRight, GameAssets.Medium);

            // buttons
            _dealButton = UIFactory.Button("Deal", _stage, Heb.Deal, new Vector2(0, ButtonY), new Vector2(460, 84), "btn_gold", Ink, OnDeal);
            _dealButton.GetComponent<ButtonJuice>().Pulse = true;
            _rebuyButton = UIFactory.Button("Rebuy", _stage, Heb.Rebuy + " $" + startingBankroll, new Vector2(0, ButtonY), new Vector2(460, 84), "btn_blue", Cream, OnRebuy);
            _foldButton = UIFactory.Button("Fold", _stage, Heb.Fold, new Vector2(-350, ButtonY), new Vector2(300, 80), "btn_red", Cream, OnFold);
            _swapButton = UIFactory.Button("Swap", _stage, Heb.PickCard, new Vector2(0, ButtonY), new Vector2(340, 80), "btn_purple", Cream, OnSwap);
            _raiseButton = UIFactory.Button("Raise", _stage, Heb.Raise + " $" + CaribbeanStudRules.RaiseAmount, new Vector2(350, ButtonY), new Vector2(300, 80), "btn_green", Cream, OnRaise);
            _nextButton = UIFactory.Button("Next", _stage, Heb.NextHand, new Vector2(-230, ButtonY), new Vector2(320, 80), "btn_gold", Ink, OnNextHand);
            _slapButton = UIFactory.Button("Slap", _stage, Heb.SlapHim, new Vector2(200, ButtonY), new Vector2(380, 88), "btn_red", Cream, OnSlap);
            _slapButton.GetComponent<ButtonJuice>().Pulse = true;
            _musicButton = UIFactory.Button("Music", _stage, "מוזיקה: פועלת", new Vector2(840, 505), new Vector2(210, 56), "btn_blue", Cream, OnMusic);
            _musicButton.GetComponentInChildren<Text>().fontSize = 22;

            if (!Application.isMobilePlatform)
                UIFactory.Text("Keys", _stage, Heb.Keys, 16, new Color(1, 1, 1, 0.45f), new Vector2(790, -470), new Vector2(330, 60), TextAnchor.MiddleCenter, GameAssets.Medium, 30);
        }

        private GameObject Pill(Vector2 pos, out Text label)
        {
            var pill = UIFactory.Image("Hand Label", _stage, pos, new Vector2(560, 50), GameAssets.Sprite("UI/panel"), Color.white);
            label = UIFactory.Text("Text", pill.transform, "", 24, Cream, new Vector2(0, 1), new Vector2(540, 40), TextAnchor.MiddleCenter, GameAssets.Bold);
            pill.gameObject.SetActive(false);
            return pill.gameObject;
        }
    }
}
