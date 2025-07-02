using System;

public class TuTruBatTuRequest : TheologyBase
{
    public string name { get; set; }

    public DateTime birthDateTime { get; set; }

    public string birthPlace { get; set; }

    public Gender gender { get; set; }

    public TuTruBatTuCategory category { get; set; }

    public void Standardize()
    {
        birthPlace = birthPlace?.Trim();
        name = name?.Trim();
    }

    public string InitUniqueKey(TheologyKind kind, string sysPrompt = null, string userPrompt = null)
    {
        return string.Join("|",
             Normalize(name),
             Normalize(birthPlace),
             Normalize(birthDateTime, "dd/MM/yyyy HH:mm"),
             Normalize(((short?)gender)?.ToString()),
             Normalize(((short?)category)?.ToString()),
             Normalize(((short?)kind)?.ToString()),
             Normalize(sysPrompt),
             Normalize(userPrompt)
         ).ComputeSha256Hash();
    }
}