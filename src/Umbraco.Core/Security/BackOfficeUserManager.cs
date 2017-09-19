﻿using System;
using System.ComponentModel;
using System.Configuration.Provider;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Security;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security.DataProtection;
using Umbraco.Core.Composing;
using Umbraco.Core.Auditing;
using Umbraco.Core.Configuration;
using Umbraco.Core.Configuration.UmbracoSettings;
using Umbraco.Core.Models.Identity;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Services;

namespace Umbraco.Core.Security
{
    /// <summary>
    /// Default back office user manager
    /// </summary>
    public class BackOfficeUserManager : BackOfficeUserManager<BackOfficeIdentityUser>
    {
        public const string OwinMarkerKey = "Umbraco.Web.Security.Identity.BackOfficeUserManagerMarker";

        public BackOfficeUserManager(IUserStore<BackOfficeIdentityUser, int> store)
            : base(store)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use the constructor specifying all dependencies instead")]
        public BackOfficeUserManager(
            IUserStore<BackOfficeIdentityUser, int> store,
            IdentityFactoryOptions<BackOfficeUserManager> options,
            MembershipProviderBase membershipProvider)
            : this(store, options, membershipProvider, UmbracoConfig.For.UmbracoSettings().Content)
        {
        }

        public BackOfficeUserManager(
            IUserStore<BackOfficeIdentityUser, int> store,
            IdentityFactoryOptions<BackOfficeUserManager> options,
            MembershipProviderBase membershipProvider,
            IContentSection contentSectionConfig)
            : base(store)
        {
            if (options == null) throw new ArgumentNullException("options");
            InitUserManager(this, membershipProvider, contentSectionConfig, options);
        }

        #region Static Create methods

        /// <summary>
        /// Creates a BackOfficeUserManager instance with all default options and the default BackOfficeUserManager
        /// </summary>
        /// <param name="options"></param>
        /// <param name="userService"></param>
        /// <param name="memberTypeService"></param>
        /// <param name="entityService"></param>
        /// <param name="externalLoginService"></param>
        /// <param name="membershipProvider"></param>
        /// <param name="contentSectionConfig"></param>
        /// <returns></returns>
        public static BackOfficeUserManager Create(
            IdentityFactoryOptions<BackOfficeUserManager> options,
            IUserService userService,
            IMemberTypeService memberTypeService,
            IEntityService entityService,
            IExternalLoginService externalLoginService,
            MembershipProviderBase membershipProvider,
            IContentSection contentSectionConfig)
        {
            if (options == null) throw new ArgumentNullException("options");
            if (userService == null) throw new ArgumentNullException("userService");
            if (memberTypeService == null) throw new ArgumentNullException("memberTypeService");
            if (externalLoginService == null) throw new ArgumentNullException("externalLoginService");

            var manager = new BackOfficeUserManager(new BackOfficeUserStore(userService, memberTypeService, entityService, externalLoginService, membershipProvider));
            manager.InitUserManager(manager, membershipProvider, contentSectionConfig, options);
            return manager;
        }
        
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use the overload specifying all dependencies instead")]
        public static BackOfficeUserManager Create(
           IdentityFactoryOptions<BackOfficeUserManager> options,
           BackOfficeUserStore customUserStore,
           MembershipProviderBase membershipProvider)
        {
            var manager = new BackOfficeUserManager(customUserStore, options, membershipProvider);
            return manager;
        }

        /// <summary>
        /// Creates a BackOfficeUserManager instance with all default options and a custom BackOfficeUserManager instance
        /// </summary>
        /// <param name="options"></param>
        /// <param name="customUserStore"></param>
        /// <param name="membershipProvider"></param>
        /// <param name="contentSectionConfig"></param>
        /// <returns></returns>
        public static BackOfficeUserManager Create(
            IdentityFactoryOptions<BackOfficeUserManager> options,
            BackOfficeUserStore customUserStore,
            MembershipProviderBase membershipProvider,
            IContentSection contentSectionConfig)
        {
            var manager = new BackOfficeUserManager(customUserStore, options, membershipProvider, contentSectionConfig);
            return manager;
        }
        #endregion

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use the overload specifying all dependencies instead")]
        protected void InitUserManager(
            BackOfficeUserManager manager,
            MembershipProviderBase membershipProvider,         
            IdentityFactoryOptions<BackOfficeUserManager> options)
        {
            InitUserManager(manager, membershipProvider, UmbracoConfig.For.UmbracoSettings().Content, options);
        }

