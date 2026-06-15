using UnityEngine;
using System.Collections.Generic;
using FirstView.Gameplay;
using UnityEngine.UI;

namespace FirstView
{
    public class FirstViewScene : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private FocusCameraRig cameraRig;
        [SerializeField] private Transform focusIdle;
        [SerializeField] private Transform focusHand;
        [SerializeField] private Transform focusMyField;
        [SerializeField] private Transform focusEnemyField;
        [SerializeField] private Transform focusOpponent;

        [Header("Table References")]
        [SerializeField] private Transform tableSurface;
        [SerializeField] private Transform handAnchor;
        [SerializeField] private Transform myFieldAnchor;
        [SerializeField] private Transform enemyFieldAnchor;
        [SerializeField] private Transform opponentAnchor;
        [SerializeField] private Transform opponentHandAnchor;
        [SerializeField] private Transform deckPosition;

        [Header("Card Prefab")]
        [SerializeField] private string cardPrefabResourcePath = "P_Card_Template";
        [SerializeField] private string slotPrefabResourcePath = "P_CardSlot_Template";
        [SerializeField] private GameObject slotPrefab;

        [Header("Interaction")]
        [SerializeField] private TableInteractor interactor;

        [Header("Start Screen")]
        [SerializeField] private string startScreenPrefabResourcePath = "StartScreenCanvas";
        [SerializeField] private GameObject startScreenRoot;
        [SerializeField] private Button startButton;
        [SerializeField] private Toggle normalAIToggle;
        [SerializeField] private Toggle strongAIToggle;
        [SerializeField] private Toggle godAIToggle;

        [Header("Layout")]
        [SerializeField] private float handCardSpacing = 0.068f;
        [SerializeField] private float handFanAngle = 6f;
        [SerializeField] private float handLiftY = 0.001f;
        [SerializeField] private float fieldSlotSpacing = 0.08f;
        [SerializeField] private int fieldSlotsPerSide = 1;

        [Header("Score Displays")]
        [SerializeField] private ScoreCounterDisplay playerScoreDisplay;
        [SerializeField] private ScoreCounterDisplay enemyScoreDisplay;

        [Header("Discard Pile")]
        [SerializeField] private DiscardPile discardPile;

        private readonly List<Card3D> handCards = new List<Card3D>();
        private readonly List<Card3D> enemyHandCards = new List<Card3D>();
        private readonly List<CardSlot> myFieldSlots = new List<CardSlot>();
        private readonly List<CardSlot> enemyFieldSlots = new List<CardSlot>();
        private GameSession session;
        private Card3D pendingSelectedCard;
        private Card3D playerFieldCard;
        private Card3D enemyFieldCard;
        private int playerPlayedHandIndex;
        private GameObject cardPrefabInstance;
        private Transform playerTransform;
        private Transform opponentTransform;
        private bool gameStarted;
        private bool startupReferencesValid;
        private bool startScreenInstantiated;
        private EnemyAIDifficulty selectedAIDifficulty = EnemyAIDifficulty.Normal;

