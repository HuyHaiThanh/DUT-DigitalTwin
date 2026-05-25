using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DUT.Data;

namespace DUT.Data
{
    public class MockDataProvider : MonoBehaviour
    {
        public BuildingDataStore store;
        public float updateInterval = 60f;
        static readonly System.Random _rng = new System.Random(42);

        void Start()
        {
            if (store == null) store = FindFirstObjectByType<BuildingDataStore>();
            GenerateAllData();
            StartCoroutine(Co_Flicker());
            // Refresh màu sau khi data đã được sinh
            StartCoroutine(Co_RefreshColors());
        }

        void GenerateAllData()
        {
            if (store?.AllBuildings == null) return;
            foreach (var info in store.AllBuildings)
                store.UpdateLiveData(BuildLiveData(info));
        }

        BuildingLiveData BuildLiveData(BuildingInfo info)
        {
            var live = new BuildingLiveData { building_id = info.building_id, timestamp = DUT.UI.DUTTime.Now.ToString("o") };
            live.schedule = GenSchedule(info);
            live.occupancy = GenOccupancy(info, live.schedule);
            live.infrastructure = GenInfra(live.occupancy);
            live.equipment = GenEquipment(info);
            live.events = GenEvents(info);
            live.maintenance = GenMaintenance(info);
            return live;
        }

        ScheduleData GenSchedule(BuildingInfo info)
        {
            var d = new ScheduleData();
            int hour = DUT.UI.DUTTime.Now.Hour;
            bool academic = info.chuc_nang == BuildingFunction.GiangDuong || info.chuc_nang == BuildingFunction.ThiNghiem;
            float chance = academic ? 0.65f : 0.25f;
            if ((hour >= 7 && hour <= 11) || (hour >= 13 && hour <= 17)) chance += 0.15f;
            float r = (float)_rng.NextDouble();
            if (r < chance)      { d.status = BuildingStatus.Occupied; d.current_class = GenClass(info, hour); d.occupancy_rate = 0.4f + (float)_rng.NextDouble() * 0.5f; }
            else if (r < chance + 0.2f) { d.status = BuildingStatus.Upcoming; d.next_class = GenClass(info, hour + 2); d.occupancy_rate = 0.05f; }
            else                 { d.status = BuildingStatus.Empty; }
            int[] slots = { 7, 9, 10, 13, 15 };
            foreach (var s in slots) if (_rng.NextDouble() < (academic ? 0.6 : 0.2)) d.today_classes.Add(GenClass(info, s));
            return d;
        }

        ClassInfo GenClass(BuildingInfo info, int h)
        {
            string[] crs = { "Lập trình HĐT","Cấu trúc DL","Giải tích","Vật lý ĐC","Mạng MT","Điện tử CL","TN Điện","Cơ học","Toán RR" };
            string[] cod = { "CO2003","CO2011","MA1001","PH1001","CO3041","EE2001","EE2011","CE3001","CO1007" };
            string[] lec = { "TS. Nguyễn A","PGS. Trần B","TS. Lê C","GS. Phạm D","TS. Hoàng E" };
            int i = _rng.Next(crs.Length), cap = 35 + _rng.Next(3) * 5;
            return new ClassInfo { class_name=crs[i], class_code=cod[i], group=$"Nhóm {_rng.Next(1,6):D2}",
                lecturer=lec[_rng.Next(lec.Length)], time_start=$"{h:D2}:30", time_end=$"{h+2:D2}:00",
                student_count=20+_rng.Next(cap-20), student_capacity=cap,
                room_id=$"{info.ten_khu}{100+_rng.Next(300)}", progress=(float)_rng.NextDouble() };
        }

        OccupancyData GenOccupancy(BuildingInfo info, ScheduleData sched)
        {
            int cap = info.suc_chua_toi_da > 0 ? info.suc_chua_toi_da : 200;
            int cnt = sched.status == BuildingStatus.Occupied ? (int)(cap*(0.4f+(float)_rng.NextDouble()*0.4f)) : (int)(cap*(float)_rng.NextDouble()*0.1f);
            float ratio = (float)cnt/cap;
            var lvl = ratio<0.1f?OccupancyLevel.Empty:ratio<0.3f?OccupancyLevel.Low:ratio<0.6f?OccupancyLevel.Medium:ratio<0.9f?OccupancyLevel.High:OccupancyLevel.Overcrowded;
            var d = new OccupancyData { current_count=cnt, max_capacity=cap, density_ratio=ratio, level=lvl, trend=_rng.NextDouble()>0.5?"+12% hôm qua":"-8% hôm qua" };
            for (int h2=7;h2<=21;h2++) d.hourly_today.Add(new HourlyCount{hour=h2,count=(int)(cap*(float)_rng.NextDouble()*0.8f)});
            return d;
        }

