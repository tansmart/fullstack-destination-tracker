using Microsoft.AspNetCore.Identity;

namespace backend.Models;

/*
    ApplicationUser extends the IdentityUser class to include additional
    properties specific to the application's user requirements.
*/
public class ApplicationUser : IdentityUser
{
    // Optional: add fields like DisplayName, Country, etc.
}
