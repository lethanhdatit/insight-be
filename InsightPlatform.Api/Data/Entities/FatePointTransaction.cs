using System;

public class FatePointTransaction : Trackable
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? TheologyRecordId { get; set; }
    public Guid? TransactionId { get; set; }
    public int Fates { get; set; } // + -

    public User User { get; set; }
    public Transaction Transaction { get; set; }
    public TheologyRecord TheologyRecord { get; set; }
}
