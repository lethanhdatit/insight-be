using System;

public class TuTruBatTuRequest : TheologyBase
{
    public DateTime birthDateTime { get; set; }

    public string birthPlace { get; set; }

    public Gender gender { get; set; }

    public TuTruBatTuCategory Category { get; set; }

    public void Standardize()
    {
        birthPlace = birthPlace?.Trim();
    }

    public string InitUniqueKey(TheologyKind kind, string sysPrompt = null, string userPrompt = null)
    {
        return string.Join("|",
             Normalize(birthPlace),
             Normalize(birthDateTime, "dd/MM/yyyy HH:mm"),
             Normalize(((short?)gender)?.ToString()),
             Normalize(((short?)Category)?.ToString()),
             Normalize(((short?)kind)?.ToString()),
             Normalize(sysPrompt),
             Normalize(userPrompt)
         ).ComputeSha256Hash();
    }
}