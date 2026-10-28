using UnityEngine;

namespace Dollwright.Graybox
{
    public sealed class DreamDollAppearance : MonoBehaviour
    {
        public Sprite circle, square;
        public Material spriteMaterial;
        public DollRecipe AppliedRecipe { get; private set; }
        Transform visual;
        public void Apply(DollRecipe recipe)
        {
            if (circle == null || square == null || spriteMaterial == null) return;
            AppliedRecipe = recipe;
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
                if (visual == null || !renderer.transform.IsChildOf(visual)) renderer.enabled = false;
            if (visual != null) { visual.gameObject.SetActive(false); Destroy(visual.gameObject); }
            visual = new GameObject("Crafted_Doll_Visual").transform; visual.SetParent(transform,false);
            Color body = recipe.body == DollBody.Cotton ? new Color(.94f,.93f,.89f)
                : recipe.body == DollBody.Wood ? new Color(.68f,.64f,.58f) : new Color(.83f,.86f,.88f);
            Color ink = new Color(.22f,.24f,.27f);
            Shape("Body",new Vector2(0,-.16f),new Vector2(.45f,.66f),body,10);
            Shape("Head",new Vector2(0,.43f),new Vector2(.67f,.57f),body,11);
            foreach(float side in new[]{-1f,1f})
            {
                Shape("Arm",new Vector2(side*.28f,-.12f),new Vector2(.12f,.40f),body,10);
                Shape("Leg",new Vector2(side*.13f,-.51f),new Vector2(.16f,.27f),body,10);
                Shape("Eye",new Vector2(side*.125f,.45f),Vector2.one*(recipe.HasButtons ? .105f : .060f),ink,12);
                if(recipe.HasButtons)for(int j=0;j<2;j++)
                {
                    var line=Shape("Thread",new Vector2(side*.125f,.45f),new Vector2(.085f,.013f),Color.white,13,false);
                    line.localRotation=Quaternion.Euler(0,0,j==0?45:-45);
                }
                if(recipe.IsSad)
                {
                    var brow=Shape("SadBrow",new Vector2(side*.125f,.55f),new Vector2(.11f,.018f),ink,12,false);
                    brow.localRotation=Quaternion.Euler(0,0,side*-20);
                }
            }
            for(int i=0;i<2;i++)
            {
                var mouth=Shape("Mouth",new Vector2(-.03f+i*.06f,.32f),new Vector2(.065f,.018f),ink,12,false);
                mouth.localRotation=Quaternion.Euler(0,0,(i==0?-20:20)*(recipe.IsSad?-1:1));
            }
            if(recipe.head==DollHead.Magician)
            {
                Shape("HatBrim",new Vector2(0,.74f),new Vector2(.88f,.09f),ink,12,false);
                var hat=Shape("Hat",new Vector2(-.04f,.94f),new Vector2(.46f,.35f),ink,12,false);
                hat.localRotation=Quaternion.Euler(0,0,8);
            }
            else if(recipe.head==DollHead.Bubble)
            {
                Shape("Bubble",new Vector2(0,.43f),new Vector2(.82f,.75f),new Color(.65f,.81f,.9f,.24f),14);
                Shape("Crown",new Vector2(0,.87f),new Vector2(.25f,.12f),ink,15,false);
            }
            var phase=GetComponent<DreamPhaseAbility>(); if(phase!=null)phase.RefreshVisuals();
        }
        Transform Shape(string name,Vector2 pos,Vector2 size,Color color,int order,bool rounded=true)
        {
            var part=new GameObject(name).transform;part.SetParent(visual,false);part.localPosition=pos;part.localScale=new Vector3(size.x,size.y,1);
            var renderer=part.gameObject.AddComponent<SpriteRenderer>();renderer.sprite=rounded?circle:square;
            renderer.sharedMaterial=spriteMaterial;renderer.color=color;renderer.sortingOrder=order;
            return part;
        }
    }
}

