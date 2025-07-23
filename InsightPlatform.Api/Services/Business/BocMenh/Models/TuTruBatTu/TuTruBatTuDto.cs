using System.Collections.Generic;
using System.Text.Json.Serialization;

public class TuTruBatTuDto
{
    [JsonPropertyName("original")]
    public string Original { get; set; }
    
    [JsonPropertyName("metaData")]
    public List<MetadataEntry> MetaData { get; set; }
}

// Lớp cha chứa toàn bộ kết quả luận giải
public class TuTruAnalysisResult
{
    [JsonPropertyName("day_master_analysis")]
    public SectionContent DayMasterAnalysis { get; set; }

    [JsonPropertyName("five_elements_analysis")]
    public FiveElementsAnalysis FiveElementsAnalysis { get; set; }

    [JsonPropertyName("ten_gods_analysis")]
    public SectionContent TenGodsAnalysis { get; set; }

    [JsonPropertyName("useful_and_unfavorable_gods")]
    public UsefulUnfavorableGods UsefulAndUnfavorableGods { get; set; }

    [JsonPropertyName("ten_year_cycles")]
    public TenYearCycles TenYearCycles { get; set; }

    [JsonPropertyName("career_guidance")]
    public SectionContent CareerGuidance { get; set; }

    [JsonPropertyName("improvement_suggestions")]
    public ImprovementSuggestions ImprovementSuggestions { get; set; }

    [JsonPropertyName("conclusion")]
    public SectionContent Conclusion { get; set; }

    public void ToFree()
    {
        FiveElementsAnalysis?.ToFree();
        TenGodsAnalysis?.ToFree();
        UsefulAndUnfavorableGods?.ToFree();
        TenYearCycles?.ToFree();
        CareerGuidance?.ToFree();
        ImprovementSuggestions?.ToFree();
        Conclusion?.ToFree();
    }
}

// Cấu trúc chung cho các mục luận giải
public class SectionContent
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("key_point")]
    public string KeyPoint { get; set; }

    [JsonPropertyName("detailed_analysis")]
    public string DetailedAnalysis { get; set; }

    public virtual void ToFree()
    {
        KeyPoint = null;
        DetailedAnalysis = null;
    }
}

// Mục luận giải Ngũ Hành, có thêm dữ liệu cho biểu đồ
public class FiveElementsAnalysis : SectionContent
{
    [JsonPropertyName("element_distribution")]
    public List<ElementInfo> ElementDistribution { get; set; }

    public override void ToFree()
    {
        base.ToFree();
        ElementDistribution = null;
    }
}

public class ElementInfo
{
    [JsonPropertyName("element")]
    public string Element { get; set; } // "Kim", "Mộc", "Thủy", "Hỏa", "Thổ"

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("strength")]
    public string Strength { get; set; } // "Vượng", "Tướng", "Hưu", "Tù", "Tử" hoặc "Vượng", "Nhược"
}

// Mục Dụng Thần, Kỵ Thần
public class UsefulUnfavorableGods : SectionContent
{
    [JsonPropertyName("useful_gods")]
    public GodInfo UsefulGods { get; set; }

    [JsonPropertyName("unfavorable_gods")]
    public GodInfo UnfavorableGods { get; set; }

    public override void ToFree()
    {
        base.ToFree();
        UsefulGods = null;
        UnfavorableGods = null;
    }
}

public class GodInfo : SectionContent
{
    [JsonPropertyName("elements")]
    public List<string> Elements { get; set; }

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; }

    public override void ToFree()
    {
        base.ToFree();
        Elements = null;
        Explanation = null;
    }
}

// Mục Đại Vận
public class TenYearCycles : SectionContent
{
    [JsonPropertyName("cycles")]
    public List<CycleInfo> Cycles { get; set; }

    public override void ToFree()
    {
        base.ToFree();
        Cycles = null;
    }
}

public class CycleInfo
{
    [JsonPropertyName("age_range")]
    public string AgeRange { get; set; }

    [JsonPropertyName("can_chi")]
    public string CanChi { get; set; }

    [JsonPropertyName("element")]
    public string Element { get; set; }

    [JsonPropertyName("analysis")]
    public string Analysis { get; set; }
}

// Mục Cải Vận
public class ImprovementSuggestions : SectionContent
{
    [JsonPropertyName("feng_shui_items")]
    public List<FengShuiItem> FengShuiItems { get; set; }

    public override void ToFree()
    {
        base.ToFree();
        FengShuiItems = null;
    }
}

public class FengShuiItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("elements")]
    public List<string> Elements { get; set; }

    [JsonPropertyName("material")]
    public string Material { get; set; }

    [JsonPropertyName("purpose")]
    public string Purpose { get; set; }

    [JsonPropertyName("usage_instructions")]
    public string UsageInstructions { get; set; }
}
