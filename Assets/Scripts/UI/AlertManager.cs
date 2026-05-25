using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using DUT.Data;

namespace DUT.UI
{
    public class AlertManager : MonoBehaviour
    {
        public static AlertManager Instance { get; private set; }
        public BuildingDataStore store;
        public float scanInterval  = 30f;
        public float toastDuration = 4f;

        List<AlertData> _alerts = new List<AlertData>();
        public IReadOnlyList<AlertData> Alerts => _alerts;
        public event System.Action OnAlertsChanged;

        VisualElement _root;
        Label         _bellBadge;
        VisualElement _dropdown;
        VisualElement _dropdownList;
        VisualElement _toast;
        Label         _toastMsg;
        bool          _dropdownOpen;
        bool          _toastActive;

        static readonly Color C_CRITICAL = new Color(0.94f, 0.27f, 0.27f);
        static readonly Color C_WARNING  = new Color(0.96f, 0.62f, 0.04f);
        static readonly Color C_INFO     = new Color(0.18f, 0.56f, 0.91f);
        static readonly Color C_DIM      = new Color(0.24f, 0.35f, 0.44f);
        static readonly Color C_MUTED    = new Color(0.48f, 0.60f, 0.72f);
        static readonly Color C_TEXT     = new Color(0.91f, 0.94f, 0.97f);
        static readonly Color C_HOVER    = new Color(0.08f, 0.15f, 0.24f);
        static readonly Color C_SEP      = new Color(0.10f, 0.19f, 0.31f);

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            StartCoroutine(Co_WireUI());
            StartCoroutine(Co_Scan());
        }

        IEnumerator Co_WireUI()
        {
            yield return null; // let UIManager.Start() complete
            if (UIManager.Instance == null) { Debug.LogWarning("[Alert] UIManager not found"); yield break; }
            var doc = UIManager.Instance.GetComponent<UIDocument>();
            if (doc == null) yield break;
            _root = doc.rootVisualElement;
            WireAlertUI();
        }

void WireAlertUI()
{
    if (_root == null) return;

    _bellBadge    = _root.Q<Label>("alert-badge");
    _dropdown     = _root.Q<VisualElement>("alert-dropdown");
    _dropdownList = _root.Q<VisualElement>("alert-dropdown-list");
    _toast        = _root.Q<VisualElement>("alert-toast");
    _toastMsg     = _root.Q<Label>("alert-toast-msg");

    _root.Q<Button>("alert-bell")?.RegisterCallback<ClickEvent>(evt => {
        evt.StopPropagation();
        ToggleDropdown();
    });

    // Close dropdown when clicking outside
    _root.RegisterCallback<ClickEvent>(evt => {
        if (!_dropdownOpen) return;
        if (_dropdown != null && _dropdown.Contains(evt.target as VisualElement)) return;
        if (_root.Q<Button>("alert-bell")?.Contains(evt.target as VisualElement) == true) return;
        CloseDropdown();
    }, TrickleDown.TrickleDown);

    HideDropdown();
    HideToast();
    UpdateBellBadge();
}

        // ── Scan ─────────────────────────────────────────────────────────────
        IEnumerator Co_Scan()
        {
            yield return new WaitForSeconds(3f); // initial delay: let MockData + RealData populate
            while (true)
            {
                ScanAlerts();
                yield return new WaitForSeconds(scanInterval);
            }
        }

