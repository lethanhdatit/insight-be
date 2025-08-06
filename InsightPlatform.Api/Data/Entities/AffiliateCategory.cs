using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

public class AffiliateCategory : Trackable
{
    public Guid Id { get; set; }
    
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long AutoId { get; set; }
    
    public string Code { get; set; }
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Localized content stored as JSONB
    // Structure: { "vi": { "name": "...", "description": "..." }, "en": { "name": "...", "description": "..." } }
    public string LocalizedContent { get; set; }
    
    // Navigation properties
    public AffiliateCategory Parent { get; set; }
    public ICollection<AffiliateCategory> Children { get; set; }
    public ICollection<AffiliateProduct> Products { get; set; }
    public ICollection<AffiliateProductCategory> ProductCategories { get; set; }
}