        /// <summary>
        /// Initializes the user manager with the correct options
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="membershipProvider"></param>
        /// <param name="contentSectionConfig"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        protected void InitUserManager(
            BackOfficeUserManager manager,
            MembershipProviderBase membershipProvider,
            IContentSection contentSectionConfig,
            IdentityFactoryOptions<BackOfficeUserManager> options)
        {
            //NOTE: This method is mostly here for backwards compat
            base.InitUserManager(manager, membershipProvider, options.DataProtectionProvider, contentSectionConfig);
        }
    }

    /// <summary>
    /// Generic Back office user manager
    /// </summary>
    public class BackOfficeUserManager<T> : UserManager<T, int>
        where T : BackOfficeIdentityUser
    {
        public BackOfficeUserManager(IUserStore<T, int> store)
            : base(store)
        {
        }


        #region What we support do not currently

        //NOTE: Not sure if we really want/need to ever support this
        public override bool SupportsUserClaim
        {
            get { return false; }
        }

        //TODO: Support this
        public override bool SupportsQueryableUsers
        {
            get { return false; }
        }

        /// <summary>
        /// Developers will need to override this to support custom 2 factor auth
        /// </summary>
        public override bool SupportsUserTwoFactor
        {
            get { return false; }
        }

        //TODO: Support this
        public override bool SupportsUserPhoneNumber
        {
            get { return false; }
        }
        #endregion

        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use the overload specifying all dependencies instead")]
        protected void InitUserManager(
            BackOfficeUserManager<T> manager,
            MembershipProviderBase membershipProvider,
            IDataProtectionProvider dataProtectionProvider)
        {
            InitUserManager(manager, membershipProvider, dataProtectionProvider, UmbracoConfig.For.UmbracoSettings().Content);
        }

        /// <summary>
        /// Initializes the user manager with the correct options
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="membershipProvider">
        /// The <see cref="MembershipProviderBase"/> for the users called UsersMembershipProvider
        /// </param>
        /// <param name="dataProtectionProvider"></param>
        /// <param name="contentSectionConfig"></param>
        /// <returns></returns>
        protected void InitUserManager(
            BackOfficeUserManager<T> manager,
            MembershipProviderBase membershipProvider,
            IDataProtectionProvider dataProtectionProvider,
            IContentSection contentSectionConfig)
        {
            // Configure validation logic for usernames
            manager.UserValidator = new BackOfficeUserValidator<T>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };

            // Configure validation logic for passwords
            manager.PasswordValidator = new MembershipProviderPasswordValidator(membershipProvider);

            //use a custom hasher based on our membership provider
            manager.PasswordHasher = GetDefaultPasswordHasher(membershipProvider);

            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider = new DataProtectorTokenProvider<T, int>(dataProtectionProvider.Create("ASP.NET Identity"));
            }

            manager.UserLockoutEnabledByDefault = true;
            manager.MaxFailedAccessAttemptsBeforeLockout = membershipProvider.MaxInvalidPasswordAttempts;
            //NOTE: This just needs to be in the future, we currently don't support a lockout timespan, it's either they are locked
            // or they are not locked, but this determines what is set on the account lockout date which corresponds to whether they are
            // locked out or not.
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromDays(30);

            //custom identity factory for creating the identity object for which we auth against in the back office
            manager.ClaimsIdentityFactory = new BackOfficeClaimsIdentityFactory<T>();

            manager.EmailService = new EmailService(
                contentSectionConfig.NotificationEmailAddress,
                new EmailSender());

            //NOTE: Not implementing these, if people need custom 2 factor auth, they'll need to implement their own UserStore to suport it

            //// Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
            //// You can write your own provider and plug in here.
            //manager.RegisterTwoFactorProvider("PhoneCode", new PhoneNumberTokenProvider<ApplicationUser>
            //{
            //    MessageFormat = "Your security code is: {0}"
            //});
            //manager.RegisterTwoFactorProvider("EmailCode", new EmailTokenProvider<ApplicationUser>
            //{
            //    Subject = "Security Code",
            //    BodyFormat = "Your security code is: {0}"
            //});

            //manager.SmsService = new SmsService();
        }

        /// <summary>
        /// This will determine which password hasher to use based on what is defined in config
        /// </summary>
        /// <returns></returns>
        protected virtual IPasswordHasher GetDefaultPasswordHasher(MembershipProviderBase provider)
        {
            //if the current user membership provider is unkown (this would be rare), then return the default password hasher
            if (provider.IsUmbracoUsersProvider() == false)
                return new PasswordHasher();

            //if the configured provider has legacy features enabled, then return the membership provider password hasher
            if (provider.AllowManuallyChangingPassword || provider.DefaultUseLegacyEncoding)
                return new MembershipProviderPasswordHasher(provider);

            //we can use the user aware password hasher (which will be the default and preferred way)
            return new UserAwareMembershipProviderPasswordHasher(provider);
        }

        /// <summary>
        /// Gets/sets the default back office user password checker
        /// </summary>
        public IBackOfficeUserPasswordChecker BackOfficeUserPasswordChecker { get; set; }

        /// <summary>
        /// Helper method to generate a password for a user based on the current password validator
        /// </summary>
        /// <returns></returns>
        public string GeneratePassword()
        {
            var passwordValidator = PasswordValidator as PasswordValidator;

            if (passwordValidator == null)
            {
                var membershipPasswordHasher = PasswordHasher as IMembershipProviderPasswordHasher;

                //get the real password validator, this should not be null but in some very rare cases it could be, in which case
                //we need to create a default password validator to use since we have no idea what it actually is or what it's rules are
                //this is an Edge Case!
                passwordValidator = PasswordValidator as PasswordValidator
                                    ?? (membershipPasswordHasher != null
                                        ? new MembershipProviderPasswordValidator(membershipPasswordHasher.MembershipProvider)
                                        : new PasswordValidator());
            }

            var password = Membership.GeneratePassword(
                passwordValidator.RequiredLength,
                passwordValidator.RequireNonLetterOrDigit ? 2 : 0);

            var random = new Random();

            var passwordChars = password.ToCharArray();

            if (passwordValidator.RequireDigit && passwordChars.ContainsAny(Enumerable.Range(48, 58).Select(x => (char)x)))
                password += Convert.ToChar(random.Next(48, 58));  // 0-9

            if (passwordValidator.RequireLowercase && passwordChars.ContainsAny(Enumerable.Range(97, 123).Select(x => (char)x)))
                password += Convert.ToChar(random.Next(97, 123));  // a-z

            if (passwordValidator.RequireUppercase && passwordChars.ContainsAny(Enumerable.Range(65, 91).Select(x => (char)x)))
                password += Convert.ToChar(random.Next(65, 91));  // A-Z

            if (passwordValidator.RequireNonLetterOrDigit && passwordChars.ContainsAny(Enumerable.Range(33, 48).Select(x => (char)x)))
                password += Convert.ToChar(random.Next(33, 48));  // symbols !"#$%&'()*+,-./

            return password;
        }

        /// <summary>
        /// Override to check the user approval value as well as the user lock out date, by default this only checks the user's locked out date
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <remarks>
        /// In the ASP.NET Identity world, there is only one value for being locked out, in Umbraco we have 2 so when checking this for Umbraco we need to check both values
        /// </remarks>
        public override async Task<bool> IsLockedOutAsync(int userId)
        {
            var user = await FindByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("No user found by id " + userId);
            if (user.IsApproved == false)
                return true;

            return await base.IsLockedOutAsync(userId);
        }

        #region Overrides for password logic
        
        /// <summary>
        /// Logic used to validate a username and password
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <remarks>
        /// By default this uses the standard ASP.Net Identity approach which is:
        /// * Get password store
        /// * Call VerifyPasswordAsync with the password store + user + password
        /// * Uses the PasswordHasher.VerifyHashedPassword to compare the stored password
        ///
        /// In some cases people want simple custom control over the username/password check, for simplicity
        /// sake, developers would like the users to simply validate against an LDAP directory but the user
        /// data remains stored inside of Umbraco.
        /// See: http://issues.umbraco.org/issue/U4-7032 for the use cases.
        ///
        /// We've allowed this check to be overridden with a simple callback so that developers don't actually
        /// have to implement/override this class.
        /// </remarks>
        public override async Task<bool> CheckPasswordAsync(T user, string password)
        {
            if (BackOfficeUserPasswordChecker != null)
            {
                var result = await BackOfficeUserPasswordChecker.CheckPasswordAsync(user, password);

                if (user.HasIdentity == false)
                {
                    return false;
                }

                //if the result indicates to not fallback to the default, then return true if the credentials are valid
                if (result != BackOfficeUserPasswordCheckerResult.FallbackToDefaultChecker)
                {
                    return result == BackOfficeUserPasswordCheckerResult.ValidCredentials;
                }
            }

            //we cannot proceed if the user passed in does not have an identity
            if (user.HasIdentity == false)
                return false;

            //use the default behavior
            return await base.CheckPasswordAsync(user, password);
        }

        public override Task<IdentityResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            var result = base.ChangePasswordAsync(userId, currentPassword, newPassword);
            if (result.Result.Succeeded)
                RaisePasswordChangedEvent(userId);
            return result;
        }

        /// <summary>
        /// Override to determine how to hash the password
        /// </summary>
        /// <param name="store"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        protected override async Task<bool> VerifyPasswordAsync(IUserPasswordStore<T, int> store, T user, string password)
        {
            var userAwarePasswordHasher = PasswordHasher as IUserAwarePasswordHasher<BackOfficeIdentityUser, int>;
            if (userAwarePasswordHasher == null)
                return await base.VerifyPasswordAsync(store, user, password);

            var hash = await store.GetPasswordHashAsync(user);
            return userAwarePasswordHasher.VerifyHashedPassword(user, hash, password) != PasswordVerificationResult.Failed;
        }

        /// <summary>
        /// Override to determine how to hash the password
        /// </summary>
        /// <param name="passwordStore"></param>
        /// <param name="user"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method is called anytime the password needs to be hashed for storage (i.e. including when reset password is used)
        /// </remarks>
        protected override async Task<IdentityResult> UpdatePassword(IUserPasswordStore<T, int> passwordStore, T user, string newPassword)
        {
            var userAwarePasswordHasher = PasswordHasher as IUserAwarePasswordHasher<BackOfficeIdentityUser, int>;
            if (userAwarePasswordHasher == null)
                return await base.UpdatePassword(passwordStore, user, newPassword);

            var result = await PasswordValidator.ValidateAsync(newPassword);
            if (result.Succeeded == false)
                return result;

            await passwordStore.SetPasswordHashAsync(user, userAwarePasswordHasher.HashPassword(user, newPassword));
            await UpdateSecurityStampInternal(user);
            return IdentityResult.Success;

            
        }

        /// <summary>
        /// This is copied from the underlying .NET base class since they decied to not expose it
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private async Task UpdateSecurityStampInternal(BackOfficeIdentityUser user)
        {
            if (SupportsUserSecurityStamp == false)
                return;
            await GetSecurityStore().SetSecurityStampAsync(user, NewSecurityStamp());
        }

        /// <summary>
        /// This is copied from the underlying .NET base class since they decied to not expose it
        /// </summary>
        /// <returns></returns>
        private IUserSecurityStampStore<BackOfficeIdentityUser, int> GetSecurityStore()
        {
            var store = Store as IUserSecurityStampStore<BackOfficeIdentityUser, int>;
            if (store == null)
                throw new NotSupportedException("The current user store does not implement " + typeof(IUserSecurityStampStore<>));
            return store;
        }

        /// <summary>
        /// This is copied from the underlying .NET base class since they decied to not expose it
        /// </summary>
        /// <returns></returns>
        private static string NewSecurityStamp()
        {
            return Guid.NewGuid().ToString();
        }

        #endregion

        public override Task<IdentityResult> SetLockoutEndDateAsync(int userId, DateTimeOffset lockoutEnd)
        {
            var result = base.SetLockoutEndDateAsync(userId, lockoutEnd);

            // The way we unlock is by setting the lockoutEnd date to the current datetime
            if (result.Result.Succeeded && lockoutEnd >= DateTimeOffset.UtcNow)
                RaiseAccountLockedEvent(userId);
            else
                RaiseAccountUnlockedEvent(userId);

            return result;
        }

        public override async Task<IdentityResult> ResetAccessFailedCountAsync(int userId)
        {
            var lockoutStore = (IUserLockoutStore<BackOfficeIdentityUser, int>)Store;
            var user = await FindByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("No user found by user id " + userId);

            var accessFailedCount = await GetAccessFailedCountAsync(user.Id);

            if (accessFailedCount == 0)
                return IdentityResult.Success;

            await lockoutStore.ResetAccessFailedCountAsync(user);
            //raise the event now that it's reset
            RaiseResetAccessFailedCountEvent(userId);
            return await UpdateAsync(user);
        }
        

      

        public override Task<IdentityResult> AccessFailedAsync(int userId)
        {
            var result = base.AccessFailedAsync(userId);

            //Slightly confusing: this will return a Success if we successfully update the AccessFailed count
            if (result.Result.Succeeded)
                RaiseLoginFailedEvent(userId);

            return result;
        }

        internal void RaiseAccountLockedEvent(int userId)
        {
            OnAccountLocked(new IdentityAuditEventArgs(AuditEvent.AccountLocked, GetCurrentRequestIpAddress(), userId));
        }

        internal void RaiseAccountUnlockedEvent(int userId)
        {
            OnAccountUnlocked(new IdentityAuditEventArgs(AuditEvent.AccountUnlocked, GetCurrentRequestIpAddress(), userId));
        }

        internal void RaiseForgotPasswordRequestedEvent(int userId)
        {
            OnForgotPasswordRequested(new IdentityAuditEventArgs(AuditEvent.ForgotPasswordRequested, GetCurrentRequestIpAddress(), userId));
        }

        internal void RaiseForgotPasswordChangedSuccessEvent(int userId)
        {
            OnForgotPasswordChangedSuccess(new IdentityAuditEventArgs(AuditEvent.ForgotPasswordChangedSuccess, GetCurrentRequestIpAddress(), userId));
        }

        internal void RaiseLoginFailedEvent(int userId)
        {
            OnLoginFailed(new IdentityAuditEventArgs(AuditEvent.LoginFailed, GetCurrentRequestIpAddress(), userId));
        }

        internal void RaiseInvalidLoginAttemptEvent(string username)
        {
            OnLoginFailed(new IdentityAuditEventArgs(AuditEvent.LoginFailed, GetCurrentRequestIpAddress(), username, string.Format("Attempted login for username '{0}' failed", username)));
        }

        internal void RaiseLoginRequiresVerificationEvent(int userId)
        {
            OnLoginRequiresVerification(new IdentityAuditEventArgs(AuditEvent.LoginRequiresVerification, GetCurrentRequestIpAddress(), userId));
        }

        internal void RaiseLoginSuccessEvent(int userId)
        {
            OnLoginSuccess(new IdentityAuditEventArgs(AuditEvent.LoginSucces, GetCurrentRequestIpAddress(), userId));
        }

        internal void RaiseLogoutSuccessEvent(int userId)
        {
            OnLogoutSuccess(new IdentityAuditEventArgs(AuditEvent.LogoutSuccess, GetCurrentRequestIpAddress(), userId));
        }

        internal void RaisePasswordChangedEvent(int userId)
        {
            OnPasswordChanged(new IdentityAuditEventArgs(AuditEvent.PasswordChanged, GetCurrentRequestIpAddress(), userId));
        }

        internal void RaisePasswordResetEvent(int userId)
        {
            OnPasswordReset(new IdentityAuditEventArgs(AuditEvent.PasswordReset, GetCurrentRequestIpAddress(), userId));
        }
        internal void RaiseResetAccessFailedCountEvent(int userId)
        {
            OnResetAccessFailedCount(new IdentityAuditEventArgs(AuditEvent.ResetAccessFailedCount, GetCurrentRequestIpAddress(), userId));
        }

        public static event EventHandler AccountLocked;
        public static event EventHandler AccountUnlocked;
        public static event EventHandler ForgotPasswordRequested;
        public static event EventHandler ForgotPasswordChangedSuccess;
        public static event EventHandler LoginFailed;
        public static event EventHandler LoginRequiresVerification;
        public static event EventHandler LoginSuccess;
        public static event EventHandler LogoutSuccess;
        public static event EventHandler PasswordChanged;
        public static event EventHandler PasswordReset;
        public static event EventHandler ResetAccessFailedCount;

        protected virtual void OnAccountLocked(IdentityAuditEventArgs e)
        {
            if (AccountLocked != null) AccountLocked(this, e);
        }

        protected virtual void OnAccountUnlocked(IdentityAuditEventArgs e)
        {
            if (AccountUnlocked != null) AccountUnlocked(this, e);
        }

        protected virtual void OnForgotPasswordRequested(IdentityAuditEventArgs e)
        {
            if (ForgotPasswordRequested != null) ForgotPasswordRequested(this, e);
        }

        protected virtual void OnForgotPasswordChangedSuccess(IdentityAuditEventArgs e)
        {
            if (ForgotPasswordChangedSuccess != null) ForgotPasswordChangedSuccess(this, e);
        }

        protected virtual void OnLoginFailed(IdentityAuditEventArgs e)
        {
            if (LoginFailed != null) LoginFailed(this, e);
        }

        protected virtual void OnLoginRequiresVerification(IdentityAuditEventArgs e)
        {
            if (LoginRequiresVerification != null) LoginRequiresVerification(this, e);
        }

        protected virtual void OnLoginSuccess(IdentityAuditEventArgs e)
        {
            if (LoginSuccess != null) LoginSuccess(this, e);
        }

        protected virtual void OnLogoutSuccess(IdentityAuditEventArgs e)
        {
            if (LogoutSuccess != null) LogoutSuccess(this, e);
        }

        protected virtual void OnPasswordChanged(IdentityAuditEventArgs e)
        {
            if (PasswordChanged != null) PasswordChanged(this, e);
        }

        protected virtual void OnPasswordReset(IdentityAuditEventArgs e)
        {
            if (PasswordReset != null) PasswordReset(this, e);
        }

        protected virtual void OnResetAccessFailedCount(IdentityAuditEventArgs e)
        {
            if (ResetAccessFailedCount != null) ResetAccessFailedCount(this, e);
        }

        /// <summary>
        /// Returns the current request IP address for logging if there is one
        /// </summary>
        /// <returns></returns>
        protected virtual string GetCurrentRequestIpAddress()
        {
            //TODO: inject a service to get this value, we should not be relying on the old HttpContext.Current especially in the ASP.NET Identity world.
            var httpContext = HttpContext.Current == null ? (HttpContextBase)null : new HttpContextWrapper(HttpContext.Current);
            return httpContext.GetCurrentRequestIpAddress();
        }
    }
}
