using UnityEngine;
namespace Dollwright.Graybox
{
    public sealed class ShelfDollHandle : MonoBehaviour
    {
        public string instanceId;
        public DollDefinition definition;
        void OnDestroy() { if (definition != null) Destroy(definition); }
    }
}
