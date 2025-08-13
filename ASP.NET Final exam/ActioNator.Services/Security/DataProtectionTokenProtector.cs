using ActioNator.Services.Interfaces.Security;
using Microsoft.AspNetCore.DataProtection;

namespace ActioNator.Services.Security
{
    /// <summary>
    /// Token protector backed by ASP.NET Core Data Protection API.
    /// </summary>
    public sealed class DataProtectionTokenProtector : ITokenProtector
    {
        private readonly IDataProtector _protector;

        public DataProtectionTokenProtector(IDataProtectionProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            _protector = provider.CreateProtector("ActioNator.Dropbox.RefreshToken");
        }

        public string Protect(string plaintext)
        {
            if (string.IsNullOrEmpty(plaintext)) throw new ArgumentException("Value cannot be null or empty.", nameof(plaintext));
            return _protector.Protect(plaintext);
        }

        public string Unprotect(string protectedData)
        {
            if (string.IsNullOrEmpty(protectedData)) throw new ArgumentException("Value cannot be null or empty.", nameof(protectedData));
            return _protector.Unprotect(protectedData);
        }
    }
}
