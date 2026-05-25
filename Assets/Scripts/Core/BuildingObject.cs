using UnityEngine;
using DUT.Data;

namespace DUT.Core
{
    /// <summary>
    /// Attach trên mỗi tòa nhà. Xử lý click/hover.
    /// Màu sắc được quản lý bởi BuildingColorizer.
    /// </summary>
    public class BuildingObject : MonoBehaviour
    {
        [Header("Identity")]
        public string buildingId;
        public string khu;

        [HideInInspector]
        public BuildingDataStore store;

        // Giữ lại để tương thích — không dùng nữa
        [HideInInspector] public GameObject statusDot;

        void Start()
        {
            if (store == null)
                store = FindFirstObjectByType<BuildingDataStore>();
            EnsureCollider();
        }

        // ── Collider ────────────────────────────────────────────────────
        void EnsureCollider()
        {
            if (GetComponent<Collider>() != null) return;
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            var bc = gameObject.AddComponent<BoxCollider>();
            bc.center = transform.InverseTransformPoint(bounds.center);
            var ws = transform.lossyScale;
            if (Mathf.Abs(ws.x) > 0.0001f)
                bc.size = new Vector3(
                    bounds.size.x / ws.x,
                    bounds.size.y / ws.y,
                    bounds.size.z / ws.z);
        }

        public bool IsSelected => store?.SelectedBuildingId == buildingId;
    }
}