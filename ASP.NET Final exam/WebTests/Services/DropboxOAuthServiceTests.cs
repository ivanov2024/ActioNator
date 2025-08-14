using System;
using System.Text.RegularExpressions;
using ActioNator.Services.Implementations.Cloud;
using NUnit.Framework;

namespace WebTests.Services
{
    public class DropboxOAuthServiceTests
    {
        [Test]
        public void GeneratePkceCodes_Produces_NonEmpty_And_UrlSafe()
        {
            var svc = new DropboxOAuthService();
            var codes1 = svc.GeneratePkceCodes();
            var codes2 = svc.GeneratePkceCodes();

            Assert.That(codes1.CodeVerifier, Is.Not.Null.And.Not.Empty);
            Assert.That(codes1.CodeChallenge, Is.Not.Null.And.Not.Empty);
            Assert.That(codes2.CodeVerifier, Is.Not.EqualTo(codes1.CodeVerifier));

            var urlSafe = new Regex("^[A-Za-z0-9_-]+$");
            Assert.That(urlSafe.IsMatch(codes1.CodeVerifier), Is.True);
            Assert.That(urlSafe.IsMatch(codes1.CodeChallenge), Is.True);
        }

        [Test]
        public void BuildAuthorizeUri_Composes_Expected_Query()
        {
            var svc = new DropboxOAuthService();
            var uri = svc.BuildAuthorizeUri(
                appKey: "APP",
                redirectUri: "https://localhost/cb",
                state: "xyz",
                codeChallenge: "CC");

            var s = uri.ToString();
            Assert.That(uri.Scheme, Is.EqualTo("https"));
            Assert.That(s, Does.Contain("response_type=code"));
            Assert.That(s, Does.Contain("client_id=APP"));
            Assert.That(s, Does.Contain("redirect_uri=https%3A%2F%2Flocalhost%2Fcb"));
            Assert.That(s, Does.Contain("state=xyz"));
            Assert.That(s, Does.Contain("token_access_type=offline"));
            Assert.That(s, Does.Contain("code_challenge=CC"));
            Assert.That(s, Does.Contain("code_challenge_method=S256"));
            // Scope may be space separated or URL-encoded depending on Uri formatting
            Assert.That(s, Does.Contain("scope="));
            Assert.That(s, Does.Contain("files.content.write"));
            Assert.That(s, Does.Contain("sharing.read"));
            Assert.That(s, Does.Contain("sharing.write"));
        }
    }
}
