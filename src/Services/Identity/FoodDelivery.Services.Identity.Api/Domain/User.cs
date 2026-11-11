using FoodDelivery.BuildingBlocks.Domain;

namespace FoodDelivery.Services.Identity.Api.Domain;

public sealed class User : Entity<Guid>
{
    public string Email { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public DateTime CreatedAtUtc { get; private set; }

    private User()
    {
    }

    public static User Register(string email, string passwordHash, string firstName, string lastName)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        user.Raise(new UserRegisteredDomainEvent(user.Id, user.Email, user.FirstName, user.LastName));
        return user;
    }
}

public sealed record UserRegisteredDomainEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName) : DomainEvent;