        InfrastructureData GenInfra(OccupancyData occ)
        {
            float kw=5f+occ.current_count*0.15f, avg=kw*0.9f;
            bool abn=_rng.NextDouble()<0.12f; if(abn) kw*=1.35f;
            int act=4+_rng.Next(10), err=_rng.NextDouble()<0.1?_rng.Next(1,3):0;
            return new InfrastructureData {
                electric=new ElectricData{current_kw=kw,avg_kw=avg,daily_kwh=kw*8f,is_abnormal=abn,alert_msg=abn?$"Điện cao ({kw:F0} kW)":""},
                water=new WaterData{current_lph=50f+(float)_rng.NextDouble()*200f,is_abnormal=_rng.NextDouble()<0.05f},
                climate=new ClimateData{temperature_c=26f+(float)_rng.NextDouble()*6f,humidity_pct=60f+(float)_rng.NextDouble()*20f,ac_total=act,ac_active=act-err-_rng.Next(2),ac_error=err,is_abnormal=err>0},
                has_alert=abn||err>0};
        }

        EquipmentData GenEquipment(BuildingInfo info)
        {
            bool lab=info.chuc_nang==BuildingFunction.ThiNghiem;
            int tot=lab?20+_rng.Next(30):5+_rng.Next(15), errc=_rng.NextDouble()<(lab?0.25:0.1)?_rng.Next(1,4):0;
            bool crit=errc>0&&_rng.NextDouble()<0.4;
            var d=new EquipmentData{total_devices=tot,active_devices=tot-errc,error_devices=errc,has_critical_error=crit};
            string[] dv={"Máy chiếu","Máy tính","TB lab","Điều hòa"}, et={"Không kết nối","Quá nhiệt","Hỏng nguồn"};
            for(int i=0;i<errc;i++) d.errors.Add(new DeviceError{device_name=dv[_rng.Next(dv.Length)],location=$"{info.ten_ngan} P.{_rng.Next(100,500)}",error_type=et[_rng.Next(et.Length)],reported_at=$"{7+_rng.Next(3):D2}:{_rng.Next(60):D2}",severity=crit?AlertSeverity.Critical:AlertSeverity.Warning});
            return d;
        }

        List<EventData> GenEvents(BuildingInfo info)
        {
            var list=new List<EventData>();
            if(_rng.NextDouble()<0.15){string[] nm={"Bảo vệ LV K20","Hội thảo KH","Thi lập trình","Seminar IoT"};list.Add(new EventData{event_name=nm[_rng.Next(nm.Length)],event_type="conference",date=DUT.UI.DUTTime.Now.ToString("dd/MM/yyyy"),time_start="14:00",time_end="17:00",location=info.ten_ngan,attendee_count=20+_rng.Next(80),is_today=true});}
            return list;
        }

        MaintenanceData GenMaintenance(BuildingInfo info)
        {
            bool hw=_rng.NextDouble()<0.08f;
            var d=new MaintenanceData{has_active_work=hw,overall_status=hw?MaintenanceStatus.InProgress:MaintenanceStatus.Normal};
            if(hw){string[] wk={"Sơn hành lang","Sửa thang máy","Thay đèn LED","Bảo trì PCCC"};d.active_tickets.Add(new MaintenanceTicket{title=wk[_rng.Next(wk.Length)],started_at="10/05/2025",expected_done="25/05/2025",status=MaintenanceStatus.InProgress,severity=AlertSeverity.Info,affected_area=$"Tầng {_rng.Next(1,info.so_tang+1)}" });}
            return d;
        }

        System.Collections.IEnumerator Co_RefreshColors()
        {
            yield return null;
            yield return null; // chờ 2 frames cho Colorizer.Start() chạy xong
            var cols = FindObjectsByType<DUT.Core.BuildingColorizer>(FindObjectsSortMode.None);
            foreach (var c in cols) c.Refresh();
            Debug.Log($"[MockData] Refreshed {cols.Length} colorizers");
        }

        IEnumerator Co_Flicker()
        {
            float t = 0;
            while (true)
            {
                t += Time.deltaTime;
                if (t < updateInterval) { yield return null; continue; }
                t = 0;
                if (store?.AllBuildings == null) yield return null;
                else foreach (var info in store.AllBuildings)
                {
                    if (_rng.NextDouble() < 0.2f)
                    {
                        var newData = BuildLiveData(info);
                        // Preserve schedule written by RealDataProvider
                        var existing = store.GetLiveData(info.building_id);
                        if (existing?.schedule != null)
                            newData.schedule = existing.schedule;
                        store.UpdateLiveData(newData);
                    }
                }
            }
        }
    }
}