using System.Collections.Generic;
using System.Text.Json.Serialization;

public class Info
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
}

public class CompassEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("compass")]
    public string Compass { get; set; }

    [JsonPropertyName("zodiac")]
    public string Zodiac { get; set; }

    [JsonPropertyName("five_elements")]
    public string FiveElements { get; set; }

    [JsonPropertyName("info")]
    public Info Info { get; set; }
}

public class Compass
{
    [JsonPropertyName("good")]
    public List<CompassEntry> Good { get; set; }

    [JsonPropertyName("bad")]
    public List<CompassEntry> Bad { get; set; }
}

public class LSBTDateInfo
{
    [JsonPropertyName("solar")]
    public Solar Solar { get; set; }

    [JsonPropertyName("lunar")]
    public Lunar Lunar { get; set; }

    [JsonPropertyName("nonglich")]
    public Nonglich Nonglich { get; set; }
}

public class Solar
{
    [JsonPropertyName("day")]
    public int Day { get; set; }

    [JsonPropertyName("month")]
    public int Month { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }
}

public class Lunar
{
    [JsonPropertyName("day")]
    public int Day { get; set; }

    [JsonPropertyName("month")]
    public int Month { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }
}

public class Nonglich
{
    [JsonPropertyName("day")]
    public int Day { get; set; }

    [JsonPropertyName("month")]
    public string Month { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }
}

public class TruBase
{
    [JsonPropertyName("vongtruongsinh")]
    public VongTruongSinh VongTruongSinh { get; set; }

    [JsonPropertyName("thansat")]
    public List<ThanSatItem> ThanSat { get; set; }

    [JsonPropertyName("napam")]
    public NapAm NapAm { get; set; }
}

public class TruGio : TruBase
{
    [JsonPropertyName("hour_can")]
    public string HourCan { get; set; }

    [JsonPropertyName("hour_chi")]
    public string HourChi { get; set; }

    [JsonPropertyName("hour_can_chi")]
    public string HourCanChi { get; set; }

    [JsonPropertyName("hour_can_nguhanh")]
    public string HourCanNguHanh { get; set; }

    [JsonPropertyName("hour_chi_nguhanh")]
    public string HourChiNguHanh { get; set; }

    [JsonPropertyName("hour_tang_can_chi_nguhanh")]
    public List<CanNguHanh> HourTangCanChiNguHanh { get; set; }

    [JsonPropertyName("hour_tang_can_chi")]
    public List<string> HourTangCanChi { get; set; }

    [JsonPropertyName("hour_can_thap_than")]
    public ThapThan HourCanThapThan { get; set; }

    [JsonPropertyName("hour_thap_than")]
    public List<ThapThan> HourThapThan { get; set; }
}

public class TruNgay : TruBase
{
    [JsonPropertyName("day")]
    public int? Day { get; set; }

    [JsonPropertyName("day_lunar")]
    public int? DayLunar { get; set; }

    [JsonPropertyName("day_can")]
    public string DayCan { get; set; }

    [JsonPropertyName("day_chi")]
    public string DayChi { get; set; }

    [JsonPropertyName("day_can_chi")]
    public string DayCanChi { get; set; }

    [JsonPropertyName("day_can_nguhanh")]
    public string DayCanNguHanh { get; set; }

    [JsonPropertyName("day_chi_nguhanh")]
    public string DayChiNguHanh { get; set; }

    [JsonPropertyName("day_tang_can_chi_nguhanh")]
    public List<CanNguHanh> DayTangCanChiNguHanh { get; set; }

    [JsonPropertyName("day_tang_can_chi")]
    public List<string> DayTangCanChi { get; set; }

    [JsonPropertyName("day_can_thap_than")]
    public ThapThan DayCanThapThan { get; set; }

    [JsonPropertyName("day_thap_than")]
    public List<ThapThan> DayThapThan { get; set; }
}

public class TruThang : TruBase
{
    [JsonPropertyName("month")]
    public int? Month { get; set; }

    [JsonPropertyName("month_lunar")]
    public int? MonthLunar { get; set; }

    [JsonPropertyName("month_can")]
    public string MonthCan { get; set; }

    [JsonPropertyName("month_chi")]
    public string MonthChi { get; set; }

