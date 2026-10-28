using UnityEngine;
using UnityEngine.UI;

namespace Dollwright.Graybox
{
    [RequireComponent(typeof(Button))]
    public sealed class WorkbenchMenuButton : MonoBehaviour
    {
        Font font;
        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(MainMenuUI.ReturnToMenu);
            font = Font.CreateDynamicFontFromOSFont(new[] { "Microsoft YaHei", "Arial" }, 20);
            GetComponentInChildren<Text>().font = font;
        }
        void OnDestroy() { if (font) Destroy(font); }
    }
}
