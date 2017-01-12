using System.Security;

namespace Inedo.Extensions
{
#if Otter
    // remove this when BuildMaster SDK is updated to v5.7, and replace all SecureString extension methods with their AH equivalents
    internal static class SecureStringExtensions
    {
        public static string ToUnsecureString(this SecureString thisValue) => AH.Unprotect(thisValue);
        public static SecureString ToSecureString(this string s) => AH.CreateSecureString(s);
    }
#endif
}
