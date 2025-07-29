using System;
using System.Collections.Generic;

public class TheologyRecord
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid ServicePriceId { get; set; }

    public byte Kind { get; set; }

    public byte Status { get; set; }

    public string ServicePriceSnap { get; set; }

    public string Input { get; set; }

    public string PreData { get; set; }

    public string SystemPrompt { get; set; }

    public string UserPrompt { get; set; }

    public string CombinedPrompts { get; set; }    

    public string UniqueKey { get; set; }

    public string Result { get; set; }

    public DateTime CreatedTs { get; set; }
    public DateTime? FirstAnalysisTs { get; set; }
    public DateTime? LastAnalysisTs { get; set; }
    public DateTime? SuccessedAnalysisTs { get; set; }
    public DateTime? FailedAnalysisTs { get; set; }

    public int? FailedCount { get; set; }

    public List<string> Errors { get; set; }

    public User User { get; set; }

    public ServicePrice ServicePrice { get; set; }

    public ICollection<FatePointTransaction> FatePointTransactions { get; set; }
}