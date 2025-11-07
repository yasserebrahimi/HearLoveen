using FluentAssertions;
using HearLoveen.Domain.Entities;
using Xunit;

namespace HearLoveen.Tests.Domain;

public class UserTests
{
    [Fact]
    public void User_ShouldBeCreated_WithValidData()
    {
        // Arrange
        var id = Guid.NewGuid();
        var email = "test@example.com";
        var name = "Test User";
        var role = "Parent";

        // Act
        var user = new User
        {
            Id = id,
            Email = email,
            Name = name,
            Role = role
        };

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().Be(id);
        user.Email.Should().Be(email);
        user.Name.Should().Be(name);
        user.Role.Should().Be(role);
    }

    [Theory]
    [InlineData("Parent")]
    [InlineData("Therapist")]
    [InlineData("Admin")]
    public void User_ShouldAccept_ValidRoles(string role)
    {
        // Arrange & Act
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            Name = "Test",
            Role = role
        };

        // Assert
        user.Role.Should().Be(role);
    }

    [Fact]
    public void User_ShouldHave_UniqueId()
    {
        // Arrange & Act
        var user1 = new User { Id = Guid.NewGuid(), Email = "user1@test.com", Name = "User 1", Role = "Parent" };
        var user2 = new User { Id = Guid.NewGuid(), Email = "user2@test.com", Name = "User 2", Role = "Parent" };

        // Assert
        user1.Id.Should().NotBe(user2.Id);
    }
}
