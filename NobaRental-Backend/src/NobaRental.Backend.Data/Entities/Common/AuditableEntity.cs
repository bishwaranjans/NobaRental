namespace NobaRental.Backend.Data.Entities.Common;

public abstract class AuditableEntity : IAuditableEntity
{
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? ModifiedAt { get; set; }
}
