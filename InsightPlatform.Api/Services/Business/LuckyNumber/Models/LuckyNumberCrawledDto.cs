
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

public class LuckyNumberCrawledDto
{
    [JsonPropertyName("CrawlUrl")]
    public string CrawlUrl { get; set; }

    [JsonPropertyName("ĐB")]
    public List<string> ĐB { get; set; } = [];

    [JsonPropertyName("G1")]
    public List<string> G1 { get; set; } = [];

    [JsonPropertyName("G2")]
    public List<string> G2 { get; set; } = [];

    [JsonPropertyName("G3")]
    public List<string> G3 { get; set; } = [];

    [JsonPropertyName("G4")]
    public List<string> G4 { get; set; } = [];

    [JsonPropertyName("G5")]
    public List<string> G5 { get; set; } = [];

    [JsonPropertyName("G6")]
    public List<string> G6 { get; set; } = [];

    [JsonPropertyName("G7")]
    public List<string> G7 { get; set; } = [];

    [JsonPropertyName("G8")]
    public List<string> G8 { get; set; } = [];

    public HashSet<string> PrizeNames()
    {
       return Prizes.Select(p => p.Name).ToHashSet();
    }

    public void Standardize()
    {
        var lists = Prizes.ToList();

        foreach (var property in lists)
        {
            var list = (List<string>)property.GetValue(this);

            list.RemoveAll(item => item.IsMissing());

            property.SetValue(this, list);
        }
    }

    public bool AnyListPresent()
    {
        var lists = Prizes.Select(p => (List<string>)p.GetValue(this))
                          .ToList();

        return lists.Any(list => list != null && list.Any(item => item.IsPresent()));
    }

    public bool AllListPresent()
    {
        var lists = Prizes.Select(p => (List<string>)p.GetValue(this))
                          .ToList();

        return lists.Count != 0 && lists.All(list => list != null && list.Any(item => item.IsPresent()));
    }

    private IEnumerable<PropertyInfo> Prizes => this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                              .Where(p => p.PropertyType == typeof(List<string>));
}