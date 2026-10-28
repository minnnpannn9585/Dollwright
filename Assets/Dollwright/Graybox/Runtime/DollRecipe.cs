using System;
using UnityEngine;

namespace Dollwright.Graybox
{
    public enum DollBody { Cotton, Wood, Ceramic }
    public enum DollFace { HappyPaint, HappyButton, SadPaint, SadButton }
    public enum DollHead { None, Magician, Bubble }

    [Serializable]
    public struct DollRecipe
    {
        public DollBody body;
        public DollFace face;
        public DollHead head;
        public DollRecipe(DollBody body, DollFace face, DollHead head)
        { this.body = body; this.face = face; this.head = head; }
        public bool IsValid => Enum.IsDefined(typeof(DollBody), body)
            && Enum.IsDefined(typeof(DollFace), face) && Enum.IsDefined(typeof(DollHead), head);
        public bool IsSad => face == DollFace.SadPaint || face == DollFace.SadButton;
        public bool HasButtons => face == DollFace.HappyButton || face == DollFace.SadButton;
        public string BodyId => "R" + ((int)body + 1);
        public string FaceId => "F0" + ((int)face + 1);
        public string HeadId => head == DollHead.None ? null : "H" + (int)head;
        public string DisplayName => (IsSad ? "悲伤的" : "快乐的")
            + (head == DollHead.Magician ? "魔术师" : head == DollHead.Bubble ? "泡泡" : "")
            + (body == DollBody.Cotton ? "布偶" : body == DollBody.Wood ? "木偶" : "瓷偶");
        public string DreamPath => head == DollHead.Magician
            ? "Assets/Dollwright/Graybox/Scenes/Dream02_MagicBox.unity"
            : head == DollHead.None ? DreamTravel.DreamScene : "";
        public string Ability => head == DollHead.Magician ? "穿越屏障" : head == DollHead.Bubble ? "漂浮" : "";
        public string[] Ingredients => HeadId == null
            ? new[] { BodyId, FaceId } : new[] { BodyId, FaceId, HeadId };
    }

    [Serializable]
    public sealed class CraftedDoll
    {
        public string instanceId;
        public DollRecipe recipe;
        public string DisplayName => recipe.DisplayName;
        public DollDefinition CreateDefinition()
        {
            var definition = ScriptableObject.CreateInstance<DollDefinition>();
            definition.instanceId = instanceId;
            definition.recipe = recipe;
            definition.dollId = recipe.head == DollHead.Magician ? "02" : recipe.head == DollHead.Bubble ? "03" : "01";
            definition.displayName = DisplayName;
            definition.dreamScenePath = recipe.DreamPath;
            definition.dreamName = recipe.head == DollHead.Magician ? "魔术舞台" : recipe.head == DollHead.Bubble ? "" : "快乐游乐园";
            definition.abilities = "行走 · 跳跃 · 推箱" + (string.IsNullOrEmpty(recipe.Ability) ? "" : " · " + recipe.Ability);
            return definition;
        }
    }

    public static class DollMaterialCatalog
    {
        public static readonly string[] Ids = { "R1", "R2", "R3", "F01", "F02", "F03", "F04", "H1", "H2" };
        public static bool Contains(string id) => Array.IndexOf(Ids, id) >= 0;
        // Pickup ids identify a place in a level; they are separate from recipe material ids.
        public static string FromPickup(string id, GrayboxPickup.Kind kind, string scenePath)
        {
            if (kind == GrayboxPickup.Kind.Head) return id == "H2" ? "H2" : "H1";
            if (kind == GrayboxPickup.Kind.Base)
                return id == "R2" ? "R2" : id == "R3" ? "R3" : "R1";
            if (id == "F03" || id == "F04" || id == "F02" || id == "F01") return id;
            if (scenePath.Contains("Dream02")) return id == "F2" ? "F04" : "F02";
            return id == "F2" ? "F03" : "F01";
        }
    }
}

