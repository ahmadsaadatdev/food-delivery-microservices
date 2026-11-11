using FoodDelivery.BuildingBlocks.Domain;

namespace FoodDelivery.Services.Customers.Api.Domain;

public sealed class Customer : Entity<Guid>
{
    public Guid IdentityId { get; private set; }
    public string Email { get; private set; } = default!;
    public string FirstName { get; private set; } = default!;
    public string LastName { get; private set; } = default!;
    public string? Phone { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Customer()
    {
    }

    public static Customer CreateFromIdentity(Guid identityId, string email, string firstName, string lastName)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            IdentityId = identityId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void UpdateContact(string? phone) => Phone = phone;
}
