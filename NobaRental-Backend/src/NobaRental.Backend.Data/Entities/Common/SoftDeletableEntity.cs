namespace NobaRental.Backend.Data.Entities.Common;

public abstract class SoftDeletableEntity : AuditableEntity, ISoftDeletable
{
    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
