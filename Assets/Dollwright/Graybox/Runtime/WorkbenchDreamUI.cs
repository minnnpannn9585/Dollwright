using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dollwright.Graybox
{
    public sealed class WorkbenchDreamUI : MonoBehaviour
    {
        public DollDefinition initialDoll;
        public Button bedButton;
        public Camera viewCamera;
        public Transform focusPose;
        public Collider dollCollider;
        public Collider magicianCollider;
        public DollDefinition magicianDoll;
        public GameObject backButton;
        public float focusFieldOfView = 24;
        public float transitionSeconds = .8f;
        public WorkbenchCraftingUI crafting;
        public Transform craftingPose;
        public Collider craftingPlatform;
        public float craftingFieldOfView = 29;
        public bool IsCrafting { get; private set; }
        public bool CraftReady => IsCrafting && progress >= 1 && !DreamTravel.IsLoading;
        public bool HomeReady => !IsSelecting && !IsCrafting && progress >= 1 && !DreamTravel.IsLoading;
        public bool IsSelecting { get; private set; }
        public bool FocusReady => IsSelecting && progress >= 1 && !DreamTravel.IsLoading;
        public DollDefinition SelectedDoll { get; private set; }
        // Retained only so older migration scripts compile. No legacy panel is built or used.
        [HideInInspector] public GameObject selectionPanel;
        [HideInInspector] public Button dollButton, enterButton;
        [HideInInspector] public Text nameLabel, dreamLabel, abilitiesLabel, summaryLabel, statusLabel;
        [HideInInspector] public Image selectionBorder;
        Vector3 homePosition, fromPosition;
        Quaternion homeRotation, fromRotation;
        float homeFov, fromFov, progress = 1;
        Font font;
        void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
            homePosition = viewCamera.transform.position;
            homeRotation = viewCamera.transform.rotation;
            homeFov = viewCamera.fieldOfView;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 24);
            foreach (var label in GetComponentsInChildren<Text>(true)) label.font = font;
            if (backButton != null) backButton.SetActive(false);
            if (crafting != null) crafting.Initialize(this);
        }
        void Update()
        {
            if (progress < 1)
            {
                progress = Mathf.Min(1, progress + Time.unscaledDeltaTime / Mathf.Max(.01f, transitionSeconds));
                float t = Mathf.SmoothStep(0, 1, progress);
                viewCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(fromPosition, IsSelecting ? focusPose.position : IsCrafting ? craftingPose.position : homePosition, t),
                    Quaternion.Slerp(fromRotation, IsSelecting ? focusPose.rotation : IsCrafting ? craftingPose.rotation : homeRotation, t));
                viewCamera.fieldOfView = Mathf.Lerp(fromFov, IsSelecting ? focusFieldOfView : IsCrafting ? craftingFieldOfView : homeFov, t);
            }
            if ((IsSelecting || IsCrafting) && Input.GetKeyDown(KeyCode.Escape))
            {
                if(crafting!=null && crafting.IsProcessingBody)crafting.Workshop.Cancel();
                else CloseSelection();
            }
            if (HomeReady && Input.GetMouseButtonDown(0)
                && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                TryOpenCraftingAtScreenPoint(Input.mousePosition);
            if (FocusReady && Input.GetMouseButtonDown(0) &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
                TrySelectAtScreenPoint(Input.mousePosition);
        }
        void BeginTransition()
        {
            fromPosition = viewCamera.transform.position;
            fromRotation = viewCamera.transform.rotation;
            fromFov = viewCamera.fieldOfView;
            progress = 0;
        }
        public void OpenSelection()
        {
            if (DreamTravel.IsLoading || focusPose == null || !HomeReady) return;
            if (crafting != null && crafting.VisibleDolls.Count == 0) return;
            BeginTransition();
            IsSelecting = true;
            SelectedDoll = null;
            bedButton.gameObject.SetActive(false);
            backButton.SetActive(true);
            if (crafting != null) crafting.SetShelfMode(true);
        }
        public void CloseSelection()
        {
            if (IsCrafting) { CloseCrafting(); return; }
            if (DreamTravel.IsLoading || !IsSelecting) return;
            BeginTransition();
            IsSelecting = false;
            SelectedDoll = null;
            backButton.SetActive(false);
            bedButton.gameObject.SetActive(true);
            if (crafting != null) crafting.SetShelfMode(false);
        }
        public void OpenCrafting()
        {
            if (!HomeReady || crafting == null || craftingPose == null) return;
            BeginTransition();
            IsCrafting = true;
            bedButton.gameObject.SetActive(false);
            backButton.SetActive(true);
            crafting.SetCraftingMode(true);
        }
        public bool TryOpenCraftingAtScreenPoint(Vector2 point)
        {
            if (!HomeReady || craftingPlatform == null) return false;
            if (!Physics.Raycast(viewCamera.ScreenPointToRay(point), out var hit, 100)
                || hit.collider != craftingPlatform) return false;
            OpenCrafting();
            return IsCrafting;
        }
        public void CloseCrafting()
        {
            if (!IsCrafting || DreamTravel.IsLoading) return;
            BeginTransition();
            IsCrafting = false;
            crafting.SetCraftingMode(false);
            backButton.SetActive(false);
            bedButton.gameObject.SetActive(true);
        }
        public bool TrySelectAtScreenPoint(Vector2 point)
        {
            if (!FocusReady || dollCollider == null) return false;
            if (!Physics.Raycast(viewCamera.ScreenPointToRay(point), out var hit, 100)) return false;
            var handle = hit.collider.GetComponent<ShelfDollHandle>();
            if (handle != null) SelectedDoll = handle.definition;
            else if (hit.collider == dollCollider) SelectedDoll = initialDoll;
            else if (hit.collider == magicianCollider) SelectedDoll = magicianDoll;
            else return false;
            if (SelectedDoll == null || string.IsNullOrEmpty(SelectedDoll.dreamScenePath))
            {
                if (crafting != null) crafting.ShowUnavailableDream();
                return false;
            }
            EnterSelectedDream();
            return true;
        }
        public void SelectInitialDoll() { if (FocusReady) SelectedDoll = initialDoll; }
        public void EnterSelectedDream()
        {
            if (!FocusReady || SelectedDoll == null) return;
            DreamTravel.Enter(SelectedDoll);
        }
        void OnDestroy() { if (font != null) Destroy(font); }
    }
}
