using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Dollwright.Graybox
{
    public sealed class WorkbenchCraftingUI : MonoBehaviour
    {
        public GameObject controls, shelfControls;
        public Transform previewAnchor, shelfRoot;
        public Vector3 shelfFirstPosition;
        public float shelfStep = -.81f;
        public Material[] palette;
        public Button[] bodyButtons, faceButtons, headButtons;
        public Text[] bodyCounts, faceCounts, headCounts;
        public Button makeButton, previousPage, nextPage;
        public Text dollName, shelfName;
        public GameObject hoverRing;
        public int Page { get; private set; }
        public bool IsBusy => Time.unscaledTime < busyUntil;
        public GameObject Preview => preview;
        public DollRecipe Draft => inventory.Draft;
        public IReadOnlyList<ShelfDollHandle> VisibleDolls => visible.AsReadOnly();
        readonly List<ShelfDollHandle> visible = new List<ShelfDollHandle>();
        DollCraftingInventory inventory;
        WorkbenchDreamUI flow;
        GameObject preview;
        string previewKey, shelfKey;
        float busyUntil, noticeUntil;
        bool dragging;
        float previousMouseX;
        bool initialized;
        DollBody? preparedBody;
        public bool BodyReady => preparedBody.HasValue && preparedBody.Value==Draft.body;
        public BodyWorkshopUI Workshop { get; private set; }
        public bool IsProcessingBody => Workshop!=null && Workshop.IsOpen;
        Text makeLabel;

        public void Initialize(WorkbenchDreamUI owner)
        {
            if (initialized) return;
            initialized = true; flow = owner; inventory = DollCraftingStore.Current;
            foreach (Transform child in shelfRoot) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            foreach (Transform child in previewAnchor) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            controls.SetActive(false); shelfControls.SetActive(false);
            previewAnchor.gameObject.SetActive(false);
            if (hoverRing != null) hoverRing.SetActive(false);
            for (int i=0;i<bodyButtons.Length;i++) { int option=i; bodyButtons[i].onClick.AddListener(() => SelectBody(option)); }
            for (int i=0;i<faceButtons.Length;i++) { int option=i; faceButtons[i].onClick.AddListener(() => SelectFace(option)); }
            for (int i=0;i<headButtons.Length;i++) { int option=i; headButtons[i].onClick.AddListener(() => SelectHead(option)); }
            makeLabel=makeButton.GetComponentInChildren<Text>();
            Workshop=BodyWorkshopUI.Create(flow.transform,makeLabel.font);
            makeButton.onClick.AddListener(() => { if(BodyReady)Craft();else ConfirmBody(); });
            previousPage.onClick.AddListener(() => ChangePage(-1)); nextPage.onClick.AddListener(() => ChangePage(1));
            inventory.Changed += Refresh;
            var bedLabel=flow.transform.Find("BedButton/BedLabel");
            if(bedLabel!=null)bedLabel.gameObject.SetActive(false);
            Refresh();
        }
        public void SelectBody(int value)
        {
            if(!flow.CraftReady||IsBusy||IsProcessingBody)return;
            var recipe=Draft;recipe.body=(DollBody)value;
            if(recipe.body!=Draft.body && recipe.IsValid){preparedBody=null;SetDraft(recipe);}
        }
        public void SelectFace(int value) { if(!BodyReady)return;var recipe=Draft;recipe.face=(DollFace)value;SetDraft(recipe); }
        public void SelectHead(int value) { if(!BodyReady)return;var recipe=Draft;recipe.head=(DollHead)value;SetDraft(recipe); }
        void SetDraft(DollRecipe recipe)
        {
            if (!flow.CraftReady || IsBusy || IsProcessingBody) return;
            if(!inventory.SetDraft(recipe)) ShowNotice("保存失败");
        }
        public bool ConfirmBody()
        {
            if(!flow.CraftReady||IsBusy||IsProcessingBody||inventory.Count(Draft.BodyId)<1)return false;
            var selected=Draft.body;
            dragging=false;controls.SetActive(false);previewAnchor.gameObject.SetActive(false);
            flow.backButton.SetActive(false);
            Workshop.Open(selected,done=>
            {
                if(done && Draft.body==selected)preparedBody=selected;
                controls.SetActive(flow.IsCrafting);previewAnchor.gameObject.SetActive(flow.IsCrafting);
                flow.backButton.SetActive(flow.IsCrafting);Refresh();
            });
            return true;
        }
        public bool Craft()
        {
            if (!flow.CraftReady || IsBusy || IsProcessingBody || !BodyReady) return false;
            busyUntil = Time.unscaledTime + .65f;
            if(!inventory.TryCraft(Draft,out var created))
            {
                busyUntil=0;
                if(!string.IsNullOrEmpty(inventory.LastError))ShowNotice("保存失败");
                Refresh();return false;
            }
            preparedBody=null;
            Page=(inventory.Dolls.Count-1)/3;
            RefreshShelf();
            Refresh();
            return true;
        }
        public void SetCraftingMode(bool open)
        {
            if(!open && IsProcessingBody)Workshop.Cancel();
            controls.SetActive(open);previewAnchor.gameObject.SetActive(open);
            if (hoverRing != null)hoverRing.SetActive(false);
            dragging=false; Refresh();
        }
        public void SetShelfMode(bool open)
        {
            shelfControls.SetActive(open);shelfName.text="";noticeUntil=0;
            if(open)RefreshShelf();
        }
        public void ChangePage(int delta)
        {
            if(!flow.FocusReady)return;
            Page=Mathf.Clamp(Page+delta,0,Mathf.Max(0,(inventory.Dolls.Count-1)/3));RefreshShelf();
        }
        void Refresh()
        {
            if (!initialized) return;
            for(int i=0;i<3;i++)UpdateOption(bodyButtons[i],bodyCounts[i],i==(int)Draft.body,inventory.Count("R"+(i+1)),"");
            for(int i=0;i<4;i++)UpdateOption(faceButtons[i],faceCounts[i],i==(int)Draft.face,inventory.Count("F0"+(i+1)),"");
            for(int i=0;i<3;i++)UpdateOption(headButtons[i],headCounts[i],i==(int)Draft.head,i==0?0:inventory.Count("H"+i),i==0?"无":"");
            dollName.text=Draft.DisplayName;
            string key=JsonUtility.ToJson(Draft);
            if(previewKey!=key)
            {
                previewKey=key;
                if(preview!=null){preview.SetActive(false);Destroy(preview);}
                preview=DollPlaceholderFactory.Create(previewAnchor,Draft,palette);
                preview.transform.localScale=Vector3.one*1.65f;
            }
            RefreshStageControls();
            RefreshShelf();
        }
        void RefreshStageControls()
        {
            bool ready=flow.CraftReady && !IsBusy && !IsProcessingBody;
            foreach(var button in faceButtons)button.interactable=ready&&BodyReady;
            foreach(var button in headButtons)button.interactable=ready&&BodyReady;
            foreach(var button in bodyButtons)button.interactable=ready;
            makeLabel.text=BodyReady?"制作":"确认素胚";
            makeButton.interactable=ready&&(BodyReady?inventory.CanCraft(Draft):inventory.Count(Draft.BodyId)>0);
        }
        static void UpdateOption(Button button,Text count,bool selected,int amount,string replacement)
        {
            button.GetComponent<Image>().color=selected?new Color(.62f,.72f,.68f,.98f):new Color(.91f,.90f,.86f,.96f);
            count.text=string.IsNullOrEmpty(replacement)?"×"+amount:replacement;
            count.color=amount>0 || replacement=="无"?new Color(.18f,.21f,.24f):new Color(.48f,.49f,.49f);
        }
        public void RefreshShelf()
        {
            Page=Mathf.Clamp(Page,0,Mathf.Max(0,(inventory.Dolls.Count-1)/3));
            flow.bedButton.interactable=inventory.Dolls.Count>0;
            string key=Page+":"+string.Join(",",System.Linq.Enumerable.Select(inventory.Dolls,x=>x.instanceId));
            if(shelfKey!=key)
            {
                shelfKey=key;
                foreach(var doll in visible)if(doll!=null){doll.gameObject.SetActive(false);Destroy(doll.gameObject);}
                visible.Clear();flow.magicianCollider=null;flow.dollCollider=null;flow.initialDoll=null;
                for(int i=0;i<3 && Page*3+i<inventory.Dolls.Count;i++)
                {
                    var record=inventory.Dolls[Page*3+i];
                    var doll=DollPlaceholderFactory.Create(shelfRoot,record.recipe,palette,true);
                    doll.name="Shelf_Doll_"+record.instanceId;
                    doll.transform.position=shelfFirstPosition+new Vector3(shelfStep*i,0,0);
                    var handle=doll.AddComponent<ShelfDollHandle>();handle.instanceId=record.instanceId;handle.definition=record.CreateDefinition();
                    visible.Add(handle);
                    var collider=doll.GetComponent<Collider>();
                    if(flow.dollCollider==null)flow.dollCollider=collider;
                    if(i==0)flow.initialDoll=handle.definition;
                    if(record.recipe.head==DollHead.Magician && flow.magicianCollider==null)flow.magicianCollider=collider;
                }
            }
            int last=Mathf.Max(0,(inventory.Dolls.Count-1)/3);
            previousPage.gameObject.SetActive(last>0);nextPage.gameObject.SetActive(last>0);
            previousPage.interactable=Page>0;nextPage.interactable=Page<last;
        }
        public void ShowUnavailableDream() { ShowNotice("梦境待制作"); }
        void ShowNotice(string value)
        {
            if(flow.IsSelecting)shelfName.text=value;else dollName.text=value;
            noticeUntil=Time.unscaledTime+2;
        }
        void Update()
        {
            if(!initialized)return;
            RefreshStageControls();
            if(preview!=null)
            {
                float pulse=IsBusy?1+Mathf.Sin((busyUntil-Time.unscaledTime)/.65f*Mathf.PI)*.06f:1;
                preview.transform.localScale=Vector3.one*1.65f*pulse;
            }
            bool overUI=EventSystem.current!=null && EventSystem.current.IsPointerOverGameObject();
            if(flow.CraftReady && !IsBusy && !IsProcessingBody)
            {
                if(Input.GetMouseButtonDown(0) && !overUI){dragging=true;previousMouseX=Input.mousePosition.x;}
                if(Input.GetMouseButtonUp(0))dragging=false;
                if(dragging && preview!=null)
                {
                    float x=Input.mousePosition.x;
                    preview.transform.Rotate(0,-(x-previousMouseX)*.45f,0,Space.Self);previousMouseX=x;
                }
            }
            else dragging=false;
            if(noticeUntil>0 && Time.unscaledTime>noticeUntil)
            { noticeUntil=0;dollName.text=Draft.DisplayName;shelfName.text=""; }
            if(flow.FocusReady && noticeUntil==0 && !overUI)
            {
                string name="";
                if(Physics.Raycast(flow.viewCamera.ScreenPointToRay(Input.mousePosition),out var hit,100))
                {
                    var handle=hit.collider.GetComponent<ShelfDollHandle>();
                    if(handle!=null && handle.definition!=null)name=handle.definition.displayName;
                }
                shelfName.text=name;
            }
            if(hoverRing!=null)
            {
                bool hover=flow.HomeReady && !overUI
                    && Physics.Raycast(flow.viewCamera.ScreenPointToRay(Input.mousePosition),out var hit,100)
                    && hit.collider==flow.craftingPlatform;
                hoverRing.SetActive(hover);
            }
        }
        void OnDestroy()
        {
            if(inventory!=null)inventory.Changed-=Refresh;
        }
    }
}

