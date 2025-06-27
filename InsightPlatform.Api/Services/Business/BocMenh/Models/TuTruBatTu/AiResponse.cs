using System.Collections.Generic;

public class TuTruBatTuFromAI
{    
    //public string Title { get; set; }
    //public string SubTitle { get; set; }
    //public string OpeningContent { get; set; }
    //public List<Section> Sections { get; set; } = [];
    public List<string> DungThan { get; set; } = [];
    public List<string> KyThan { get; set; } = [];
    public List<ItemHint> RecommendedItems { get; set; } = [];
}

public class Section
{
    public string Title { get; set; }
    public string SubTitle { get; set; }
    public string Content { get; set; }
    public List<Paragraph> Paragraphs { get; set; } = [];
}

public class Paragraph
{
    public string PrefixContent { get; set; }
    public string Content { get; set; }
    public string SuffixContent { get; set; }
}

public class ItemHint
{
    public string Name { get; set; }
    public string Category { get; set; }
    public List<string> Elements { get; set; } = [];
    public List<string> Uses { get; set; } = [];
    public string Material { get; set; }
    public string Description { get; set; }
}
