using System;

namespace ActioNator.Services.Interfaces.Security
{
    /// <summary>
    /// Provides encryption/decryption for sensitive tokens at rest.
    /// Backed by ASP.NET Core Data Protection.
    /// </summary>
    public interface ITokenProtector
    {
        string Protect(string plaintext);
        string Unprotect(string protectedData);
    }
}
