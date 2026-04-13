using Synquid.Domain.Entities;

namespace Synquid.Application.DTOs;

public class UserResponseDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Role { get; set; }
    public bool IsActive { get; set; }
    public bool EmailVerified { get; set; }
    public Guid? InstitutionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public static UserResponseDto FromUser(User user)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            EmailVerified = user.EmailVerified,
            InstitutionId = user.InstitutionId,
            CreatedAt = user.CreatedAt
        };
    }
}