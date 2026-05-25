using UnityEngine;
using DUT.Data;
using DUT.Core;

public class SceneBootstrapper : MonoBehaviour
{
    public BuildingDataStore store;
    public Camera mainCamera;

    void Awake()
    {
        DisableSketchUpCameras();
        if (store == null) store = Resources.Load<BuildingDataStore>("BuildingDataStore");
        if (store == null) { Debug.LogError("[Boot] BuildingDataStore not found!"); return; }
        if (mainCamera == null) mainCamera = Camera.main;
        SetupCamera();
        SetupBuildings();
        SetupDataProvider();
    }

    void DisableSketchUpCameras()
    {
        var allCams = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in allCams)
        {
            if (c.name.Contains("skp_") || c.name.Contains("SketchUp"))
            { c.enabled = false; c.gameObject.SetActive(false); }
            else if (c.name == "Main Camera")
            { c.depth = 0; c.tag = "MainCamera"; }
        }
    }

    void SetupCamera()
    {
        if (mainCamera == null) return;
        if (mainCamera.GetComponent<DUT.Core.CameraController>() == null)
        {
            var ctrl = mainCamera.gameObject.AddComponent<DUT.Core.CameraController>();
            var pivot = GameObject.Find("CameraPivot");
            if (pivot != null) ctrl.pivot = pivot.transform;
        }
    }

    void SetupBuildings()
    {
        store.AllBuildings.Clear();
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            if (!root.name.StartsWith("[Khu")) continue;
            string khu = root.name.Replace("[Khu ", "").Replace("]", "");
            foreach (Transform child in root.transform)
            {
                var rends = child.GetComponentsInChildren<Renderer>();
                if (rends.Length == 0) continue;
                var bounds = rends[0].bounds;
                foreach (var r in rends) bounds.Encapsulate(r.bounds);

                string id = $"B_{khu}_{store.AllBuildings.Count}";
                var parts = child.name.Split(',');
                string tenNgan = parts[0].Trim();
                if (tenNgan.Contains(" - "))
                    tenNgan = tenNgan.Split(new[]{" - "},System.StringSplitOptions.None)[0].Trim();

                int soTang = 1;
                foreach (var p in parts) {
                    var m = System.Text.RegularExpressions.Regex.Match(p.Trim(), @"(\d+)\s*t");
                    if (m.Success) { soTang = int.Parse(m.Groups[1].Value); break; }
                }
                string cf = parts.Length > 2 ? parts[2].Trim().ToLower() : "";
                var func = cf.Contains("giảng đường") ? BuildingFunction.GiangDuong
                         : cf.Contains("thí nghiệm")  ? BuildingFunction.ThiNghiem
                         : cf.Contains("hành chính")  ? BuildingFunction.HanhChinh
                         : cf.Contains("hội trường")  ? BuildingFunction.HoiTruong
                         : cf.Contains("thể chất")    ? BuildingFunction.GDTC
                         : cf.Contains("ký túc")      ? BuildingFunction.KyTucXa
                                                         : BuildingFunction.TienIch;

                store.AllBuildings.Add(new BuildingInfo {
                    building_id=id, ten_khu=khu, ten_day_du=child.name, ten_ngan=tenNgan,
                    so_tang=soTang, chuc_nang_str=cf, chuc_nang=func,
                    world_position=bounds.center, bounds_size=bounds.size,
                    so_phong=soTang*8,
                    suc_chua_toi_da=Mathf.Clamp((int)(bounds.size.x*bounds.size.z*0.5f),50,1000)
                });

                // BuildingObject
                var bo = child.GetComponent<BuildingObject>() ?? child.gameObject.AddComponent<BuildingObject>();
                bo.buildingId = id; bo.khu = khu; bo.store = store;

                // ColliderProxy: child GO ở world space, BoxCollider size=1
                SetupColliderProxy(child.gameObject, bounds);
            }
        }
        Debug.Log($"[Boot] {store.AllBuildings.Count} buildings setup");
    }

    void SetupColliderProxy(GameObject parent, Bounds wb)
    {
        // Xóa collider trên parent
        foreach (var c in parent.GetComponents<Collider>()) DestroyImmediate(c);

        // Tìm/tạo proxy child
        const string proxyName = "_ColliderProxy";
        var proxyT = parent.transform.Find(proxyName);
        if (proxyT == null)
        {
            proxyT = new GameObject(proxyName).transform;
            proxyT.SetParent(parent.transform, false);
        }
        // Đặt proxy theo world bounds (detach → set → re-attach)
        proxyT.SetParent(null, false);
        proxyT.position   = wb.center;
        proxyT.rotation   = Quaternion.identity;
        proxyT.localScale = wb.size;
        proxyT.SetParent(parent.transform, true);

        foreach (var c in proxyT.GetComponents<Collider>()) DestroyImmediate(c);
        var bc = proxyT.gameObject.AddComponent<BoxCollider>();
        bc.size = Vector3.one; bc.center = Vector3.zero;

        // Gắn BuildingObject lên proxy để Raycast tìm được
        var parentBO = parent.GetComponent<BuildingObject>();
        if (parentBO != null)
        {
            var proxyBO = proxyT.GetComponent<BuildingObject>() ?? proxyT.gameObject.AddComponent<BuildingObject>();
            proxyBO.buildingId = parentBO.buildingId;
            proxyBO.khu        = parentBO.khu;
            proxyBO.store      = store;
        }
    }

void SetupDataProvider()
    {
        // MockDataProvider: sinh mock data cho 5 layers (Occupancy, Infra, Equipment, Events, Maintenance)
        var mock = GetComponent<DUT.Data.MockDataProvider>() ?? gameObject.AddComponent<DUT.Data.MockDataProvider>();
        mock.store = store;

        // RealDataProvider
        var dp = GetComponent<DUT.Data.RealDataProvider>() ?? gameObject.AddComponent<DUT.Data.RealDataProvider>();
        dp.store = store;

        // LayerManager
        var lm = GetComponent<DUT.Core.LayerManager>() ?? gameObject.AddComponent<DUT.Core.LayerManager>();
        lm.store = store;

        // AlertManager
        var am = GetComponent<DUT.UI.AlertManager>() ?? gameObject.AddComponent<DUT.UI.AlertManager>();
        am.store = store;

        // ScreenManager — wire sau 1 frame để UIDocuments Start() xong
        var sm = GetComponent<DUT.UI.ScreenManager>() ?? gameObject.AddComponent<DUT.UI.ScreenManager>();
        sm.store = store;
        StartCoroutine(Co_WireScreenManager(sm));
    }

    System.Collections.IEnumerator Co_WireScreenManager(DUT.UI.ScreenManager sm)
    {
        yield return null; // đợi UIDocuments Awake/Start
        var allDocs = UnityEngine.Object.FindObjectsByType<UnityEngine.UIElements.UIDocument>(
            UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None);
        foreach (var d in allDocs)
        {
            if (d.gameObject.name == "DUT_UI")        sm.docMain      = d;
            if (d.gameObject.name == "DUT_Dashboard") sm.docDashboard = d;
            if (d.gameObject.name == "DUT_Schedule")  sm.docSchedule  = d;
        }
        Debug.Log($"[Boot] SM wired: main={sm.docMain?.gameObject.name} dash={sm.docDashboard?.gameObject.name}");
    }
}