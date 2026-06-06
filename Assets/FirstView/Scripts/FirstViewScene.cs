using UnityEngine;
using System.Collections.Generic;

namespace FirstView
{
    /// <summary>
    /// Main scene orchestrator. Sets up the 3D first-person card table
    /// with mock data, manages card hand layout, and wires interactions.
    /// </summary>
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
        [SerializeField] private Transform deckPosition;

        [Header("Card Prefab")]
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private GameObject slotPrefab;

        [Header("Interaction")]
        [SerializeField] private TableInteractor interactor;

        [Header("Layout")]
        [SerializeField] private float handCardSpacing = 0.55f;
        [SerializeField] private float handFanAngle = 8f;
        [SerializeField] private float handLiftY = 0.01f;
        [SerializeField] private float fieldSlotSpacing = 0.6f;
        [SerializeField] private int fieldSlotsPerSide = 4;

        [Header("Mock")]
        [SerializeField] private int mockHandSize = 5;
        [SerializeField] private int mockFieldCards = 2;

        private readonly List<Card3D> handCards = new List<Card3D>();
        private readonly List<Card3D> enemyFieldCards = new List<Card3D>();
        private readonly List<CardSlot> myFieldSlots = new List<CardSlot>();
        private readonly List<CardSlot> enemyFieldSlots = new List<CardSlot>();
        private MockCardDB cardDB;

        private void Start()
        {
            cardDB = MockCardDB.CreateDefault();
            SetupFocusPoints();
            CreateFieldSlots();
            DealMockCards();
            WireInteraction();

            cameraRig.Initialize("Idle");
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
            if (slotPrefab != null)
            {
                slotObj = Instantiate(slotPrefab, pos, rot, tableSurface);
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
            obj.transform.localScale = new Vector3(0.48f, 0.68f, 1f);
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

        private void DealMockCards()
        {
            if (cardDB == null || cardDB.cards == null || cardDB.cards.Length == 0) return;

            // Deal hand cards with fan layout
            for (int i = 0; i < mockHandSize; i++)
            {
                var entry = cardDB.cards[i % cardDB.cards.Length];
                Card3D card = CreateCard(entry);

                // Fan layout
                float normalizedPos = (float)i / Mathf.Max(1, mockHandSize - 1) - 0.5f; // -0.5 to 0.5
                float angle = normalizedPos * handFanAngle;
                float xOffset = normalizedPos * handCardSpacing * (mockHandSize - 1);
                float yOffset = Mathf.Abs(normalizedPos) * handLiftY * 2f;

                Vector3 cardPos = handAnchor.position + handAnchor.right * xOffset + handAnchor.up * yOffset;
                Quaternion cardRot = handAnchor.rotation * Quaternion.Euler(0f, angle, 0f);

                card.SetBasePose(cardPos, cardRot);
                card.PlayDrawAnimation(
                    deckPosition != null ? deckPosition.position : cardPos + Vector3.up * 0.5f,
                    cardRot,
                    i * 0.15f);
                handCards.Add(card);
            }

            // Place some mock enemy cards
            for (int i = 0; i < mockFieldCards && i < enemyFieldSlots.Count; i++)
            {
                var entry = cardDB.cards[(mockHandSize + i) % cardDB.cards.Length];
                Card3D card = CreateCard(entry);
                card.SetBasePose(enemyFieldSlots[i].GetCardPosition(), enemyFieldSlots[i].GetCardRotation());
                enemyFieldSlots[i].PlaceCard(card);
                enemyFieldCards.Add(card);
            }
        }

        private Card3D CreateCard(MockCardDB.CardEntry entry)
        {
            GameObject cardObj;

            if (cardPrefab != null)
            {
                cardObj = Instantiate(cardPrefab);
            }
            else
            {
                cardObj = CreateDefaultCardVisual();
            }

            var card = cardObj.GetComponent<Card3D>();
            if (card == null) card = cardObj.AddComponent<Card3D>();

            card.cardId = entry.id;
            card.cardName = entry.displayName;
            card.attack = entry.attack;
            card.health = entry.health;
            card.ability = entry.ability;
            card.cost = entry.cost;
            card.rarity = entry.rarity;

            cardObj.name = $"Card_{entry.displayName}";
            cardObj.layer = LayerMask.NameToLayer("Default");

            return card;
        }

        private GameObject CreateDefaultCardVisual()
        {
            // Card body: thin box
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.transform.localScale = new Vector3(0.5f, 0.005f, 0.7f);

            // Card material
            var renderer = obj.GetComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.12f, 0.1f, 0.08f));
            renderer.material = mat;

            // BoxCollider for interaction (Card3D sets it up in Awake)

            // Create card face child (simple quad with text)
            var faceObj = new GameObject("CardFace");
            faceObj.transform.SetParent(obj.transform, false);
            faceObj.transform.localPosition = new Vector3(0f, 0.5f, 0f); // On top face
            faceObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            faceObj.transform.localScale = new Vector3(0.9f, 1.3f, 1f);

            var faceRenderer = faceObj.AddComponent<MeshRenderer>();
            var faceMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            faceMat.SetColor("_BaseColor", new Color(0.95f, 0.92f, 0.85f));
            faceRenderer.material = faceMat;
            faceObj.AddComponent<MeshFilter>().mesh = CreateQuadMesh();

            return obj;
        }

        private static Mesh CreateQuadMesh()
        {
            var mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f)
            };
            mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            mesh.uv = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f)
            };
            mesh.RecalculateNormals();
            return mesh;
        }

        private void WireInteraction()
        {
            if (interactor == null) return;

            interactor.OnCardClicked += HandleCardClicked;
            interactor.OnSlotClicked += HandleSlotClicked;
            interactor.OnEnvironmentClicked += HandleEnvironmentClicked;
        }

        private void HandleCardClicked(Card3D card)
        {
            Debug.Log($"[FirstView] Card clicked: {card.cardName}");
            // For prototype: just focus to the relevant area
            if (handCards.Contains(card))
                cameraRig.FocusTo("Hand");
            else if (enemyFieldCards.Contains(card))
                cameraRig.FocusTo("EnemyField");
        }

        private void HandleSlotClicked(CardSlot slot)
        {
            Debug.Log($"[FirstView] Slot clicked: {slot.slotId}");
            if (slot.owner == SlotOwner.Player)
                cameraRig.FocusTo("MyField");
        }

        private void HandleEnvironmentClicked(string focusId)
        {
            Debug.Log($"[FirstView] Environment clicked -> focus: {focusId}");
            cameraRig.FocusTo(focusId);
        }

        private void OnDestroy()
        {
            if (interactor != null)
            {
                interactor.OnCardClicked -= HandleCardClicked;
                interactor.OnSlotClicked -= HandleSlotClicked;
                interactor.OnEnvironmentClicked -= HandleEnvironmentClicked;
            }
        }
    }
}