        private void Start()
        {
            session = gameObject.AddComponent<GameSession>();

            cardPrefabInstance = Resources.Load<GameObject>(cardPrefabResourcePath);
            if (cardPrefabInstance == null)
                Debug.LogError("[FirstView] Card prefab not found at Resources/" + cardPrefabResourcePath);

            playerTransform = EnsureEntity("Player", new Vector3(0f, 1.2f, -0.5f));
            opponentTransform = EnsureEntity("Opponent", new Vector3(0f, 1.2f, 1.5f));

            EnsureStartScreen();
            startupReferencesValid = ValidateStartupReferences();
            ShowStartScreen();
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGameFromStartScreen);
                startButton.onClick.AddListener(StartGameFromStartScreen);
                startButton.interactable = startupReferencesValid;
            }
            else if (IsStartScreenExpected())
            {
                Debug.LogError("[FirstView] Start screen is expected but no start button is available; game will not auto-start.");
            }
            else
            {
                Debug.LogWarning("[FirstView] Start button is not assigned; starting game immediately.");
                StartGameFromStartScreen();
            }
        }

        private void EnsureStartScreen()
        {
            if (startScreenRoot == null && !string.IsNullOrWhiteSpace(startScreenPrefabResourcePath))
            {
                GameObject startScreenPrefab = Resources.Load<GameObject>(startScreenPrefabResourcePath);
                if (startScreenPrefab == null)
                {
                    Debug.LogError("[FirstView] Start screen prefab not found at Resources/" + startScreenPrefabResourcePath);
                }
                else
                {
                    startScreenRoot = Instantiate(startScreenPrefab);
                    startScreenRoot.name = startScreenPrefab.name;
                    startScreenInstantiated = true;
                }
            }

            if (startButton == null && startScreenRoot != null)
            {
                Transform startButtonTransform = FindChildRecursive(startScreenRoot.transform, "StartGameButton");
                if (startButtonTransform != null)
                {
                    startButton = startButtonTransform.GetComponent<Button>();
                    if (startButton == null)
                    {
                        Debug.LogError("[FirstView] StartGameButton was found but it has no Button component.");
                    }
                }
            }

            ResolveAIDifficultyToggles();
        }

        private void ResolveAIDifficultyToggles()
        {
            if (startScreenRoot == null) return;

            if (normalAIToggle == null)
                normalAIToggle = FindToggleRecursive(startScreenRoot.transform, "AIDifficultyOption_普通");
            if (strongAIToggle == null)
                strongAIToggle = FindToggleRecursive(startScreenRoot.transform, "AIDifficultyOption_强力");
            if (godAIToggle == null)
                godAIToggle = FindToggleRecursive(startScreenRoot.transform, "AIDifficultyOption_神级");
        }

        private static Toggle FindToggleRecursive(Transform parent, string childName)
        {
            Transform child = FindChildRecursive(parent, childName);
            return child != null ? child.GetComponent<Toggle>() : null;
        }

        private bool IsStartScreenExpected()
        {
            return !string.IsNullOrWhiteSpace(startScreenPrefabResourcePath) || startScreenRoot != null;
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent == null) return null;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child.name == childName) return child;

                Transform match = FindChildRecursive(child, childName);
                if (match != null) return match;
            }

            return null;
        }

        private void ShowStartScreen()
        {
            if (startScreenRoot != null)
            {
                startScreenRoot.SetActive(true);
            }
        }

        private void StartGameFromStartScreen()
        {
            if (gameStarted) return;

            if (!startupReferencesValid && !ValidateStartupReferences())
            {
                if (startButton != null)
                {
                    startButton.interactable = false;
                }
                return;
            }

            gameStarted = true;
            selectedAIDifficulty = ReadSelectedAIDifficulty();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[FirstView] Selected AI difficulty: {selectedAIDifficulty}");
#endif

            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGameFromStartScreen);
                startButton.interactable = false;
            }

            if (startScreenRoot != null)
            {
                startScreenRoot.SetActive(false);
            }

            CreateFieldSlots();
            WireSession();
            WireInteraction();
            cameraRig.Initialize("Hand");
            session.BeginGame();
        }

        private EnemyAIDifficulty ReadSelectedAIDifficulty()
        {
            ResolveAIDifficultyToggles();

            if (godAIToggle != null && godAIToggle.isOn) return EnemyAIDifficulty.God;
            if (strongAIToggle != null && strongAIToggle.isOn) return EnemyAIDifficulty.Strong;
            return EnemyAIDifficulty.Normal;
        }

        private bool ValidateStartupReferences()
        {
            bool valid = true;

            valid &= ValidateReference(session, nameof(session));
            valid &= ValidateReference(cardPrefabInstance, nameof(cardPrefabInstance));
            valid &= ValidateReference(cameraRig, nameof(cameraRig));
            valid &= ValidateReference(tableSurface, nameof(tableSurface));
            valid &= ValidateReference(handAnchor, nameof(handAnchor));
            valid &= ValidateReference(myFieldAnchor, nameof(myFieldAnchor));
            valid &= ValidateReference(enemyFieldAnchor, nameof(enemyFieldAnchor));
            valid &= ValidateReference(interactor, nameof(interactor));

            if (startScreenRoot == null && startButton != null)
            {
                Debug.LogError("[FirstView] Start screen root is not assigned while start button is assigned.");
                valid = false;
            }

            if (startScreenRoot == null && IsStartScreenExpected())
            {
                Debug.LogError("[FirstView] Start screen root is missing and prefab loading failed.");
                valid = false;
            }

            if (startScreenRoot != null && startButton == null && IsStartScreenExpected())
            {
                Debug.LogError("[FirstView] Start screen prefab is missing a StartGameButton with a Button component.");
                valid = false;
            }

            if (startScreenRoot != null && startButton == null && !IsStartScreenExpected())
            {
                Debug.LogWarning("[FirstView] Start screen root is assigned but start button is missing; game will auto-start.");
            }

            if (startScreenRoot != null && startScreenRoot.GetComponentInParent<Canvas>() == null)
            {
                Debug.LogError("[FirstView] Start screen root must include a Canvas component or be parented under one.");
                valid = false;
            }

            if (startScreenRoot != null && normalAIToggle == null && strongAIToggle == null && godAIToggle == null)
            {
                Debug.LogWarning("[FirstView] AI difficulty toggles were not found; defaulting to Normal AI.");
            }

            if (startScreenInstantiated && startScreenRoot != null && startScreenRoot.scene != gameObject.scene)
            {
                Debug.LogError("[FirstView] Instantiated start screen is not in the active gameplay scene.");
                valid = false;
            }

            startupReferencesValid = valid;
            return valid;
        }

        private static bool ValidateReference(Object reference, string fieldName)
        {
            if (reference != null) return true;

            Debug.LogError($"[FirstView] Required startup reference is missing: {fieldName}");
            return false;
        }

        private static Transform EnsureEntity(string name, Vector3 pos)
        {
            var go = GameObject.Find(name);
            if (go == null)
            {
                go = new GameObject(name);
                go.transform.position = pos;
            }
            return go.transform;
        }

        private void SetupFocusPoints()
        {
            // Configure focus targets on the camera rig at runtime
            // (These are set via serialized fields on the prefab/scene)
        }

        private void CreateFieldSlots()
        {
            // Create player field slots
            Vector3 myStart = myFieldAnchor.position - myFieldAnchor.right * (fieldSlotSpacing * (fieldSlotsPerSide - 1) * 0.5f);
            for (int i = 0; i < fieldSlotsPerSide; i++)
            {
                Vector3 pos = myStart + myFieldAnchor.right * (fieldSlotSpacing * i);
                CreateSlot(pos, myFieldAnchor.rotation, SlotOwner.Player, i, myFieldSlots);
            }

            // Create enemy field slots
            Vector3 enemyStart = enemyFieldAnchor.position - enemyFieldAnchor.right * (fieldSlotSpacing * (fieldSlotsPerSide - 1) * 0.5f);
            for (int i = 0; i < fieldSlotsPerSide; i++)
            {
                Vector3 pos = enemyStart + enemyFieldAnchor.right * (fieldSlotSpacing * i);
                CreateSlot(pos, enemyFieldAnchor.rotation, SlotOwner.Opponent, i, enemyFieldSlots);
            }
        }

        private void CreateSlot(Vector3 pos, Quaternion rot, SlotOwner owner, int index, List<CardSlot> list)
        {
            GameObject slotObj;
            var slotTemplate = Resources.Load<GameObject>(slotPrefabResourcePath);
            if (slotTemplate != null)
            {
                slotObj = Instantiate(slotTemplate, pos, rot, tableSurface);
                slotObj.transform.localScale = Vector3.one * 0.1f;
            }
            else
            {
                slotObj = CreateDefaultSlotVisual(pos, rot);
            }

            var slot = slotObj.GetComponent<CardSlot>();
            if (slot == null) slot = slotObj.AddComponent<CardSlot>();
            slot.slotId = $"{owner}_Slot_{index}";
            slot.owner = owner;
            slot.slotIndex = index;
            list.Add(slot);
        }

        private GameObject CreateDefaultSlotVisual(Vector3 pos, Quaternion rot)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.transform.localScale = new Vector3(0.096f, 0.136f, 1f);
            obj.layer = LayerMask.NameToLayer("Default");

            // Remove collider from slot visual (we add BoxCollider for CardSlot)
            var col = obj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var boxCol = obj.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(1f, 1f, 0.1f);
            boxCol.isTrigger = true;

            // Set transparent material
            var renderer = obj.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.SetColor("_BaseColor", new Color(0.8f, 0.7f, 0.3f, 0.15f));
            renderer.material = mat;

            return obj;
        }

        private void DealCards()
        {
            if (session.PlayerHand == null) return;

            int playerCount = session.PlayerHand.Count;
            for (int i = 0; i < playerCount; i++)
            {
                GameCard gc = session.PlayerHand[i];
                Card3D card = CreateCard(gc);
                card.facing = CardFacing.FacePlayer;
                card.faceTarget = playerTransform;

                float normalizedPos = (float)i / Mathf.Max(1, playerCount - 1) - 0.5f;
                float angle = normalizedPos * handFanAngle;
                float xOffset = normalizedPos * handCardSpacing * (playerCount - 1);
                float yOffset = Mathf.Abs(normalizedPos) * handLiftY * 2f;

                Vector3 cardPos = handAnchor.position + handAnchor.right * xOffset + handAnchor.up * yOffset;

                card.SetBasePose(cardPos, Quaternion.Euler(0f, angle, 0f));
                card.PlayDrawAnimation(
                    deckPosition != null ? deckPosition.position : cardPos + Vector3.up * 0.1f,
                    Quaternion.Euler(0f, angle, 0f),
                    i * 0.1f);
                handCards.Add(card);
            }

            Transform enemyAnchor = opponentHandAnchor != null ? opponentHandAnchor : opponentAnchor;
            if (enemyAnchor != null)
            {
                int enemyCount = session.EnemyHand.Count;
                for (int i = 0; i < enemyCount; i++)
                {
                    GameCard gc = session.EnemyHand[i];
                    Card3D card = CreateCard(gc);
                    card.facing = CardFacing.FaceEnemy;
                    card.faceTarget = opponentTransform;

                    float normalizedPos = (float)i / Mathf.Max(1, enemyCount - 1) - 0.5f;
                    float xOffset = normalizedPos * handCardSpacing * (enemyCount - 1);
                    float yOffset = Mathf.Abs(normalizedPos) * handLiftY * 2f;

                    Vector3 cardPos = enemyAnchor.position + enemyAnchor.right * xOffset + enemyAnchor.up * yOffset;

                    card.SetBasePose(cardPos, Quaternion.identity);
                    card.PlayDrawAnimation(
                        cardPos + Vector3.up * 0.1f,
                        Quaternion.identity,
                        i * 0.1f);
                    enemyHandCards.Add(card);
                }
            }
        }

        private static readonly Dictionary<CardColor, string> ColorNames = new Dictionary<CardColor, string>
        {
            { CardColor.Green, "绿" },
            { CardColor.Blue, "蓝" },
            { CardColor.Red, "红" }
        };

        private Card3D CreateCard(GameCard gc)
        {
            if (cardPrefabInstance == null) return null;

            GameObject cardObj = Instantiate(cardPrefabInstance);
            cardObj.name = "Card_" + gc.ToString();

            var card = cardObj.GetComponent<Card3D>();
            if (card == null) card = cardObj.AddComponent<Card3D>();

            card.cardId = gc.ToString();
            card.cardName = gc.ToString();

            string cn = ColorNames[gc.Color];
            Sprite front = Resources.Load<Sprite>($"Cards/{cn}{gc.Number}");
            Sprite back = Resources.Load<Sprite>($"Cards/{cn}背景");

            if (front != null) card.SetFrontSprite(front);
            if (back != null) card.SetBackSprite(back);

            return card;
        }

        private void WireSession()
        {
            session.OnRoundStart += HandleRoundStart;
            session.OnTurnStart += HandleTurnStart;
            session.OnBothCardsPlayed += HandleBothCardsPlayed;
            session.OnSettled += HandleSettled;

            if (playerScoreDisplay != null) playerScoreDisplay.SetValueImmediate(0);
            if (enemyScoreDisplay != null) enemyScoreDisplay.SetValueImmediate(0);
        }

        private void HandleRoundStart()
        {
            ClearFieldCards();
            if (handCards.Count == 0 && enemyHandCards.Count == 0)
                DealCards();
        }

        private void HandleTurnStart(bool isPlayerTurn)
        {
            if (!isPlayerTurn)
                StartCoroutine(EnemyPlayCoroutine());
        }

        private System.Collections.IEnumerator EnemyPlayCoroutine()
        {
            yield return new UnityEngine.WaitForSeconds(1f);
            EnemyPlay();
        }

        private void EnemyPlay()
        {
            if (session.Phase != RoundPhase.FirstTurn && session.Phase != RoundPhase.SecondTurn) return;
            if (session.PlayerIsFirst && session.Phase == RoundPhase.FirstTurn) return;
            if (!session.PlayerIsFirst && session.Phase == RoundPhase.SecondTurn) return;

            EnemyAIDecisionContext aiContext = BuildEnemyAIContext();
            int aiPick = FirstView.Gameplay.EnemyAI.PickCardIndex(session.EnemyHand, aiContext);
            if (aiPick < 0) return;

            Card3D card = enemyHandCards[aiPick];
            enemyHandCards.RemoveAt(aiPick);
            card.facing = CardFacing.FaceDown;
            card.SetSlotTransform(enemyFieldSlots[0].transform);
            enemyFieldSlots[0].PlaceCard(card);
            enemyFieldCard = card;
            RearrangeEnemyHand();

            session.SetPlayedIndex(false, aiPick);
            session.OnCardPlayed(false);
        }

        private EnemyAIDecisionContext BuildEnemyAIContext()
        {
            List<PublicCardInfo> playerPublicCards = new List<PublicCardInfo>();
            if (session.PlayerHand != null)
            {
                for (int i = 0; i < session.PlayerHand.Count; i++)
                {
                    playerPublicCards.Add(new PublicCardInfo(session.PlayerHand[i].Color));
                }
            }

            bool hasPlayerPlayedPublicCard = session.PlayerIsFirst
                && session.PlayerPlayedIndex >= 0
                && session.PlayerHand != null
                && session.PlayerPlayedIndex < session.PlayerHand.Count;

            PublicCardInfo playerPlayedPublicCard = hasPlayerPlayedPublicCard
                ? new PublicCardInfo(session.PlayerHand[session.PlayerPlayedIndex].Color)
                : default;

            return new EnemyAIDecisionContext(
                selectedAIDifficulty,
                playerPublicCards,
                hasPlayerPlayedPublicCard,
                playerPlayedPublicCard,
                session.SettledHistory,
                new PublicCardInfo(session.RemovedCard.Color),
                session.CurrentRound,
                session.PlayerScore,
                session.EnemyScore,
                session.PlayerIsFirst);
        }

        private void HandleBothCardsPlayed()
        {
            StartCoroutine(RevealCoroutine());
        }

        private System.Collections.IEnumerator RevealCoroutine()
        {
            yield return new UnityEngine.WaitForSeconds(0.5f);

            if (playerFieldCard != null) playerFieldCard.FlipReveal();
            if (enemyFieldCard != null) enemyFieldCard.FlipReveal();

            // Flip takes 0.4s; wait for flip + display time
            yield return new UnityEngine.WaitForSeconds(1.2f);

            session.Settle();
        }

        private void HandleSettled(int result, int score, int round)
        {
            if (discardPile != null)
            {
                if (playerFieldCard != null) discardPile.AddCard(playerFieldCard);
                if (enemyFieldCard != null) discardPile.AddCard(enemyFieldCard);
            }
            playerFieldCard = null;
            enemyFieldCard = null;

            if (playerScoreDisplay != null)
                playerScoreDisplay.AnimateTo(session.PlayerScore);
            if (enemyScoreDisplay != null)
                enemyScoreDisplay.AnimateTo(session.EnemyScore);

            cameraRig.FocusTo("Hand");
        }

        private void ClearFieldCards()
        {
            if (playerFieldCard != null && discardPile != null) discardPile.AddCard(playerFieldCard);
            if (enemyFieldCard != null && discardPile != null) discardPile.AddCard(enemyFieldCard);
            playerFieldCard = null;
            enemyFieldCard = null;

            foreach (var slot in myFieldSlots) slot.RemoveCard();
            foreach (var slot in enemyFieldSlots) slot.RemoveCard();
        }

        private void WireInteraction()
        {
            if (interactor == null) return;

            interactor.OnCardClicked += HandleCardClicked;
            interactor.OnCardPlaced += HandleCardPlaced;
            interactor.OnCardDeselected += HandleCardDeselected;
            interactor.OnEnvironmentClicked += HandleEnvironmentClicked;
            interactor.OnDiscardPileClicked += HandleDiscardPileClicked;
        }

        private void HandleCardClicked(Card3D card)
        {
            bool isPlayerTurn = (session.PlayerIsFirst && session.Phase == RoundPhase.FirstTurn)
                             || (!session.PlayerIsFirst && session.Phase == RoundPhase.SecondTurn);

            if (isPlayerTurn && handCards.Contains(card))
            {
                interactor.SelectCard(card);
                pendingSelectedCard = card;
                cameraRig.FocusTo("MyField");
                HighlightPlayerSlots(true);
            }
        }

        private void HandleDiscardPileClicked(DiscardPile pile)
        {
            pile.ToggleExpand();
            cameraRig.FocusTo(pile.IsExpanded ? "DiscardPile" : "Hand");
        }

        private void HandleCardPlaced(Card3D card, CardSlot slot)
        {
            if (!handCards.Contains(card)) return;
            if (!myFieldSlots.Contains(slot)) return;
            if (slot.isOccupied) return;

            playerPlayedHandIndex = handCards.IndexOf(card);
            handCards.Remove(card);
            card.facing = CardFacing.FaceDown;
            card.SetSlotTransform(slot.transform);
            slot.PlaceCard(card);
            playerFieldCard = card;
            RearrangeHand();
            HighlightPlayerSlots(false);
            pendingSelectedCard = null;

            session.SetPlayedIndex(true, playerPlayedHandIndex);
            session.OnCardPlayed(true);
        }

        private void HandleCardDeselected(Card3D _)
        {
            HighlightPlayerSlots(false);
            pendingSelectedCard = null;
        }

        private void HandleEnvironmentClicked(string focusId)
        {
            cameraRig.FocusTo(focusId);
        }

        private void HighlightPlayerSlots(bool on)
        {
            foreach (var slot in myFieldSlots)
            {
                if (!slot.isOccupied)
                    slot.SetHighlight(on);
            }
        }

        private void RearrangeHand()
        {
            int count = handCards.Count;
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                float normalizedPos = count == 1 ? 0f : (float)i / (count - 1) - 0.5f;
                float angle = normalizedPos * handFanAngle;
                float xOffset = normalizedPos * handCardSpacing * (count - 1);
                float yOffset = Mathf.Abs(normalizedPos) * handLiftY * 2f;

                Vector3 cardPos = handAnchor.position + handAnchor.right * xOffset + handAnchor.up * yOffset;
                Quaternion cardRot = Quaternion.Euler(0f, angle, 0f);

                handCards[i].PlayPlaceAnimation(cardPos, cardRot);
                handCards[i].SetBasePose(cardPos, cardRot);
            }
        }

        private void RearrangeEnemyHand()
        {
            int count = enemyHandCards.Count;
            if (count == 0) return;

            Transform enemyAnchor = opponentHandAnchor != null ? opponentHandAnchor : opponentAnchor;
            if (enemyAnchor == null) return;

            for (int i = 0; i < count; i++)
            {
                float normalizedPos = count == 1 ? 0f : (float)i / (count - 1) - 0.5f;
                float xOffset = normalizedPos * handCardSpacing * (count - 1);
                float yOffset = Mathf.Abs(normalizedPos) * handLiftY * 2f;

                Vector3 cardPos = enemyAnchor.position + enemyAnchor.right * xOffset + enemyAnchor.up * yOffset;
                Quaternion cardRot = Quaternion.identity;

                enemyHandCards[i].PlayPlaceAnimation(cardPos, cardRot);
                enemyHandCards[i].SetBasePose(cardPos, cardRot);
            }
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGameFromStartScreen);
            }

            if (interactor != null)
            {
                interactor.OnCardClicked -= HandleCardClicked;
                interactor.OnCardPlaced -= HandleCardPlaced;
                interactor.OnCardDeselected -= HandleCardDeselected;
                interactor.OnEnvironmentClicked -= HandleEnvironmentClicked;
                interactor.OnDiscardPileClicked -= HandleDiscardPileClicked;
            }
            if (session != null)
            {
                session.OnRoundStart -= HandleRoundStart;
                session.OnTurnStart -= HandleTurnStart;
                session.OnBothCardsPlayed -= HandleBothCardsPlayed;
                session.OnSettled -= HandleSettled;
            }
        }
    }
}
