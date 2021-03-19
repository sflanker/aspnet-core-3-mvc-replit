using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace MvcSample {
  public sealed class MemoryUserStore :
    IUserStore<IdentityUser>,
    IUserClaimStore<IdentityUser>,
    IUserLoginStore<IdentityUser>,
    IUserRoleStore<IdentityUser>,
    IUserPasswordStore<IdentityUser>,
    IUserSecurityStampStore<IdentityUser>,
    IUserEmailStore<IdentityUser>,
    IUserPhoneNumberStore<IdentityUser>,
    IUserAuthenticatorKeyStore<IdentityUser>,
    IUserTwoFactorStore<IdentityUser>,
    IUserTwoFactorRecoveryCodeStore<IdentityUser>,
    IUserLockoutStore<IdentityUser> {

    public static MemoryUserStore Instance { get; } = new MemoryUserStore();

    private static readonly IImmutableSet<String> EmptyRoleSet = ImmutableHashSet.Create<String>(StringComparer.OrdinalIgnoreCase);

    private readonly Object sync = new Object();

    private readonly ConcurrentDictionary<String, IdentityUser> users =
      new ConcurrentDictionary<String, IdentityUser>(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<String, String> userIdToUserNameLookup =
      new ConcurrentDictionary<String, String>();

    private readonly ConcurrentDictionary<String, (IReadOnlyList<Claim> Claims, IImmutableSet<String> Roles, IReadOnlyList<UserLoginInfo> Logins)> userDetails =
      new ConcurrentDictionary<String, (IReadOnlyList<Claim> Claims, IImmutableSet<String> Roles, IReadOnlyList<UserLoginInfo> Logins)>();

    private readonly ConcurrentDictionary<String, String> authenticatorKeyStore = new ConcurrentDictionary<String, String>();

    private readonly ConcurrentDictionary<String, String[]> recoveryCodeStore = new ConcurrentDictionary<String, String[]>();

    public Task<IdentityResult> CreateAsync(IdentityUser user, CancellationToken cancellationToken) {
      user.Id = $"{Guid.NewGuid()}";
      var clone = user.Clone();
      var result = this.users.TryAdd(clone.NormalizedUserName, clone);

      if (result) {
        var tmp = userIdToUserNameLookup.TryAdd(clone.Id, clone.NormalizedUserName);
        Debug.Assert(tmp, "User.Id collision.");
        return Task.FromResult(IdentityResult.Success);
      } else {
        return Task.FromResult(
          IdentityResult.Failed(new IdentityError {
            Code = ErrorCodes.UserAlreadyExists,
            Description =
              $"A user with the specified {nameof(IdentityUser.UserName)} ({user.UserName}) already exists."
          })
        );
      }
    }

    public Task<IdentityResult> DeleteAsync(IdentityUser user, CancellationToken cancellationToken) {
      var result = this.users.TryRemove(user.UserName, out var removed);

      if (result) {
        userIdToUserNameLookup.TryRemove(removed.Id, out _);
        return Task.FromResult(IdentityResult.Success);
      } else {
        return Task.FromResult(
          IdentityResult.Failed(new IdentityError {
            Code = ErrorCodes.UserDoesNotExist,
            Description =
              $"A user with the specified {nameof(IdentityUser.UserName)} ({user.UserName}) does not exist."
          })
        );
      }
    }

    public Task<IdentityUser> FindByIdAsync(String userId, CancellationToken cancellationToken) {
      if (this.userIdToUserNameLookup.TryGetValue(userId, out var username) &&
        this.users.TryGetValue(username, out var user)) {

        return Task.FromResult(user.Clone());
      } else {
        return Task.FromResult<IdentityUser>(null);
      }
    }

    public Task<IdentityUser> FindByNameAsync(String normalizedUserName, CancellationToken cancellationToken) {
      return Task.FromResult(
        this.users.TryGetValue(normalizedUserName, out var user) ? user.Clone() : null
      );
    }

    public Task<String> GetNormalizedUserNameAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(user.UserName);
    }

    public Task<String> GetUserIdAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(user.Id);
    }

    public Task<String> GetUserNameAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(user.UserName);
    }

    public Task SetNormalizedUserNameAsync(IdentityUser user, String normalizedName, CancellationToken cancellationToken) {
      user.NormalizedUserName = normalizedName;
      return Task.CompletedTask;
    }

    public Task SetUserNameAsync(IdentityUser user, String userName, CancellationToken cancellationToken) {
      user.UserName = userName;
      return Task.CompletedTask;
    }

    public Task<IdentityResult> UpdateAsync(IdentityUser user, CancellationToken cancellationToken) {
      lock (sync) {
        if (this.userIdToUserNameLookup.TryGetValue(user.Id, out var originalUserName) &&
          this.users.TryGetValue(originalUserName, out var existingUser)) {

          if (user.NormalizedUserName != originalUserName) {
            if (this.users.TryAdd(user.NormalizedUserName, user.Clone())) {
              // Remove the old and update the index
              this.users.TryRemove(originalUserName, out _);
              var tmp = this.userIdToUserNameLookup.TryUpdate(user.Id, user.NormalizedUserName, originalUserName);
              Debug.Assert(tmp);

              return Task.FromResult(IdentityResult.Success);
            } else {
              return Task.FromResult(
                IdentityResult.Failed(new IdentityError {
                  Code = ErrorCodes.UserAlreadyExists,
                  Description =
                    $"A user with the specified {nameof(IdentityUser.UserName)} ({user.UserName}) already exists."
                })
              );
            }
          } else {
            var tmp = this.users.TryUpdate(user.NormalizedUserName, user.Clone(), existingUser);
            Debug.Assert(tmp);
            return Task.FromResult(IdentityResult.Success);
          }
        } else {
          return Task.FromResult(
            IdentityResult.Failed(new IdentityError {
              Code = ErrorCodes.UserDoesNotExist,
              Description =
                $"A user with the specified {nameof(IdentityUser.UserName)} ({user.UserName}) does not exist."
            })
          );
        }
      }
    }

    public Task AddClaimsAsync(IdentityUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken) {
      this.userDetails.AddOrUpdate(
        user.Id,
        _ => (claims.ToArray(), EmptyRoleSet, new UserLoginInfo[0]),
        (_, details) => (details.Claims.Concat(claims).ToArray(), details.Roles, details.Logins)
      );

      return Task.CompletedTask;
    }

    public Task<IList<Claim>> GetClaimsAsync(IdentityUser user, CancellationToken cancellationToken) {
      if (this.userDetails.TryGetValue(user.Id, out var details)) {
        return Task.FromResult<IList<Claim>>(details.Claims.ToList());
      } else {
        return Task.FromResult<IList<Claim>>(new List<Claim>());
      }
    }

    public Task<IList<IdentityUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken) {
      return Task.FromResult<IList<IdentityUser>>(this.EnumerateUsersWithClaim(claim).ToList());
    }

    public Task RemoveClaimsAsync(IdentityUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken) {
      this.userDetails.AddOrUpdate(
        user.Id,
        _ => (new Claim[0], EmptyRoleSet, new UserLoginInfo[0]),
        (_, details) => (RemoveClaims(details.Claims, claims).ToArray(), details.Roles, details.Logins)
      );

      return Task.CompletedTask;
    }

    public Task ReplaceClaimAsync(IdentityUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken) {
      this.userDetails.AddOrUpdate(
        user.Id,
        _ => (new[] { claim }, EmptyRoleSet, new UserLoginInfo[0]),
        (_, details) => (ReplaceClaim(details.Claims, claim, newClaim).ToArray(), details.Roles, details.Logins)
      );

      return Task.CompletedTask;
    }

    public Task AddLoginAsync(IdentityUser user, UserLoginInfo login, CancellationToken cancellationToken) {
      this.userDetails.AddOrUpdate(
        user.Id,
        _ => (new Claim[0], EmptyRoleSet, new [] { login }),
        (_, details) => (details.Claims, details.Roles, details.Logins.Append(login).ToArray())
      );

      return Task.CompletedTask;
    }

    public Task<IdentityUser> FindByLoginAsync(String loginProvider, String providerKey, CancellationToken cancellationToken) {
      return Task.FromResult(this.EnumerateUsersWithLogin(loginProvider, providerKey).FirstOrDefault());
    }

    public Task<IList<UserLoginInfo>> GetLoginsAsync(IdentityUser user, CancellationToken cancellationToken) {
      if (this.userDetails.TryGetValue(user.Id, out var details)) {
        return Task.FromResult<IList<UserLoginInfo>>(details.Logins.ToList());
      } else {
        return Task.FromResult<IList<UserLoginInfo>>(new List<UserLoginInfo>());
      }
    }

    public Task RemoveLoginAsync(IdentityUser user, String loginProvider, String providerKey, CancellationToken cancellationToken) {
      this.userDetails.AddOrUpdate(
        user.Id,
        _ => (new Claim[0], ImmutableHashSet<String>.Empty, new UserLoginInfo[0]),
        (_, details) => (details.Claims, details.Roles, RemoveLogins(details.Logins, loginProvider, providerKey).ToArray())
      );

      return Task.CompletedTask;
    }

    public Task AddToRoleAsync(IdentityUser user, String roleName, CancellationToken cancellationToken) {
      this.userDetails.AddOrUpdate(
        user.Id,
        _ => (new Claim[0], EmptyRoleSet.Add(roleName), new UserLoginInfo[0]),
        (_, details) => (details.Claims, details.Roles.Add(roleName), details.Logins)
      );

      return Task.CompletedTask;
    }

    public Task<IList<String>> GetRolesAsync(IdentityUser user, CancellationToken cancellationToken) {
      if (this.userDetails.TryGetValue(user.Id, out var details)) {
        return Task.FromResult<IList<String>>(details.Roles.ToList());
      } else {
        return Task.FromResult<IList<String>>(new List<String>());
      }
    }

    public Task<IList<IdentityUser>> GetUsersInRoleAsync(String roleName, CancellationToken cancellationToken) {
      return Task.FromResult<IList<IdentityUser>>(
        users.Values
          .Where(u =>
            this.userDetails.TryGetValue(u.Id, out var details) &&
            details.Roles.Contains(roleName))
          .ToList()
      );
    }

    public Task<Boolean> IsInRoleAsync(IdentityUser user, String roleName, CancellationToken cancellationToken) {
      return Task.FromResult(
        this.userDetails.TryGetValue(user.Id, out var details) &&
        details.Roles.Contains(roleName)
      );
    }

    public Task RemoveFromRoleAsync(IdentityUser user, String roleName, CancellationToken cancellationToken) {
      this.userDetails.AddOrUpdate(
        user.Id,
        _ => (new Claim[0], EmptyRoleSet, new UserLoginInfo[0]),
        (_, details) => (details.Claims, details.Roles.Remove(roleName), details.Logins)
      );

      return Task.CompletedTask;
    }

    public Task<String> GetPasswordHashAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(user.PasswordHash);
    }

    public Task<Boolean> HasPasswordAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(!String.IsNullOrEmpty(user.PasswordHash));
    }

    public Task SetPasswordHashAsync(IdentityUser user, String passwordHash, CancellationToken cancellationToken) {
      user.PasswordHash = passwordHash;
      return Task.CompletedTask;
    }

    public Task<String> GetSecurityStampAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(user.SecurityStamp);
    }

    public Task SetSecurityStampAsync(IdentityUser user, string stamp, CancellationToken cancellationToken) {
      user.SecurityStamp = stamp;
      return Task.CompletedTask;
    }

    public Task SetEmailAsync(IdentityUser user, String email, CancellationToken cancellationToken) {
      user.Email = email;
      return Task.CompletedTask;
    }

    public Task<String> GetEmailAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(user.Email);
    }

    public Task<Boolean> GetEmailConfirmedAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(user.EmailConfirmed);
    }

    public Task SetEmailConfirmedAsync(IdentityUser user, Boolean confirmed, CancellationToken cancellationToken) {
      user.EmailConfirmed = confirmed;
      return Task.CompletedTask;
    }

    public Task<IdentityUser> FindByEmailAsync(String normalizedEmail, CancellationToken cancellationToken) {
      foreach (var user in this.users.Values) {
        if (user.NormalizedEmail == normalizedEmail) {
          return Task.FromResult(user);
        }
      }

      return Task.FromResult<IdentityUser>(null);
    }

    public Task<String> GetNormalizedEmailAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(user.NormalizedEmail);
    }

    public Task SetNormalizedEmailAsync(IdentityUser user, String normalizedEmail, CancellationToken cancellationToken) {
      user.NormalizedEmail = normalizedEmail;
      return Task.CompletedTask;
    }

    public Task SetPhoneNumberAsync(IdentityUser user, String phoneNumber, CancellationToken cancellationToken) {
      user.PhoneNumber = phoneNumber;
      return Task.CompletedTask;
    }

    public Task<String> GetPhoneNumberAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(user.PhoneNumber);
    }

    public Task<Boolean> GetPhoneNumberConfirmedAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(user.PhoneNumberConfirmed);
    }

    public Task SetPhoneNumberConfirmedAsync(IdentityUser user, Boolean confirmed, CancellationToken cancellationToken) {
      user.PhoneNumberConfirmed = confirmed;
      return Task.CompletedTask;
    }

    public Task SetAuthenticatorKeyAsync(IdentityUser user, String key, CancellationToken cancellationToken) {
      this.authenticatorKeyStore.AddOrUpdate(
        user.Id,
        _ => key,
        (_, __) => key
      );

      return Task.CompletedTask;
    }

    public Task<String> GetAuthenticatorKeyAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(this.authenticatorKeyStore.TryGetValue(user.Id, out var key) ? key : null);
    }

    public Task SetTwoFactorEnabledAsync(IdentityUser user, Boolean enabled, CancellationToken cancellationToken) {
      user.TwoFactorEnabled = enabled;
      return Task.CompletedTask;
    }

    public Task<Boolean> GetTwoFactorEnabledAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(user.TwoFactorEnabled);
    }

    public Task ReplaceCodesAsync(IdentityUser user, IEnumerable<String> recoveryCodes, CancellationToken cancellationToken) {
      this.recoveryCodeStore.AddOrUpdate(
        user.Id,
        _ => recoveryCodes.ToArray(),
        (_, __) => recoveryCodes.ToArray()
      );
      return Task.CompletedTask;
    }

    public Task<Boolean> RedeemCodeAsync(IdentityUser user, String code, CancellationToken cancellationToken) {
      return Task.FromResult(
        this.recoveryCodeStore.TryGetValue(user.Id, out var recoveryCodes) &&
          recoveryCodes.Contains(code)
      );
    }

    public Task<Int32> CountCodesAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(
        this.recoveryCodeStore.TryGetValue(user.Id, out var recoveryCodes) ?
          recoveryCodes.Length :
          0
      );
    }

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(user.LockoutEnd);
    }

    public Task SetLockoutEndDateAsync(IdentityUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken) {
      user.LockoutEnd = lockoutEnd;
      return Task.CompletedTask;
    }

    public Task<Int32> IncrementAccessFailedCountAsync(IdentityUser user, CancellationToken cancellationToken) {
      user.AccessFailedCount += 1;
      return Task.FromResult(user.AccessFailedCount);
    }

    public Task ResetAccessFailedCountAsync(IdentityUser user, CancellationToken cancellationToken) {
      user.AccessFailedCount = 0;
      return Task.CompletedTask;
    }

    public Task<Int32> GetAccessFailedCountAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(user.AccessFailedCount);
    }

    public Task<Boolean> GetLockoutEnabledAsync(IdentityUser user, CancellationToken cancellationToken) {
      return Task.FromResult(user.LockoutEnabled);
    }

    public Task SetLockoutEnabledAsync(IdentityUser user, Boolean enabled, CancellationToken cancellationToken) {
      user.LockoutEnabled = enabled;
      return Task.CompletedTask;
    }

    public void Dispose() {
    }

    public IEnumerable<IdentityUser> EnumerateUsersWithClaim(Claim claim) {
      foreach (var user in this.users.Values) {
        if (this.userDetails.TryGetValue(user.Id, out var details)) {
          // Should we be filtering on anything else? Issuer?
          if (details.Claims.Any(c => c.Type == claim.Type && c.ValueType == claim.ValueType && c.Value == claim.Value)) {
            yield return user;
          }
        }
      }
    }

    private static IEnumerable<Claim> RemoveClaims(IEnumerable<Claim> existingClaims, IEnumerable<Claim> toRemove) {
      var removeList = toRemove.ToList();
      foreach (var claim in existingClaims) {
        if (removeList.All(c => c.Type != claim.Type)) {
          yield return claim;
        }
      }
    }

    private static IEnumerable<Claim> ReplaceClaim(IEnumerable<Claim> claims, Claim oldClaim, Claim newClaim) {
      var found = false;
      foreach (var claim in claims) {
        if (!found && claim.Type == oldClaim.Type) {
          found = true;
          yield return newClaim;
        } else {
          yield return claim;
        }
      }

      if (!found) {
        yield return newClaim;
      }
    }

    private IEnumerable<IdentityUser> EnumerateUsersWithLogin(String loginProvider, String providerKey) {
      foreach (var user in this.users.Values) {
        if (this.userDetails.TryGetValue(user.Id, out var details)) {
          if (details.Logins.Any(l => l.LoginProvider == loginProvider && l.ProviderKey == providerKey)) {
            yield return user;
          }
        }
      }
    }

    private static IEnumerable<UserLoginInfo> RemoveLogins(IEnumerable<UserLoginInfo> logins, String loginProvider, String providerKey) {
      return logins.Where(login => login.LoginProvider != loginProvider || login.ProviderKey != providerKey);
    }

    private static class ErrorCodes {
      public const String UserAlreadyExists = "mvc-sample:error-codes:user-already-exists";
      public const String UserDoesNotExist = "mvc-sample:error-codes:user-does-not-exist";
    }
  }
}