        void ScanAlerts()
        {
            if (store?.AllBuildings == null) return;
            var prevIds  = new HashSet<string>(_alerts.Select(a => a.alert_id));
            var newList  = new List<AlertData>();

            foreach (var info in store.AllBuildings)
            {
                var live = store.GetLiveData(info.building_id);
                if (live == null) continue;

                // Điện bất thường
                if (live.infrastructure.electric.is_abnormal)
                    newList.Add(Make(info, AlertLayer.Infrastructure, AlertSeverity.Warning,
                        $"⚡ Điện bất thường — {info.ten_ngan}",
                        string.IsNullOrEmpty(live.infrastructure.electric.alert_msg)
                            ? $"{live.infrastructure.electric.current_kw:F0} kW"
                            : live.infrastructure.electric.alert_msg));

                // Điều hòa lỗi
                if (live.infrastructure.climate.ac_error > 0)
                    newList.Add(Make(info, AlertLayer.Infrastructure, AlertSeverity.Warning,
                        $"❄ Điều hòa lỗi — {info.ten_ngan}",
                        $"{live.infrastructure.climate.ac_error} máy báo lỗi"));

                // Thiết bị hỏng nghiêm trọng
                if (live.equipment.has_critical_error)
                    newList.Add(Make(info, AlertLayer.Equipment, AlertSeverity.Critical,
                        $"❌ Thiết bị hỏng — {info.ten_ngan}",
                        $"{live.equipment.error_devices} thiết bị lỗi nghiêm trọng"));
                else if (live.equipment.error_devices > 0)
                    newList.Add(Make(info, AlertLayer.Equipment, AlertSeverity.Warning,
                        $"⚠ Thiết bị lỗi — {info.ten_ngan}",
                        $"{live.equipment.error_devices} thiết bị báo lỗi"));

                // Mật độ quá tải
                if (live.occupancy.level == OccupancyLevel.Overcrowded)
                    newList.Add(Make(info, AlertLayer.Occupancy, AlertSeverity.Warning,
                        $"👥 Quá tải — {info.ten_ngan}",
                        $"{live.occupancy.current_count}/{live.occupancy.max_capacity} người"));

                // Bảo trì nguy cấp
                if (live.maintenance.overall_status == MaintenanceStatus.Disrupted ||
                    live.maintenance.overall_status == MaintenanceStatus.Critical)
                    newList.Add(Make(info, AlertLayer.Maintenance, AlertSeverity.Critical,
                        $"🔧 Bảo trì khẩn — {info.ten_ngan}",
                        live.maintenance.active_tickets.Count > 0
                            ? live.maintenance.active_tickets[0].title
                            : "Cần xử lý ngay"));
            }

            // Critical → Warning → Info
            newList.Sort((a, b) => b.severity.CompareTo(a.severity));

            // New critical alerts → toast
            var newCritical = newList.Where(a => a.severity == AlertSeverity.Critical && !prevIds.Contains(a.alert_id)).ToList();

            _alerts = newList;
            OnAlertsChanged?.Invoke();
            UpdateBellBadge();
            if (_dropdownOpen) RebuildDropdown();

            if (newCritical.Count > 0 && !_toastActive)
                StartCoroutine(Co_ShowToast(newCritical[0]));
        }

        AlertData Make(BuildingInfo info, AlertLayer layer, AlertSeverity sev, string title, string desc) =>
            new AlertData {
                alert_id    = $"{info.building_id}_{layer}",
                building_id = info.building_id,
                layer       = layer,
                severity    = sev,
                title       = title,
                description = desc,
                timestamp   = DUTTime.Now.ToString("HH:mm")
            };

