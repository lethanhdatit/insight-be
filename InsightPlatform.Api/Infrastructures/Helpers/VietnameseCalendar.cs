using System;
using System.Collections.Generic;
using System.Linq;

public class VietnameseCalendar
{
    private const double PI = Math.PI;

    private readonly static string[] CAN = { "Giáp", "Ất", "Bính", "Đinh", "Mậu", "Kỷ", "Canh", "Tân", "Nhâm", "Quý" };
    private readonly static string[] CHI = { "Tý", "Sửu", "Dần", "Mẹo", "Thìn", "Tỵ", "Ngọ", "Mùi", "Thân", "Dậu", "Tuất", "Hợi" };
    private readonly static string[] GIO_HD = { "110100101100", "001101001011", "110011010010", "101100110100", "001011001101", "010010110011" };
    private readonly static string[] TIET_KHI = {
            "Xuân phân", "Thanh minh", "Cốc vũ", "Lập hạ", "Tiểu mãn", "Mang chủng",
            "Hạ chí", "Tiểu thử", "Đại thử", "Lập thu", "Xử thử", "Bạch lộ",
            "Thu phân", "Hàn lộ", "Sương giáng", "Lập đông", "Tiểu tuyết", "Đại tuyết",
            "Đông chí", "Tiểu hàn", "Đại hàn", "Lập xuân", "Vũ thủy", "Kinh trập"
        };

    private readonly static int[] TK20 = {
            0x3c4bd8, 0x624ae0, 0x4ca570, 0x3854d5, 0x5cd260, 0x44d950, 0x315554, 0x5656a0, 0x409ad0, 0x2a55d2,
            0x504ae0, 0x3aa5b6, 0x60a4d0, 0x48d250, 0x33d255, 0x58b540, 0x42d6a0, 0x2cada2, 0x5295b0, 0x3f4977,
            0x644970, 0x4ca4b0, 0x36b4b5, 0x5c6a50, 0x466d40, 0x2fab54, 0x562b60, 0x409570, 0x2c52f2, 0x504970,
            0x3a6566, 0x5ed4a0, 0x48ea50, 0x336a95, 0x585ad0, 0x442b60, 0x2f86e3, 0x5292e0, 0x3dc8d7, 0x62c950,
            0x4cd4a0, 0x35d8a6, 0x5ab550, 0x4656a0, 0x31a5b4, 0x5625d0, 0x4092d0, 0x2ad2b2, 0x50a950, 0x38b557,
            0x5e6ca0, 0x48b550, 0x355355, 0x584da0, 0x42a5b0, 0x2f4573, 0x5452b0, 0x3ca9a8, 0x60e950, 0x4c6aa0,
            0x36aea6, 0x5aab50, 0x464b60, 0x30aae4, 0x56a570, 0x405260, 0x28f263, 0x4ed940, 0x38db47, 0x5cd6a0,
            0x4896d0, 0x344dd5, 0x5a4ad0, 0x42a4d0, 0x2cd4b4, 0x52b250, 0x3cd558, 0x60b540, 0x4ab5a0, 0x3755a6,
            0x5c95b0, 0x4649b0, 0x30a974, 0x56a4b0, 0x40aa50, 0x29aa52, 0x4e6d20, 0x39ad47, 0x5eab60, 0x489370,
            0x344af5, 0x5a4970, 0x4464b0, 0x2c74a3, 0x50ea50, 0x3d6a58, 0x6256a0, 0x4aaad0, 0x3696d5, 0x5c92e0
        };

