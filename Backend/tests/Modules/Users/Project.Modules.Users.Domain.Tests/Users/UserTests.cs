using FluentAssertions;
using Project.Modules.Users.Domain.Users;
using System;
using System.Linq;
using Xunit;

namespace Project.Modules.Users.Domain.Tests.Users;

public class UserTests
{
    [Fact]
    public void Create_Should_CreateUserAndRaiseDomainEvent()
    {
        // Arrange
        var firstName = "John";
        var lastName = "Doe";
        var email = "john.doe@example.com";
        var hashedPassword = "hashed_password";

        // Act
        var user = User.Create(firstName, lastName, email, hashedPassword);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().NotBeEmpty();
        user.FirstName.Should().Be(firstName);
        user.LastName.Should().Be(lastName);
        user.Email.Should().Be(email);
        user.HashedPassword.Should().Be(hashedPassword);
        user.Role.Should().Be(Role.User);
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        // Verify Domain Event
        var domainEvent = user.DomainEvents.SingleOrDefault() as UserCreatedDomainEvent;
        domainEvent.Should().NotBeNull();
        domainEvent.UserId.Should().Be(user.Id);
        domainEvent.Email.Should().Be(email);
        domainEvent.FirstName.Should().Be(firstName);
        domainEvent.LastName.Should().Be(lastName);
        domainEvent.Role.Should().Be(Role.User.ToString());
    }

    [Fact]
    public void Update_Should_UpdateUserPropertiesAndRaiseDomainEvent()
    {
        // Arrange
        var user = User.Create("John", "Doe", "john@example.com", "hash");
        user.ClearDomainEvents(); // Clear creation event to cleanly assert update event

        var newFirstName = "Jane";
        var newLastName = "Smith";
        var newEmail = "jane.smith@example.com";

        // Act
        user.Update(newFirstName, newLastName, newEmail);

        // Assert
        user.FirstName.Should().Be(newFirstName);
        user.LastName.Should().Be(newLastName);
        user.Email.Should().Be(newEmail);

        // Verify Domain Event
        var domainEvent = user.DomainEvents.SingleOrDefault() as UserUpdatedDomainEvent;
        domainEvent.Should().NotBeNull();
        domainEvent.UserId.Should().Be(user.Id);
        domainEvent.FirstName.Should().Be(newFirstName);
        domainEvent.LastName.Should().Be(newLastName);
        domainEvent.Email.Should().Be(newEmail);
        domainEvent.Role.Should().Be(user.Role.ToString());
    }
}
