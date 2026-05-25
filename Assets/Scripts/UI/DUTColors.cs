using UnityEngine;

namespace DUT.UI
{
    public static class DUTColors
    {
        public static readonly Color BgApp         = Hex("#0B1520");
        public static readonly Color BgTopbar      = Hex("#08101A");
        public static readonly Color BgSidebar     = Hex("#0D1826");
        public static readonly Color BgCard        = Hex("#101E2E");
        public static readonly Color BgHover       = Hex("#162638");
        public static readonly Color Border        = Hex("#1A3050");
        public static readonly Color BorderLight   = Hex("#1E3A5F");
        public static readonly Color TextPrimary   = Hex("#E8F0F8");
        public static readonly Color TextSecondary = Hex("#7A9AB8");
        public static readonly Color TextMuted     = Hex("#3D5870");
        public static readonly Color Accent        = Hex("#1A6FBF");
        public static readonly Color AccentLight   = Hex("#2E8FE8");
        public static readonly Color AccentDim     = Hex("#102540");
        public static readonly Color StatusEmpty       = Hex("#22C55E");
        public static readonly Color StatusOccupied    = Hex("#EF4444");
        public static readonly Color StatusUpcoming    = Hex("#F59E0B");
        public static readonly Color EmptyBg           = Hex("#052010");
        public static readonly Color EmptyBorder       = Hex("#166534");
        public static readonly Color OccupiedBg        = Hex("#1C0808");
        public static readonly Color OccupiedBorder    = Hex("#7F1D1D");
        public static readonly Color UpcomingBg        = Hex("#1C1000");
        public static readonly Color UpcomingBorder    = Hex("#78350F");
        public static readonly Color FuncGiangDuong   = Hex("#2E8FE8");
        public static readonly Color FuncHanhChinh    = Hex("#A855F7");
        public static readonly Color FuncThiNghiem    = Hex("#F59E0B");
        public static readonly Color FuncTienIch      = Hex("#6B7280");
        public static readonly Color FuncGDTC         = Hex("#22C55E");
        public static readonly Color FuncKTX          = Hex("#EC4899");
        public static readonly Color FuncGiangDuongBg = Hex("#051428");
        public static readonly Color FuncHanhChinhBg  = Hex("#1A0A2E");
        public static readonly Color FuncThiNghiemBg  = Hex("#1C1000");
        public static readonly Color FuncTienIchBg    = Hex("#111316");
        public static readonly Color FuncGDTCBg       = Hex("#052010");
        public static readonly Color FuncKTXBg        = Hex("#1C0514");

        public static Color GetFuncColor(string cn)
        {
            switch(cn) {
                case "Khu giang duong": return FuncGiangDuong;
                case "Khu hanh chinh":  return FuncHanhChinh;
                case "Khu thi nghiem":  return FuncThiNghiem;
                case "Khu tien ich":    return FuncTienIch;
                case "Khu GDTC":        return FuncGDTC;
                case "Khu KTX":         return FuncKTX;
                default:                  return TextMuted;
            }
        }

        public static Color GetStatusColor(Data.BuildingStatus s)
        {
            switch(s) {
                case Data.BuildingStatus.Occupied: return StatusOccupied;
                case Data.BuildingStatus.Upcoming: return StatusUpcoming;
                case Data.BuildingStatus.Empty:    return StatusEmpty;
                default:                           return TextMuted;
            }
        }

        public static Color GetStatusBg(Data.BuildingStatus s)
        {
            switch(s) {
                case Data.BuildingStatus.Occupied: return OccupiedBg;
                case Data.BuildingStatus.Upcoming: return UpcomingBg;
                case Data.BuildingStatus.Empty:    return EmptyBg;
                default:                           return BgCard;
            }
        }

        public static Color GetStatusBorder(Data.BuildingStatus s)
        {
            switch(s) {
                case Data.BuildingStatus.Occupied: return OccupiedBorder;
                case Data.BuildingStatus.Upcoming: return UpcomingBorder;
                case Data.BuildingStatus.Empty:    return EmptyBorder;
                default:                           return Border;
            }
        }

        static Color Hex(string h) {
            ColorUtility.TryParseHtmlString(h, out Color c);
            return c;
        }
    }

    /// <summary>Giờ địa phương Việt Nam (UTC+7) — dùng thay DateTime.Now</summary>
    public static class DUTTime
    {
        static readonly System.TimeZoneInfo _vn =
            System.TimeZoneInfo.CreateCustomTimeZone("SE Asia Standard Time", System.TimeSpan.FromHours(7), "ICT", "ICT");

        public static System.DateTime Now =>
            System.TimeZoneInfo.ConvertTimeFromUtc(System.DateTime.UtcNow, _vn);
    }

}