    private readonly static int[] TK21 = {
            0x46c960, 0x2ed954, 0x54d4a0, 0x3eda50, 0x2a7552, 0x4e56a0, 0x38a7a7, 0x5ea5d0, 0x4a92b0, 0x32aab5,
            0x58a950, 0x42b4a0, 0x2cbaa4, 0x50ad50, 0x3c55d9, 0x624ba0, 0x4ca5b0, 0x375176, 0x5c5270, 0x466930,
            0x307934, 0x546aa0, 0x3ead50, 0x2a5b52, 0x504b60, 0x38a6e6, 0x5ea4e0, 0x48d260, 0x32ea65, 0x56d520,
            0x40daa0, 0x2d56a3, 0x5256d0, 0x3c4afb, 0x6249d0, 0x4ca4d0, 0x37d0b6, 0x5ab250, 0x44b520, 0x2edd25,
            0x54b5a0, 0x3e55d0, 0x2a55b2, 0x5049b0, 0x3aa577, 0x5ea4b0, 0x48aa50, 0x33b255, 0x586d20, 0x40ad60,
            0x2d4b63, 0x525370, 0x3e49e8, 0x60c970, 0x4c54b0, 0x3768a6, 0x5ada50, 0x445aa0, 0x2fa6a4, 0x54aad0,
            0x4052e0, 0x28d2e3, 0x4ec950, 0x38d557, 0x5ed4a0, 0x46d950, 0x325d55, 0x5856a0, 0x42a6d0, 0x2c55d4,
            0x5252b0, 0x3ca9b8, 0x62a930, 0x4ab490, 0x34b6a6, 0x5aad50, 0x4655a0, 0x2eab64, 0x54a570, 0x4052b0,
            0x2ab173, 0x4e6930, 0x386b37, 0x5e6aa0, 0x48ad50, 0x332ad5, 0x582b60, 0x42a570, 0x2e52e4, 0x50d160,
            0x3ae958, 0x60d520, 0x4ada90, 0x355aa6, 0x5a56d0, 0x462ae0, 0x30a9d4, 0x54a2d0, 0x3ed150, 0x28e952
    };

    private readonly static List<Holiday> SolarHolidays =
        [
            new Holiday(1, 1, "Tết Dương lịch"),
            new Holiday(9, 1, "Ngày Học sinh - Sinh viên Việt Nam"),
            new Holiday(3, 2, "Ngày thành lập Đảng Cộng sản Việt Nam"),
            new Holiday(27, 2, "Ngày Thầy thuốc Việt Nam"),
            new Holiday(8, 3, "Ngày Quốc tế Phụ nữ"),
            new Holiday(26, 3, "Ngày thành lập Đoàn Thanh niên Cộng sản Hồ Chí Minh"),
            new Holiday(21, 4, "Ngày Sách Việt Nam"),
            new Holiday(30, 4, "Ngày Thống nhất đất nước"),
            new Holiday(1, 5, "Ngày Quốc tế Lao động"),
            new Holiday(15, 5, "Ngày thành lập Đội Thiếu niên Tiền phong Hồ Chí Minh"),
            new Holiday(19, 5, "Ngày sinh của Chủ tịch Hồ Chí Minh"),
            new Holiday(1, 6, "Ngày Quốc tế Thiếu nhi"),
            new Holiday(5, 6, "Ngày Bác Hồ ra đi tìm đường cứu nước"),
            new Holiday(27, 7, "Ngày Thương binh Liệt sĩ"),
            new Holiday(19, 8, "Ngày Cách mạng tháng Tám thành công"),
            new Holiday(2, 9, "Ngày Quốc khánh"),
            new Holiday(13, 10, "Ngày Doanh nhân Việt Nam"),
            new Holiday(20, 10, "Ngày thành lập Hội Phụ nữ Việt Nam"),
            new Holiday(20, 11, "Ngày Nhà giáo Việt Nam"),
            new Holiday(22, 12, "Ngày thành lập Quân đội Nhân dân Việt Nam"),
            new Holiday(24, 12, "Ngày Lễ Giáng Sinh")
        ];

    private readonly static List<Holiday> LunarHolidays = new List<Holiday>
        {
            new Holiday(1, 1, "Tết Nguyên Đán"),
            new Holiday(15, 1, "Tết Nguyên tiêu"),
            new Holiday(3, 3, "Tết Hàn thực"),
            new Holiday(10, 3, "Giỗ Tổ Hùng Vương"),
            new Holiday(15, 4, "Lễ Phật Đản"),
            new Holiday(5, 5, "Tết Đoan ngọ"),
            new Holiday(15, 7, "Vu Lan"),
            new Holiday(15, 8, "Tết Trung thu"),
            new Holiday(23, 12, "Ông Táo chầu trời")
        };

