using System;

public abstract class Trackable
{
    public DateTime CreatedTs { get; set; }
    public DateTime? LastUpdatedTs { get; set; }
}