        // ── Bell badge ───────────────────────────────────────────────────────
        void UpdateBellBadge()
        {
            if (_bellBadge == null) return;
            int count = _alerts.Count;
            _bellBadge.text = count > 9 ? "9+" : count.ToString();
            _bellBadge.style.display = count > 0
                ? new StyleEnum<DisplayStyle>(DisplayStyle.Flex)
                : new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

        // ── Dropdown ─────────────────────────────────────────────────────────
        void ToggleDropdown() { if (_dropdownOpen) CloseDropdown(); else OpenDropdown(); }

        void OpenDropdown()
        {
            _dropdownOpen = true;
            RebuildDropdown();
            if (_dropdown != null)
                _dropdown.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        }

        void CloseDropdown()
        {
            _dropdownOpen = false;
            HideDropdown();
        }

        void HideDropdown()
        {
            if (_dropdown != null)
                _dropdown.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }

void RebuildDropdown()
{
    if (_dropdownList == null) return;
    _dropdownList.Clear();

    // Update count label
    var countLbl = _root?.Q<Label>("alert-dropdown-count");
    if (countLbl != null)
        countLbl.text = _alerts.Count == 0 ? "Không có cảnh báo" : $"{_alerts.Count} cảnh báo";

    if (_alerts.Count == 0)
    {
        var empty = new Label("Không có cảnh báo nào");
        empty.style.fontSize = 12;
        empty.style.color = new StyleColor(C_DIM);
        empty.style.paddingTop = empty.style.paddingBottom = 16;
        empty.style.unityTextAlign = TextAnchor.MiddleCenter;
        _dropdownList.Add(empty);
        return;
    }

    foreach (var alert in _alerts)
        _dropdownList.Add(BuildAlertRow(alert));
}

        VisualElement BuildAlertRow(AlertData alert)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.paddingLeft = row.style.paddingRight = 14;
            row.style.paddingTop = row.style.paddingBottom = 9;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new StyleColor(C_SEP);
            row.style.alignItems = Align.FlexStart;

            Color sevCol = alert.severity == AlertSeverity.Critical ? C_CRITICAL
                         : alert.severity == AlertSeverity.Warning   ? C_WARNING
                         : C_INFO;

            // Severity stripe
            var stripe = new VisualElement();
            stripe.style.width = 3; stripe.style.alignSelf = Align.Stretch;
            stripe.style.borderTopLeftRadius = stripe.style.borderTopRightRadius =
            stripe.style.borderBottomLeftRadius = stripe.style.borderBottomRightRadius = 2;
            stripe.style.backgroundColor = new StyleColor(sevCol);
            stripe.style.marginRight = 10;
            stripe.style.flexShrink = 0;
            row.Add(stripe);

            // Text block
            var txt = new VisualElement();
            txt.style.flexGrow = 1;
            var titleLbl = new Label(alert.title ?? "–");
            titleLbl.style.fontSize = 12;
            titleLbl.style.color = new StyleColor(C_TEXT);
            titleLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLbl.style.whiteSpace = WhiteSpace.Normal;
            txt.Add(titleLbl);
            if (!string.IsNullOrEmpty(alert.description))
            {
                var descLbl = new Label(alert.description);
                descLbl.style.fontSize = 10;
                descLbl.style.color = new StyleColor(C_MUTED);
                descLbl.style.whiteSpace = WhiteSpace.Normal;
                descLbl.style.marginTop = 2;
                txt.Add(descLbl);
            }
            row.Add(txt);

            // Timestamp
            if (!string.IsNullOrEmpty(alert.timestamp))
            {
                var ts = new Label(alert.timestamp);
                ts.style.fontSize = 10;
                ts.style.color = new StyleColor(C_DIM);
                ts.style.alignSelf = Align.FlexStart;
                ts.style.marginLeft = 8;
                ts.style.marginTop = 1;
                ts.style.flexShrink = 0;
                row.Add(ts);
            }

            // Hover + click
            row.RegisterCallback<MouseEnterEvent>(_ =>
                row.style.backgroundColor = new StyleColor(C_HOVER));
            row.RegisterCallback<MouseLeaveEvent>(_ =>
                row.style.backgroundColor = new StyleColor(Color.clear));

            string bid = alert.building_id;
            row.RegisterCallback<ClickEvent>(evt => {
                evt.StopPropagation();
                CloseDropdown();
                if (bid != null && store != null) store.SelectBuilding(bid);
            });

            return row;
        }

        // ── Toast ────────────────────────────────────────────────────────────
        IEnumerator Co_ShowToast(AlertData alert)
        {
            _toastActive = true;
            if (_toast != null && _toastMsg != null)
            {
                _toastMsg.text = alert.title ?? "Cảnh báo mới";
                _toast.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            }
            yield return new WaitForSeconds(toastDuration);
            HideToast();
            _toastActive = false;
        }

        void HideToast()
        {
            if (_toast != null)
                _toast.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }
    }
}