    public static CalendarMeta GetLunarCalendarDetails(DateTime solarDate, bool includeMonthDetail = false)
    {
        var lunarDetails = new LunarInfo();
        var lunarToday = GetLunarDate(solarDate.Day, solarDate.Month, solarDate.Year, solarDate.Hour);

        lunarDetails.SolarDate = new DateInfo
        {
            IsLeapMonth = false,
            Date = solarDate,
            DayOfWeek = new DoW(solarDate.DayOfWeek)
        };

        var lunarDate = new DateTime(lunarToday.Year, lunarToday.Month, lunarToday.Day, lunarToday.Hour, 0, 0, 0);

        lunarDetails.LunarDate = new DateInfo
        {
            IsLeapMonth = lunarToday.IsLeapMonth,
            Date = lunarDate,
            DayOfWeek = new DoW(lunarDate.DayOfWeek)
        };

        lunarDetails.CanChi = GetCanChi(lunarToday);
        lunarDetails.CanHour0 = $"{GetCanHour0(lunarToday.JulianDay)} {CHI[0]}";
        lunarDetails.SolarTerm = TIET_KHI[GetSunLongitude(lunarToday.JulianDay + 1, 7.0)];
        lunarDetails.BuddhistCalendar = GetPhatLich(lunarToday.Day, lunarToday.Month, lunarToday.Year);

        var holidays = GetHolidays(solarDate.Day, solarDate.Month, lunarToday.Day, lunarToday.Month);
        lunarDetails.Holidays = !string.IsNullOrEmpty(holidays) ? holidays : null;

        lunarDetails.AuspiciousHour = GetGioHoangDao(lunarToday.JulianDay);

        var monthDetails = includeMonthDetail ? GetMonthCalendar(solarDate.Month, solarDate.Year) : null;

        return new CalendarMeta
        {
            LunarDetails = lunarDetails,
            MonthDetails = monthDetails
        };
    }

    public static LunarDate GetLunarDate(int day, int month, int year, int hour = 0)
    {
        if (year < 1900 || year > 2099)
        {
            return new LunarDate(0, 0, 0, false, 0);
        }

        var ly = GetYearInfo(year);
        int jd = JulianDayNumber(day, month, year);

        if (jd < ly[0].JulianDay)
        {
            ly = GetYearInfo(year - 1);
        }

        return FindLunarDate(jd, ly, hour);
    }

    public static CanChi GetCanChiForHour(CanChi canChiDay, int hour)
    {
        int canIndex = Array.IndexOf(CAN, canChiDay.Can);
        int chiIndex = Array.IndexOf(CHI, canChiDay.Chi);

        int hourIndex = GetChiIndexByHour(hour);

        string chiHour = CHI[hourIndex];

        string canHour = CAN[(canIndex + hourIndex) % 10];

        return new CanChi(canHour, chiHour);
    }

    public static CanChiInfo GetCanChi(LunarDate lunar)
    {
        var day = new CanChi(CAN[(lunar.JulianDay + 9) % 10], CHI[(lunar.JulianDay + 1) % 12]);
        var month = new CanChi(CAN[(lunar.Year * 12 + lunar.Month + 3) % 10], CHI[(lunar.Month + 1) % 12], lunar.IsLeapMonth);
        var year = new CanChi(CAN[(lunar.Year + 6) % 10], CHI[(lunar.Year + 8) % 12]);
        var hour = GetCanChiForHour(day, lunar.Hour);

        return new CanChiInfo(day, month, year, hour);
    }

    public static string GetCanHour0(int jdn)
    {
        return CAN[(jdn - 1) * 2 % 10];
    }

    public static int GetPhatLich(int lunarDay, int lunarMonth, int lunarYear)
    {
        return (lunarMonth > 4 || (lunarMonth >= 4 && lunarDay >= 15)) ? lunarYear + 544 : (lunarYear + 544) - 1;
    }

    public static string GetGioHoangDao(int jd)
    {
        int chiOfDay = (jd + 1) % 12;
        string gioHD = GIO_HD[chiOfDay % 6];
        var result = new List<string>();

        for (int i = 0; i < 12; i++)
        {
            if (gioHD[i] == '1')
            {
                result.Add($"{CHI[i]} ({(i * 2 + 23) % 24}-{(i * 2 + 1) % 24})");
            }
        }

        return string.Join(", ", result);
    }


    public static int GetSunLongitude(int dayNumber, double timeZone)
    {
        return INT(SunLongitude(dayNumber - 0.5 - timeZone / 24.0) / PI * 12);
    }

    public static string GetHolidays(int solarDay, int solarMonth, int lunarDay, int lunarMonth)
    {
        var holidays = new List<string>();

        var lunarHoliday = LunarHolidays.FirstOrDefault(h => h.Day == lunarDay && h.Month == lunarMonth);
        if (lunarHoliday != null)
        {
            holidays.Add($"{lunarHoliday.Name} ({lunarHoliday.Day}/{lunarHoliday.Month} ÂL)");
        }

        var solarHoliday = SolarHolidays.FirstOrDefault(h => h.Day == solarDay && h.Month == solarMonth);
        if (solarHoliday != null)
        {
            holidays.Add($"{solarHoliday.Name} ({solarHoliday.Day}/{solarHoliday.Month} DL)");
        }

        return string.Join(", ", holidays);
    }

