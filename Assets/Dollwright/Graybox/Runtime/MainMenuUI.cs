using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Dollwright.Graybox
{
    public sealed class MainMenuUI : MonoBehaviour
    {
        public const string ScenePath = "Assets/Dollwright/Graybox/Scenes/MainMenu.unity";
        public Button newGameButton, continueButton, settingsButton, settingsBackButton;
        public GameObject mainPanel, settingsPanel;
        public Text errorLabel;
        bool busy;
        Font font;

        void Awake()
        {
            Time.timeScale = 1;
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 24);
            foreach (var text in GetComponentsInChildren<Text>(true)) text.font = font;
            newGameButton.onClick.AddListener(NewGame);
            continueButton.onClick.AddListener(ContinueGame);
            settingsButton.onClick.AddListener(OpenSettings);
            settingsBackButton.onClick.AddListener(CloseSettings);
            settingsPanel.SetActive(false); mainPanel.SetActive(true);
            errorLabel.text = "";
            RefreshButtons();
        }
        public void RefreshButtons()
        {
            newGameButton.interactable = settingsButton.interactable = !busy;
            continueButton.interactable = !busy && DollCraftingStore.Current.HasSavedGame;
        }
        bool CanEnter()
        {
            if (busy || DreamTravel.IsLoading) return false;
            if (SceneUtility.GetBuildIndexByScenePath(DreamTravel.WorkbenchScene) >= 0) return true;
            errorLabel.text = "工作台场景未配置"; return false;
        }
        public void NewGame()
        {
            if (!CanEnter()) return;
            if (!DollCraftingStore.Current.StartNewGame())
            { errorLabel.text = "无法保存，请检查存储权限或空间"; RefreshButtons(); return; }
            EnterWorkbench();
        }
        public void ContinueGame()
        {
            if (!CanEnter()) return;
            if (!DollCraftingStore.Current.ReloadSavedGame())
            { errorLabel.text = "没有可读取的存档"; RefreshButtons(); return; }
            EnterWorkbench();
        }
        void EnterWorkbench()
        {
            busy = true; RefreshButtons();
            DreamTravel.ResetSession();
            if (!DreamTravel.TravelTo(DreamTravel.WorkbenchScene))
            { busy = false; errorLabel.text = "无法进入工作台"; RefreshButtons(); }
        }
        public void OpenSettings()
        { if (busy) return; mainPanel.SetActive(false); settingsPanel.SetActive(true); }
        public void CloseSettings()
        { settingsPanel.SetActive(false); mainPanel.SetActive(true); RefreshButtons(); }
        void Update()
        { if (settingsPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape)) CloseSettings(); }
        public static void ReturnToMenu()
        {
            if (DreamTravel.IsLoading) return;
            DreamTravel.ResetSession();
            DreamTravel.TravelTo(ScenePath);
        }
        void OnDestroy() { if (font) Destroy(font); }
    }
}
