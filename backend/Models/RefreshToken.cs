namespace backend.Models;

/// <summary>
/// Represents a refresh token used for renewing authentication sessions.
/// </summary>
public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;

    /* Navigation Properties */
    public ApplicationUser User { get; set; } = null!;
}
