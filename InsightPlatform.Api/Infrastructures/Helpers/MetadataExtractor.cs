using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System;

public class MetadataExtractor
{
    public static Tuple<string, List<MetadataEntry>> Extract(string input, bool removeMetadataContent)
    {
        List<MetadataEntry> extractedData = new List<MetadataEntry>();
        string processedInput = input;

        string metadataPattern = @"<Metadata>(?<content>.*?)</Metadata>";
        Regex regex = new Regex(metadataPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);

        MatchCollection matches = regex.Matches(input);

        foreach (Match match in matches)
        {
            string metadataContent = match.Groups["content"].Value;

            try
            {
                string fullXmlContent = $"<Root>{metadataContent}</Root>";
                XDocument doc = XDocument.Parse(fullXmlContent);

                foreach (XElement element in doc.Root.Elements())
                {
                    MetadataEntry entry = new MetadataEntry
                    {
                        TagName = element.Name.LocalName.ToLower(),
                        Attributes = new List<MetadataAttribute>()
                    };

                    foreach (XAttribute attribute in element.Attributes())
                    {
                        string attrValue = attribute.Value;
                        List<string> values = attrValue.Contains(",") ? attrValue.Split(',').Select(s => s.Trim()).ToList() : new List<string> { attrValue };

                        entry.Attributes.Add(new MetadataAttribute
                        {
                            Name = attribute.Name.LocalName.ToLower(),
                            Value = values
                        });
                    }
                    extractedData.Add(entry);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Unable to parse XML content in a <Metadata> tag. Error:{ex.Message}");
            }
        }

        if (removeMetadataContent)
        {
            processedInput = regex.Replace(input, string.Empty);
        }

        return new Tuple<string, List<MetadataEntry>>(processedInput, extractedData);
    }
}

public class MetadataEntry
{
    public string TagName { get; set; }
    public List<MetadataAttribute> Attributes { get; set; }

    public override string ToString()
    {
        return $"TagName: {TagName}, Attributes: {string.Join(", ", Attributes.Select(a => $"{a.Name}=[{string.Join(",", a.Value)}]"))}";
    }
}

public class MetadataAttribute
{
    public string Name { get; set; }
    public List<string> Value { get; set; }
}