    [JsonPropertyName("month_can_chi")]
    public string MonthCanChi { get; set; }

    [JsonPropertyName("month_can_nguhanh")]
    public string MonthCanNguHanh { get; set; }

    [JsonPropertyName("month_chi_nguhanh")]
    public string MonthChiNguHanh { get; set; }

    [JsonPropertyName("month_tang_can_chi_nguhanh")]
    public List<CanNguHanh> MonthTangCanChiNguHanh { get; set; }

    [JsonPropertyName("month_tang_can_chi")]
    public List<string> MonthTangCanChi { get; set; }

    [JsonPropertyName("month_can_thap_than")]
    public ThapThan MonthCanThapThan { get; set; }

    [JsonPropertyName("month_thap_than")]
    public List<ThapThan> MonthThapThan { get; set; }
}

public class TruNam : TruBase
{
    [JsonPropertyName("year")]
    public int? Year { get; set; }

    [JsonPropertyName("year_lunar")]
    public int? YearLunar { get; set; }

    [JsonPropertyName("year_can")]
    public string YearCan { get; set; }

    [JsonPropertyName("year_chi")]
    public string YearChi { get; set; }

    [JsonPropertyName("year_can_chi")]
    public string YearCanChi { get; set; }

    [JsonPropertyName("year_can_nguhanh")]
    public string YearCanNguHanh { get; set; }

    [JsonPropertyName("year_chi_nguhanh")]
    public string YearChiNguHanh { get; set; }

    [JsonPropertyName("year_tang_can_chi_nguhanh")]
    public List<CanNguHanh> YearTangCanChiNguHanh { get; set; }

    [JsonPropertyName("year_tang_can_chi")]
    public List<string> YearTangCanChi { get; set; }

    [JsonPropertyName("year_can_thap_than")]
    public ThapThan YearCanThapThan { get; set; }

    [JsonPropertyName("year_thap_than")]
    public List<ThapThan> YearThapThan { get; set; }
}

public class CanNguHanh
{
    [JsonPropertyName("can")]
    public string Can { get; set; }

    [JsonPropertyName("nguhanh")]
    public string NguHanh { get; set; }
}

public class ThapThan
{
    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("info")]
    public Info Info { get; set; }
}

public class VongTruongSinh
{
    [JsonPropertyName("can")]
    public string Can { get; set; }

    [JsonPropertyName("chi")]
    public string Chi { get; set; }

    [JsonPropertyName("info")]
    public Info Info { get; set; }
}

public class ThanSatItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("hour")]
    public string Hour { get; set; }

    [JsonPropertyName("day")]
    public string Day { get; set; }

    [JsonPropertyName("month")]
    public string Month { get; set; }

    [JsonPropertyName("year")]
    public string Year { get; set; }

    [JsonPropertyName("toa")]
    public string Toa { get; set; }

    [JsonPropertyName("year_can_chu")]
    public string YearCanChu { get; set; }

    [JsonPropertyName("year_chi_chu")]
    public string YearChiChu { get; set; }

    [JsonPropertyName("day_can_chu")]
    public string DayCanChu { get; set; }

    [JsonPropertyName("day_chi_chu")]
    public string DayChiChu { get; set; }

    [JsonPropertyName("info")]
    public Info Info { get; set; }
}

public class NapAm
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("info")]
    public Info Info { get; set; }
}

public class Vongtruongsinh
{
    [JsonPropertyName("can")]
    public string Can { get; set; }

    [JsonPropertyName("chi")]
    public string Chi { get; set; }

    [JsonPropertyName("info")]
    public Info Info { get; set; }
}

public class ThansatEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("hour")]
    public string Hour { get; set; }

    [JsonPropertyName("toa")]
    public string Toa { get; set; }

    [JsonPropertyName("year_can_chu")]
    public string YearCanChu { get; set; }

    [JsonPropertyName("info")]
    public Info Info { get; set; }
}

public class NapamEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("info")]
    public Info Info { get; set; }
}

public class DaivanEntry
{
    [JsonPropertyName("age_start")]
    public int AgeStart { get; set; }

    [JsonPropertyName("age_content")]
    public string AgeContent { get; set; }

