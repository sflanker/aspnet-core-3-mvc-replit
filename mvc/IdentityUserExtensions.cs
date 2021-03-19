using Microsoft.AspNetCore.Identity;

namespace MvcSample {
  public static class IdentityUserExtensions {
    public static IdentityUser Clone(this IdentityUser user) {
      return new IdentityUser {
        Id = user.Id,
        UserName = user.UserName,
        NormalizedUserName = user.NormalizedUserName,
        Email = user.Email,
        NormalizedEmail = user.NormalizedEmail,
        EmailConfirmed = user.EmailConfirmed,
        PasswordHash = user.PasswordHash,
        SecurityStamp = user.SecurityStamp,
        ConcurrencyStamp = user.ConcurrencyStamp,
        PhoneNumber = user.PhoneNumber,
        PhoneNumberConfirmed = user.PhoneNumberConfirmed,
        TwoFactorEnabled = user.TwoFactorEnabled,
        LockoutEnabled = user.LockoutEnabled,
        AccessFailedCount = user.AccessFailedCount,
      };
    }
  }
}
