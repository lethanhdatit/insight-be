using System;

public class EntityOTP
{
    public Guid Id { get; set; }
    public short Module { get; set; }
    public short Type { get; set; }
    public string Key { get; set; }
    public string OTP { get; set; }
    public DateTime CreatedTs { get; set; }
    public DateTime? ConfirmedTs { get; set; }
}