    [JsonPropertyName("thuan_nghich")]
    public string ThuanNghich { get; set; }

    [JsonPropertyName("can")]
    public List<CanChiEntry> Can { get; set; }

    [JsonPropertyName("chi")]
    public List<ChiEntry> Chi { get; set; }

    [JsonPropertyName("tangcan")]
    public List<List<CanChiEntry>> Tangcan { get; set; }

    [JsonPropertyName("age")]
    public List<AgeEntry> Age { get; set; }

    [JsonPropertyName("circle_life")]
    public List<CircleLifeEntry> CircleLife { get; set; }

    [JsonPropertyName("thansat")]
    public List<List<ThansatEntry>> Thansat { get; set; }

    [JsonPropertyName("napam")]
    public List<NapamEntry> Napam { get; set; }

    [JsonPropertyName("vongtruongsinh")]
    public List<Vongtruongsinh> Vongtruongsinh { get; set; }
}

public class CanChiEntry
{
    [JsonPropertyName("can")]
    public string Can { get; set; }

    [JsonPropertyName("nguhanh")]
    public string Nguhanh { get; set; }

    [JsonPropertyName("thapthan")]
    public ThapthanEntry Thapthan { get; set; }
}

public class ThapthanEntry
{
    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("info")]
    public Info Info { get; set; }
}

public class ChiEntry
{
    [JsonPropertyName("chi")]
    public string Chi { get; set; }

    [JsonPropertyName("nguhanh")]
    public string Nguhanh { get; set; }
}

public class AgeEntry
{
    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("age")]
    public int Age { get; set; }

    [JsonPropertyName("can_chi")]
    public string CanChi { get; set; }
}

public class CircleLifeEntry
{
    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("can_chi")]
    public string CanChi { get; set; }
}

public class ThaimenhcungEntry
{
    [JsonPropertyName("thaicung")]
    public Thaicung Thaicung { get; set; }

    [JsonPropertyName("chi-idx")]
    public int ChiIdx { get; set; }

    [JsonPropertyName("menhcung")]
    public Menhcung Menhcung { get; set; }
}

public class Thaicung
{
    [JsonPropertyName("can")]
    public string Can { get; set; }

    [JsonPropertyName("chi")]
    public string Chi { get; set; }
}

public class Menhcung
{
    [JsonPropertyName("can")]
    public string Can { get; set; }

    [JsonPropertyName("chi")]
    public string Chi { get; set; }

    [JsonPropertyName("month")]
    public int Month { get; set; }

    [JsonPropertyName("can_chi")]
    public string CanChi { get; set; }

    [JsonPropertyName("so_thang_sinh")]
    public int SoThangSinh { get; set; }

    [JsonPropertyName("chi_thang_sinh")]
    public string ChiThangSinh { get; set; }
}

public class ThanSat
{
    [JsonPropertyName("name")]
    public string name { get; set; }

    [JsonPropertyName("day")]
    public string day { get; set; }

    [JsonPropertyName("year")]
    public string year { get; set; }

    [JsonPropertyName("info")]
    public Info Info { get; set; }
}

public class TuTruInfo
{
    [JsonPropertyName("thoi_tru")]
    public TruGio ThoiTru { get; set; }

    [JsonPropertyName("nhat_tru")]
    public TruNgay NhatTru { get; set; }

    [JsonPropertyName("nguyet_tru")]
    public TruThang NguyetTru { get; set; }

    [JsonPropertyName("thien_tru")]
    public TruNam ThienTru { get; set; }
}

public class LaSoBatTuResponse
{
    [JsonPropertyName("compass")]
    public List<Compass> Compass { get; set; }

    [JsonPropertyName("date")]
    public LSBTDateInfo Date { get; set; }

    [JsonPropertyName("tutru")]
    public TuTruInfo Tutru { get; set; }

    [JsonPropertyName("thansat")]
    public List<ThanSat> Thansat { get; set; }

    [JsonPropertyName("daivan")]
    public DaivanEntry Daivan { get; set; }

    [JsonPropertyName("thaimenhcung")]
    public ThaimenhcungEntry Thaimenhcung { get; set; }

    [JsonPropertyName("is_fee")]
    public bool IsFee { get; set; }
}
