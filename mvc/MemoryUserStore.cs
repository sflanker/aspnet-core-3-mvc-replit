namespace MvcSample {
  public sealed class MemoryUserStore :
    IUserStore<IdentityUser>,
    IUserClaimStore<IdentityUser>,
    IUserLoginStore<IdentityUser>,
    IUserRoleStore<IdentityUser>,
    IUserPasswordStore<IdentityUser>,
    IUserSecurityStampStore<IdentityUser> {
    
    public static MemoryUserStore Instance { get; }
    // TODO
  }
}