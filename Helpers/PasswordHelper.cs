using System;
using System.Security.Cryptography;

namespace UmbiloRentals.Helpers
{
    /// <summary>
    /// Handles password hashing using PBKDF2 (built into .NET, no extra
    /// package needed). Also transparently supports verifying against
    /// legacy plaintext passwords already stored in the database, so
    /// existing accounts are not locked out - see VerifyPassword.
    /// </summary>
    public static class PasswordHelper
    {
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 100_000;
        private const string Prefix = "PBKDF2$";

        /// <summary>
        /// Produces a new hash in the form:
        /// PBKDF2$iterations$saltBase64$hashBase64
        /// </summary>
        public static string HashPassword(string password)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] salt = new byte[SaltSize];
                rng.GetBytes(salt);

                byte[] hash = ComputeHash(password, salt, Iterations);

                return string.Format(
                    "{0}{1}${2}${3}",
                    Prefix,
                    Iterations,
                    Convert.ToBase64String(salt),
                    Convert.ToBase64String(hash));
            }
        }

        /// <summary>
        /// Verifies a password against a stored value. Handles both:
        ///  - Properly hashed values (PBKDF2$...)
        ///  - Legacy plaintext values still sitting in the database from
        ///    before hashing was added. Callers should re-hash and save
        ///    the password after a successful legacy match (see
        ///    IsLegacyPlaintext) so accounts migrate automatically the
        ///    next time each user logs in.
        /// </summary>
        public static bool VerifyPassword(string password, string storedValue)
        {
            if (string.IsNullOrEmpty(storedValue))
                return false;

            if (!storedValue.StartsWith(Prefix))
            {
                // Legacy plaintext - direct comparison
                return storedValue == password;
            }

            string[] parts = storedValue.Substring(Prefix.Length).Split('$');

            if (parts.Length != 3)
                return false;

            int iterations = int.Parse(parts[0]);
            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] expectedHash = Convert.FromBase64String(parts[2]);

            byte[] actualHash = ComputeHash(password, salt, iterations);

            return SlowEquals(expectedHash, actualHash);
        }

        /// <summary>
        /// True if the stored value predates hashing (plain text).
        /// Used to trigger an automatic, silent re-hash on next login.
        /// </summary>
        public static bool IsLegacyPlaintext(string storedValue)
        {
            return !string.IsNullOrEmpty(storedValue) &&
                   !storedValue.StartsWith(Prefix);
        }

        private static byte[] ComputeHash(string password, byte[] salt, int iterations)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                return pbkdf2.GetBytes(HashSize);
            }
        }

        // Constant-time comparison to avoid timing attacks
        private static bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;

            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }

            return diff == 0;
        }
    }
}
