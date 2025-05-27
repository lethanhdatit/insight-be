using System;
using System.Collections.Generic;

public class User
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; }
    public string Username { get; set; }
    public string PasswordHash { get; set; }
    public string PasswordSalt { get; set; }
    public string GoogleId { get; set; }
    public string FacebookId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserAgent { get; set; }
    public string ClientLocale { get; set; }
    public DateTime? DeletedTs { get; set; }
    public DateTime? DisabledTs { get; set; }
    public ICollection<Pain> Pains { get; set; }
}