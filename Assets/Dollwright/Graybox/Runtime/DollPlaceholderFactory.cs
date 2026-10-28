using UnityEngine;

namespace Dollwright.Graybox
{
    // Replace this factory with authored prefabs later; recipe/state/UI do not depend on its meshes.
    public static class DollPlaceholderFactory
    {
        public static GameObject Create(Transform parent, DollRecipe recipe, Material[] palette, bool clickable = false)
        {
            var root = new GameObject("Doll_" + recipe.BodyId + "_" + recipe.FaceId + "_" + recipe.head);
            root.transform.SetParent(parent, false);
            Material body = palette[(int)recipe.body], ink = palette[3], light = palette[4];
            Part(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0,.76f,0), new Vector3(.50f,.42f,.36f), body);
            Part(root.transform, "Torso", PrimitiveType.Sphere, new Vector3(0,.40f,0), new Vector3(.30f,.40f,.24f), body);
            foreach (float side in new[] { -1f, 1f })
            {
                var arm = Part(root.transform, side < 0 ? "Arm_L" : "Arm_R", PrimitiveType.Capsule,
                    new Vector3(side*.205f,.40f,0), new Vector3(.095f,.135f,.115f), body);
                arm.localRotation = Quaternion.Euler(0,0,side*12);
                Part(root.transform, side < 0 ? "Leg_L" : "Leg_R", PrimitiveType.Sphere,
                    new Vector3(side*.09f,.115f,0), new Vector3(.13f,.21f,.15f), body);
                if (recipe.body == DollBody.Wood)
                    Part(root.transform, "Wood_Peg", PrimitiveType.Sphere, new Vector3(side*.16f,.48f,0), Vector3.one*.055f, ink);
                var eye = Part(root.transform, "Eye", recipe.HasButtons ? PrimitiveType.Cylinder : PrimitiveType.Sphere,
                    new Vector3(side*.095f,.78f,.173f), recipe.HasButtons ? new Vector3(.075f,.010f,.075f) : new Vector3(.035f,.046f,.015f), ink);
                if (recipe.HasButtons)
                {
                    eye.localRotation = Quaternion.Euler(90,0,0);
                    for (int j=0;j<2;j++)
                    {
                        var thread = Part(root.transform, "Button_Thread", PrimitiveType.Cube,
                            new Vector3(side*.095f,.78f,.188f), new Vector3(.045f,.007f,.007f), light);
                        thread.localRotation = Quaternion.Euler(0,0,j == 0 ? 45 : -45);
                    }
                }
                if (recipe.IsSad)
                    Line(root.transform, "Sad_Brow", new Vector3(side*.14f,.84f,.17f),
                        new Vector3(side*.065f,.87f,.17f), .012f, ink);
            }
            for (int i=0;i<4;i++)
            {
                float x1 = -.048f+i*.024f, x2=x1+.024f;
                float y1 = .69f + (recipe.IsSad ? -1 : 1) * .018f * Mathf.Pow(x1/.048f,2);
                float y2 = .69f + (recipe.IsSad ? -1 : 1) * .018f * Mathf.Pow(x2/.048f,2);
                Line(root.transform, "Mouth", new Vector3(x1,y1,.18f), new Vector3(x2,y2,.18f), .012f, ink);
            }
            if (recipe.body == DollBody.Cotton)
            {
                for (int i=0;i<5;i++)
                {
                    var stitch = Part(root.transform, "Head_Stitch", PrimitiveType.Cube,
                        new Vector3(-.20f,.65f+i*.043f,.114f), new Vector3(.040f,.009f,.012f), palette[5]);
                    stitch.localRotation = Quaternion.Euler(0,0,12);
                    var seam = Part(root.transform, "Body_Stitch", PrimitiveType.Cube,
                        new Vector3(-.08f+i*.037f,.48f-i*.040f,.111f), new Vector3(.031f,.009f,.012f), palette[5]);
                    seam.localRotation = Quaternion.Euler(0,0,42);
                }
            }
            if (recipe.head == DollHead.Magician)
            {
                var hat = new GameObject("H1_Magician").transform; hat.SetParent(root.transform, false);
                hat.localPosition = new Vector3(0,.935f,0); hat.localRotation = Quaternion.Euler(0,0,-12);
                Part(hat, "Hat_Brim", PrimitiveType.Cylinder, Vector3.zero, new Vector3(.69f,.018f,.48f), ink);
                Part(hat, "Hat_Crown", PrimitiveType.Cylinder, new Vector3(0,.13f,0), new Vector3(.43f,.14f,.36f), ink);
                for (int i=0;i<5;i++)
                {
                    var lockPart = Part(root.transform, "Hair_Chunk", PrimitiveType.Cube,
                        new Vector3(-.2f+i*.10f,.92f,.14f), new Vector3(.11f,.09f,.10f), palette[5]);
                    lockPart.localRotation = Quaternion.Euler(0,0,-25+i*12);
                }
                var star = Part(hat, "Star_Placeholder", PrimitiveType.Cube, new Vector3(-.10f,.10f,.20f), new Vector3(.09f,.09f,.025f), light);
                star.localRotation = Quaternion.Euler(0,0,45);
                for (int i=0;i<2;i++)
                {
                    var card = Part(hat, "Card_" + i, PrimitiveType.Cube, new Vector3(.06f+i*.065f,.10f,.19f+i*.01f), new Vector3(.09f,.13f,.015f), light);
                    card.localRotation = Quaternion.Euler(0,0,-8-i*16);
                    var feather = Part(hat, "Feather_" + i, PrimitiveType.Capsule, new Vector3(.19f+i*.045f,.20f,0), new Vector3(.045f,.12f,.025f), palette[5]);
                    feather.localRotation = Quaternion.Euler(0,0,-30-i*18);
                }
            }
            else if (recipe.head == DollHead.Bubble)
            {
                var kit = new GameObject("H2_Bubble").transform; kit.SetParent(root.transform,false);
                Part(kit,"Shell",PrimitiveType.Sphere,new Vector3(0,.76f,0),new Vector3(.63f,.58f,.53f),palette[6]);
                Part(kit,"Side_Bubble",PrimitiveType.Sphere,new Vector3(-.39f,.90f,0),Vector3.one*.12f,palette[6]);
                Part(kit,"Crown_Base",PrimitiveType.Cylinder,new Vector3(0,1.07f,0),new Vector3(.19f,.015f,.16f),palette[5]);
                for(int i=0;i<3;i++)
                    Part(kit,"Crown_Point",PrimitiveType.Cube,new Vector3(-.065f+i*.065f,1.115f,0),new Vector3(.035f,.085f,.10f),palette[5]);
            }
            if (clickable)
            {
                var collider = root.AddComponent<BoxCollider>();
                collider.center = new Vector3(0,.61f,0); collider.size = new Vector3(.68f,1.25f,.55f);
            }
            return root;
        }
        static Transform Part(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(type); part.name=name; part.layer=2;
            part.transform.SetParent(parent,false); part.transform.localPosition=position; part.transform.localScale=scale;
            part.GetComponent<Renderer>().sharedMaterial=material;
            var collider=part.GetComponent<Collider>();
            if(Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider);
            return part.transform;
        }
        static void Line(Transform parent,string name,Vector3 a,Vector3 b,float width,Material material)
        {
            var part=Part(parent,name,PrimitiveType.Cube,(a+b)*.5f,new Vector3(Vector3.Distance(a,b),width,width),material);
            part.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
        }
    }
}

