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
        public GameObject backButton;
        public float focusFieldOfView = 24;
        public float transitionSeconds = .8f;
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
        }
        void Update()
        {
            if (progress < 1)
            {
                progress = Mathf.Min(1, progress + Time.unscaledDeltaTime / Mathf.Max(.01f, transitionSeconds));
                float t = Mathf.SmoothStep(0, 1, progress);
                viewCamera.transform.SetPositionAndRotation(
                    Vector3.Lerp(fromPosition, IsSelecting ? focusPose.position : homePosition, t),
                    Quaternion.Slerp(fromRotation, IsSelecting ? focusPose.rotation : homeRotation, t));
                viewCamera.fieldOfView = Mathf.Lerp(fromFov, IsSelecting ? focusFieldOfView : homeFov, t);
            }
            if (IsSelecting && Input.GetKeyDown(KeyCode.Escape)) CloseSelection();
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
            if (DreamTravel.IsLoading || focusPose == null || IsSelecting) return;
            BeginTransition();
            IsSelecting = true;
            SelectedDoll = null;
            bedButton.gameObject.SetActive(false);
            backButton.SetActive(true);
        }
        public void CloseSelection()
        {
            if (DreamTravel.IsLoading || !IsSelecting) return;
            BeginTransition();
            IsSelecting = false;
            SelectedDoll = null;
            backButton.SetActive(false);
            bedButton.gameObject.SetActive(true);
        }
        public bool TrySelectAtScreenPoint(Vector2 point)
        {
            if (!FocusReady || dollCollider == null) return false;
            if (!Physics.Raycast(viewCamera.ScreenPointToRay(point), out var hit, 100)) return false;
            if (hit.collider != dollCollider) return false;
            SelectInitialDoll();
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
