using System.Security.Claims;

namespace RaceDay.Api.Helpers;

public static class ClaimsExtensions
{
    // Reads the user's ID from the "uid" claim we put inside the JWT at login
    public static int GetUserId(this ClaimsPrincipal principal)
        => int.Parse(principal.FindFirst("uid")!.Value);
}