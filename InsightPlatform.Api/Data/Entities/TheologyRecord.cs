using System;

public class TheologyRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public byte Kind { get; set; }

    public byte Status { get; set; }

    public string Input { get; set; }

    public string PreData { get; set; }

    public string SystemPrompt { get; set; }

    public string UserPrompt { get; set; }

    public string UniqueKey { get; set; }

    public string Result { get; set; }

    public DateTime CreatedTs { get; set; }

    public User User { get; set; }
}