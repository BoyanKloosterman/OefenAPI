using System.Security.Cryptography;

namespace OefenApi.Auth;

internal static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int HashSize = 32;

    private static readonly string DummySalt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    private static readonly string DummyHash = Hash("niet-een-echt-wachtwoord", DummySalt);

    public static string CreateSalt()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
    }

    public static string Hash(string password, string saltBase64)
    {
        byte[] salt = Convert.FromBase64String(saltBase64);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return Convert.ToBase64String(hash);
    }

    public static bool Verify(string password, string saltBase64, string hashBase64)
    {
        byte[] expected = Convert.FromBase64String(hashBase64);
        byte[] salt = Convert.FromBase64String(saltBase64);
        byte[] actual = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            HashSize);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    public static void VerifyUnknown(string password)
    {
        Verify(password, DummySalt, DummyHash);
    }
}
