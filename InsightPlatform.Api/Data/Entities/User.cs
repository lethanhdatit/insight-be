using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

public class User : Trackable
{
    public Guid Id { get; set; }
    
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AutoId { get; set; }
    
    public string DisplayName { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public string PasswordHash { get; set; }
    public string PasswordSalt { get; set; }
    public string GoogleId { get; set; }
    public string FacebookId { get; set; }
    public string UserAgent { get; set; }
    public string ClientLocale { get; set; }
    public int Fates { get; set; }
    public DateTime? DeletedTs { get; set; }
    public DateTime? DisabledTs { get; set; }

    public ICollection<Pain> Pains { get; set; }
    public ICollection<TheologyRecord> TheologyRecords { get; set; }
    public ICollection<Transaction> Transactions { get; set; }
    public ICollection<FatePointTransaction> FatePointTransactions { get; set; }
}