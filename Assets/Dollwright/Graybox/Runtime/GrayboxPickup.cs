using UnityEngine;

namespace Dollwright.Graybox
{
    public sealed class GrayboxPickup : MonoBehaviour
    {
        public enum Kind { Base, Face, Head }
        public string materialId;
        public string craftingMaterialId;
        public Kind kind;
        public Transform visual;
        Vector3 origin;
        void Awake() { if (visual != null) origin = visual.localPosition; }
        void Start()
        {
            var run=FindObjectOfType<GrayboxRun>();
            if(run!=null&&System.Linq.Enumerable.Contains(run.Collected,materialId))gameObject.SetActive(false);
        }
        void Update()
        {
            if (visual != null) visual.localPosition = origin + Vector3.up * (Mathf.Sin(Time.time * 3 + transform.position.x) * 0.09f);
        }
        void OnTriggerEnter2D(Collider2D other)
        {
            var player = other.GetComponent<GrayboxPlayer2D>();
            if (player != null && player.run.Collect(materialId, kind, craftingMaterialId)) gameObject.SetActive(false);
        }
    }
}
