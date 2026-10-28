using UnityEngine;

namespace Dollwright.Graybox
{
    [CreateAssetMenu(menuName = "Dollwright/Doll Definition")]
    public sealed class DollDefinition : ScriptableObject
    {
        public string instanceId;
        public DollRecipe recipe;
        public string dollId = "01";
        public string displayName = "人偶 01 · 纯白";
        public string dreamName = "梦境 01 · 快乐游乐园";
        public string abilities = "行走 · 跳跃 · 推箱";
        public string dreamScenePath = DreamTravel.DreamScene;
    }
}