    public static List<CalendarDay> GetMonthCalendar(int month, int year)
    {
        var result = new List<CalendarDay>();

        var daysInMonth = DateTime.DaysInMonth(year, month);

        for (int day = 1; day <= daysInMonth; day++)
        {
            var lunarDate = GetLunarDate(day, month, year);
            var currentDate = new DateTime(year, month, day);

            result.Add(new CalendarDay
            {
                Day = day,
                Date = new DateTime(lunarDate.Year, lunarDate.Month, lunarDate.Day),
                DayOfWeek = new DoW(currentDate.DayOfWeek)
            });
        }

        return result;
    }

    private static int INT(double d)
    {
        return (int)Math.Floor(d);
    }

    private static int JulianDayNumber(int day, int month, int year)
    {
        int a = INT((14 - month) / 12);
        int y = year + 4800 - a;
        int m = month + 12 * a - 3;
        int jd = day + INT((153 * m + 2) / 5) + 365 * y + INT(y / 4) - INT(y / 100) + INT(y / 400) - 32045;
        return jd;
    }

    private static int[] JulianDayToDate(int jd)
    {
        int Z, A, alpha, B, C, D, E, dd, mm, yyyy;
        Z = jd;
        if (Z < 2299161)
        {
            A = Z;
        }
        else
        {
            alpha = INT((Z - 1867216.25) / 36524.25);
            A = Z + 1 + alpha - INT(alpha / 4);
        }
        B = A + 1524;
        C = INT((B - 122.1) / 365.25);
        D = INT(365.25 * C);
        E = INT((B - D) / 30.6001);
        dd = INT(B - D - INT(30.6001 * E));
        if (E < 14)
        {
            mm = E - 1;
        }
        else
        {
            mm = E - 13;
        }
        if (mm < 3)
        {
            yyyy = C - 4715;
        }
        else
        {
            yyyy = C - 4716;
        }
        return new int[] { dd, mm, yyyy };
    }

    private static List<LunarDate> DecodeLunarYear(int year, int yearCode)
    {
        var monthLengths = new int[] { 29, 30 };
        var regularMonths = new int[12];
        int offsetOfTet = yearCode >> 17;
        int leapMonth = yearCode & 0xf;
        int leapMonthLength = monthLengths[yearCode >> 16 & 0x1];
        int solarNY = JulianDayNumber(1, 1, year);
        int currentJD = solarNY + offsetOfTet;
        int j = yearCode >> 4;

        for (int i = 0; i < 12; i++)
        {
            regularMonths[12 - i - 1] = monthLengths[j & 0x1];
            j >>= 1;
        }

        var ly = new List<LunarDate>();
        if (leapMonth == 0)
        {
            for (int mm = 1; mm <= 12; mm++)
            {
                ly.Add(new LunarDate(1, mm, year, false, currentJD));
                currentJD += regularMonths[mm - 1];
            }
        }
        else
        {
            for (int mm = 1; mm <= leapMonth; mm++)
            {
                ly.Add(new LunarDate(1, mm, year, false, currentJD));
                currentJD += regularMonths[mm - 1];
            }
            ly.Add(new LunarDate(1, leapMonth, year, true, currentJD));
            currentJD += leapMonthLength;
            for (int mm = leapMonth + 1; mm <= 12; mm++)
            {
                ly.Add(new LunarDate(1, mm, year, false, currentJD));
                currentJD += regularMonths[mm - 1];
            }
        }
        return ly;
    }

    private static List<LunarDate> GetYearInfo(int year)
    {
        int yearCode;
        if (year >= 1900 && year < 2000)
        {
            yearCode = TK20[year - 1900];
        }
        else if (year >= 2000 && year < 2100)
        {
            yearCode = TK21[year - 2000];
        }
        else
        {
            throw new ArgumentException("Year not supported");
        }
        return DecodeLunarYear(year, yearCode);
    }

