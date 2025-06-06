using System.Collections.Generic;

public class TheologyDto
{
    public List<string> Numbers { get; set; }
    public Explanation Explanation { get; set; }
}

public class Explanation
{
    public List<ParagraphingItem> Detail { get; set; }
    public List<ParagraphingItem> Warning { get; set; }
    public List<ParagraphingItem> Advice { get; set; }
    public List<ParagraphingItem> Summary { get; set; }
}

public class ParagraphingItem
{
    public string Title { get; set; }
    public string Content { get; set; }
}