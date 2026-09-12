namespace NobaRental.Backend.Data.Entities.Common;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }

    DateTimeOffset? DeletedAt { get; set; }
}
