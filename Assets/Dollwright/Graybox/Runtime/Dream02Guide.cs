using UnityEngine;
namespace Dollwright.Graybox
{
    public sealed class Dream02Guide : MonoBehaviour
    {
        public bool entryLesson;
        public int section;
        public bool ReachedEnd { get; set; }
        GrayboxRun run;
        DreamPhaseAbility phase;
        Font font;
        GUIStyle style, title;
        void Start() { run=GetComponent<GrayboxRun>(); phase=run.player.GetComponent<DreamPhaseAbility>(); }
        void Update()
        {
            if (run != null && !run.Rewind.IsRewinding && Input.GetKeyDown(KeyCode.T)) DreamTravel.TravelTo(DreamTravel.WorkbenchScene);
        }
        void OnGUI()
        {
            if (run==null) return;
            if(style==null)
            {
                font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},20);
                style=new GUIStyle(GUI.skin.label){font=font,fontSize=20,wordWrap=true};
                title=new GUIStyle(style){fontSize=24,fontStyle=FontStyle.Bold};
            }
            GUI.color=new Color(.08f,.10f,.14f,.92f);
            GUI.DrawTexture(new Rect(18,18,Mathf.Min(Screen.width-36,1020),116),Texture2D.whiteTexture);
            GUI.color=Color.white;
            GUI.Label(new Rect(34,26,Screen.width-68,32), entryLesson ? "梦境 02 · 魔术箱教学" : section==3?"梦境 02 · 电锯消失术":section==4?"梦境 02 · 双箱替身":"梦境 02 · 魔术舞台",title);
            GUI.Label(new Rect(34,62,Screen.width-68,30),"方向键 / A D：移动　空格：跳跃　E：短暂穿过黄色屏障　Z：回溯　R：重试　T：回工作台",style);
            GUI.Label(new Rect(34,96,Screen.width-68,30), entryLesson ? "黄色仅人偶可穿；蓝色边界与灰色平台保持实体。" : $"素胚 {run.Bases}　脸部 {run.Faces}　泡泡头部 {(run.HasMagicianHead?"已获得":"未获得")}　"+(phase.IsPhasing?"穿越中":"穿越能力就绪"),style);
            string hint=ReachedEnd ? "已到达当前灰盒终点，右侧后续关卡待设计。按 T 返回工作台。" : entryLesson ? "站在黄色顶板上按 E 落入魔术箱，再从右侧黄色屏障离开。" : section==3?"试着踩下压板，观察电锯。留下重量，继续探索。":section==4?"一只箱子维持安全，另一只箱子帮助登高。按 Z 可恢复本段布局。":"探索魔术舞台。箱子无法穿过帷幕，寻找另一侧的通路。";
            GUI.color=new Color(.08f,.10f,.14f,.9f);
            GUI.DrawTexture(new Rect(18,Screen.height-76,Mathf.Min(Screen.width-245,1000),56),Texture2D.whiteTexture);
            GUI.color=Color.white;
            GUI.Label(new Rect(32,Screen.height-68,Mathf.Min(Screen.width-270,970),50),hint,style);
        }
        void OnDestroy(){if(font!=null)Destroy(font);}
    }
}
