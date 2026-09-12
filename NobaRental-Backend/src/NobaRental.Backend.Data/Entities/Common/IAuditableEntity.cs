namespace NobaRental.Backend.Data.Entities.Common;

public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }

    DateTimeOffset? ModifiedAt { get; set; }
}