    private static LunarDate FindLunarDate(int jd, List<LunarDate> ly, int hour = 0)
    {
        if (ly[0].JulianDay > jd)
        {
            return new LunarDate(0, 0, 0, false, jd);
        }

        int i = ly.Count - 1;
        while (jd < ly[i].JulianDay)
        {
            i--;
        }

        int off = jd - ly[i].JulianDay;
        return new LunarDate(ly[i].Day + off, ly[i].Month, ly[i].Year, ly[i].IsLeapMonth, jd, hour);
    }

    private static int GetChiIndexByHour(int hour)
    {
        if (hour < 0 || hour > 24)
            throw new ArgumentOutOfRangeException("hour", "Hour must be in range 0 - 24");

        return (hour == 23 || hour == 24) ? 0 : ((hour + 1) / 2) % 12;
    }
    
    private static double SunLongitude(double jdn)
    {
        double T = (jdn - 2451545.0) / 36525;
        double T2 = T * T;
        double dr = PI / 180;
        double M = 357.52910 + 35999.05030 * T - 0.0001559 * T2 - 0.00000048 * T * T2;
        double L0 = 280.46645 + 36000.76983 * T + 0.0003032 * T2;
        double DL = (1.914600 - 0.004817 * T - 0.000014 * T2) * Math.Sin(dr * M);
        DL = DL + (0.019993 - 0.000101 * T) * Math.Sin(dr * 2 * M) + 0.000290 * Math.Sin(dr * 3 * M);
        double theta = L0 + DL;
        double omega = 125.04 - 1934.136 * T;
        double lambda = theta - 0.00569 - 0.00478 * Math.Sin(omega * dr);
        lambda = lambda * dr;
        lambda = lambda - PI * 2 * (INT(lambda / (PI * 2)));
        return lambda;
    }
}

public class Holiday
{
    public int Day { get; set; }
    public int Month { get; set; }
    public string Name { get; set; }

    public Holiday(int day, int month, string name)
    {
        Day = day;
        Month = month;
        Name = name;
    }
}

public class LunarDate
{
    public int Day { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int Hour { get; set; }
    public bool IsLeapMonth { get; set; }
    public int JulianDay { get; set; }

    public LunarDate(int day, int month, int year, bool isLeapMonth, int julianDay, int hour = 0)
    {
        Day = day;
        Month = month;
        Year = year;
        IsLeapMonth = isLeapMonth;
        JulianDay = julianDay;
        Hour = hour;
    }
}

public class CanChiInfo(CanChi day, CanChi month, CanChi year, CanChi hour)
{
    public CanChi CanChiDay { get; set; } = day;
    public CanChi CanChiMonth { get; set; } = month;
    public CanChi CanChiYear { get; set; } = year;
    public CanChi CanChiHour { get; set; } = hour;
}

public class CanChi(string can, string chi, bool isLeap = false)
{
    public string Can { get; set; } = can;
    public string Chi { get; set; } = chi;
    public bool IsLeap { get; set; } = isLeap;
    public string Display { get { return $"{Can} {Chi}{(IsLeap ? " (N)" : string.Empty)}"; } }
}

public class CalendarDay
{
    public int Day { get; set; }
    public DateTime Date { get; set; }
    public DoW DayOfWeek { get; set; }
}

public class DoW
{
    public DoW(DayOfWeek code)
    {
        Code = code;
    }

    public DayOfWeek Code { get; set; }

    public string Name
    {
        get
        {
            return Code switch
            {
                DayOfWeek.Sunday => "CN",
                DayOfWeek.Monday => "T2",
                DayOfWeek.Tuesday => "T3",
                DayOfWeek.Wednesday => "T4",
                DayOfWeek.Thursday => "T5",
                DayOfWeek.Friday => "T6",
                DayOfWeek.Saturday => "T7",
                _ => string.Empty
            };
        }
    }
}

public class LunarInfo
{
    public DateInfo SolarDate { get; set; }
    public DateInfo LunarDate { get; set; }
    public bool IsLeapMonth { get; set; }
    public CanChiInfo CanChi { get; set; }
    public string CanHour0 { get; set; }
    public string SolarTerm { get; set; }
    public int BuddhistCalendar { get; set; }
    public string Holidays { get; set; }
    public string AuspiciousHour { get; set; }
}

public class DateInfo
{
    public DateTime Date { get; set; }

    public bool IsLeapMonth { get; set; }

    public DoW DayOfWeek { get; set; }
}

public class CalendarMeta
{
    public LunarInfo LunarDetails { get; set; }
    public List<CalendarDay> MonthDetails { get; set; }
}