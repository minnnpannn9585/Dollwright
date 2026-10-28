using System;
using UnityEngine;
using UnityEngine.UI;

namespace Dollwright.Graybox
{
    public sealed class BodyWorkshopUI : MonoBehaviour
    {
        public BodyWorkshopSession Session {get;private set;}
        public BodyWorkshopGraphic Surface {get;private set;}
        public Button finishButton,resetButton,cancelButton;
        public bool IsOpen=>gameObject.activeSelf;
        Text title,hint,status;
        Image progress;
        Action<bool> close;
        static readonly Color Ink=new Color(.23f,.28f,.31f);
        public static BodyWorkshopUI Create(Transform parent,Font font)
        {
            var root=Rect(parent,"Body_Workshop",Vector2.zero,Vector2.zero);
            root.anchorMin=Vector2.zero;root.anchorMax=Vector2.one;root.offsetMin=root.offsetMax=Vector2.zero;
            root.gameObject.AddComponent<Image>().color=new Color(.90f,.89f,.85f);
            var ui=root.gameObject.AddComponent<BodyWorkshopUI>();
            ui.title=Label(root,"Title","",new Vector2(0,288),new Vector2(700,46),29,font);
            ui.hint=Label(root,"Controls","",new Vector2(0,240),new Vector2(1000,40),20,font);
            var area=Rect(root,"Workpiece",new Vector2(0,-5),new Vector2(1040,450));
            ui.Surface=area.gameObject.AddComponent<BodyWorkshopGraphic>();ui.Surface.raycastTarget=true;
            var track=Rect(root,"Progress_Track",new Vector2(0,-247),new Vector2(400,7));
            track.gameObject.AddComponent<Image>().color=new Color(.79f,.79f,.76f);
            var fill=Rect(track,"Progress",Vector2.zero,new Vector2(400,7));
            fill.anchorMin=fill.anchorMax=new Vector2(0,.5f);fill.pivot=new Vector2(0,.5f);
            ui.progress=fill.gameObject.AddComponent<Image>();ui.progress.color=new Color(.39f,.64f,.55f);ui.progress.raycastTarget=false;
            ui.status=Label(root,"Status","",new Vector2(0,-217),new Vector2(720,30),18,font);
            ui.cancelButton=Button(root,"Cancel","返回",new Vector2(-250,-299),font);
            ui.resetButton=Button(root,"Reset","重做",new Vector2(0,-299),font);
            ui.finishButton=Button(root,"Finish","完成素胚",new Vector2(250,-299),font);
            ui.cancelButton.onClick.AddListener(ui.Cancel);ui.resetButton.onClick.AddListener(ui.ResetWorkpiece);ui.finishButton.onClick.AddListener(()=>ui.Finish());
            root.gameObject.SetActive(false);return ui;
        }
        public void Open(DollBody body,Action<bool> onClose)
        {
            Session=new BodyWorkshopSession(body);Surface.session=Session;close=onClose;
            title.text=body==DollBody.Cotton?"缝制布偶素胚":body==DollBody.Wood?"削制木偶素胚":"塑造陶偶素胚";
            hint.text=body==DollBody.Cotton?"← → 转动布料    ·    按住空格缝纫，松开暂停":body==DollBody.Wood?
                "按住鼠标左键沿木料边缘拖动削切    ·    浅色线为图样":"按住空格转动转盘    ·    左键拖动泥胚边缘，向内收、向外撑";
            transform.SetAsLastSibling();gameObject.SetActive(true);
            if(UnityEngine.EventSystems.EventSystem.current)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
            RefreshView();
        }
        public void Cancel(){if(!IsOpen)return;End(false);}
        public bool Finish(){if(!IsOpen||Session==null||!Session.Finished)return false;End(true);return true;}
        void End(bool done){gameObject.SetActive(false);var callback=close;close=null;callback?.Invoke(done);}
        public void ResetWorkpiece(){if(Session==null)return;Session.Reset();RefreshView();}
        void Update()
        {
            if(Session==null)return;
            float turn=(Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.LeftArrow)?1:0);
            if(Session.Body==DollBody.Cotton)Session.Turn(turn*95*Time.unscaledDeltaTime);
            Session.Tick(Time.unscaledDeltaTime,Input.GetKey(KeyCode.Space));RefreshView();
        }
        public void RefreshView()
        {
            finishButton.interactable=Session.Finished;
            progress.rectTransform.sizeDelta=new Vector2(400*Session.Progress,7);
            status.text=Session.Finished?"素胚已成形，可以继续选择脸部和头饰":Session.OffLine?"偏离缝线了，先转动布料再继续":
                Session.Body==DollBody.Wood && Overcut()?"这处削得太深了，可以重做":"";
            Surface.SetVerticesDirty();
        }
        bool Overcut(){for(int i=0;i<BodyWorkshopSession.Rows;i++)if(Mathf.Min(Session.Left[i],Session.Right[i])<BodyWorkshopSession.Target(i)-.085f)return true;return false;}
        static RectTransform Rect(Transform parent,string name,Vector2 position,Vector2 size)
        {var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;}
        static Text Label(Transform p,string name,string text,Vector2 pos,Vector2 size,int fontSize,Font font)
        {var t=Rect(p,name,pos,size).gameObject.AddComponent<Text>();t.text=text;t.font=font;t.fontSize=fontSize;t.color=Ink;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;return t;}
        static Button Button(Transform p,string name,string text,Vector2 pos,Font font)
        {var r=Rect(p,name,pos,new Vector2(185,48));var image=r.gameObject.AddComponent<Image>();image.color=new Color(.68f,.76f,.74f);var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.navigation=new Navigation{mode=Navigation.Mode.None};var colors=b.colors;colors.disabledColor=new Color(.7f,.7f,.7f,.4f);b.colors=colors;Label(r,"Label",text,Vector2.zero,r.sizeDelta,21,font);return b;}
    }
}
