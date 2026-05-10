// ============================================================
// MemberQrCodeService.cs — QR Code Generation & Verification Service
// ============================================================
// This service is responsible for the secure, tamper-proof QR codes
// used at the gym entrance check-in scanner.
//
// HOW IT WORKS:
//   1. Each member has a random QrCodeToken stored in the database.
//   2. When a member opens their QR page, CreateProtectedPayload() serialises
//      their UserId + Token to JSON, then encrypts it using ASP.NET Core's
//      Data Protection API (AES-256-CBC + HMAC by default).
//   3. The encrypted string is rendered as a QR code in the browser.
//   4. When an admin scans the QR, VerifyQrCode() in AdminController calls
//      TryReadPayload() to decrypt and validate the payload.
//   5. If the member's token in the DB no longer matches (because they
//      pressed "Regenerate QR"), the old QR code is immediately invalidated.
//
// WHY DATA PROTECTION?
//   The Data Protection API automatically handles key rotation, key storage,
//   and algorithm selection. It is far more secure than rolling a custom
//   AES implementation, and it is the ASP.NET-recommended approach for
//   protecting short-lived tokens or sensitive strings.
// ============================================================

using System.Security.Cryptography;
using System.Text.Json;
using FitTrack.Models;
using Microsoft.AspNetCore.DataProtection;

namespace FitTrack.Services;

/// <summary>
/// Represents the decrypted contents of a member's QR code.
/// This is a C# 9+ record type — immutable and value-comparable.
/// </summary>
/// <param name="MemberId">The GUID string that identifies the member in AspNetUsers.</param>
/// <param name="Token">The random token stored in <see cref="ApplicationUser.QrCodeToken"/>.</param>
public sealed record MemberQrPayload(string MemberId, string Token);

/// <summary>
/// Scoped service that creates and reads tamper-proof QR code payloads for member check-in.
/// Registered as Scoped in Program.cs (one instance per HTTP request).
/// </summary>
public class MemberQrCodeService
{
    // IDataProtector is the encryption/decryption engine provided by ASP.NET Core.
    // The "purpose" string ("FitTrack.MemberQrCode.v1") namespaces the protector:
    // data encrypted with this purpose can ONLY be decrypted with the same purpose string.
    // Changing this string (e.g., to "v2") instantly invalidates all existing QR codes.
    private readonly IDataProtector protector;

    /// <summary>
    /// Constructor — called by the DI container.
    /// Creates a purpose-specific data protector using the application's key ring.
    /// </summary>
    /// <param name="dataProtectionProvider">Injected by ASP.NET Core DI.</param>
    public MemberQrCodeService(IDataProtectionProvider dataProtectionProvider)
    {
        protector = dataProtectionProvider.CreateProtector("FitTrack.MemberQrCode.v1");
    }

    /// <summary>
    /// Creates an encrypted, URL-safe string that encodes the member's identity.
    /// This string is what gets encoded into the visual QR code image in the browser.
    /// </summary>
    /// <param name="user">The logged-in member whose QR code is being generated.</param>
    /// <returns>An encrypted Base64-URL string safe to embed in a QR code.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the user does not have a QrCodeToken — seeding should prevent this.
    /// </exception>
    public string CreateProtectedPayload(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.QrCodeToken))
        {
            throw new InvalidOperationException("The member does not have a QR code token.");
        }

        // Serialize the payload to JSON, then encrypt it with the Data Protection API.
        // The result is an opaque, tamper-proof string that only this application can read.
        var payload = JsonSerializer.Serialize(new MemberQrPayload(user.Id, user.QrCodeToken));
        return protector.Protect(payload);
    }

    /// <summary>
    /// Attempts to decrypt and validate a QR code payload string received from the scanner.
    /// Returns <c>true</c> only if the payload decrypts successfully AND contains
    /// non-empty MemberId and Token values.
    /// </summary>
    /// <param name="protectedPayload">The raw string scanned from the QR code.</param>
    /// <param name="payload">
    /// Output parameter: the decrypted <see cref="MemberQrPayload"/> if successful;
    /// <c>null</c> if decryption fails.
    /// </param>
    /// <returns><c>true</c> if the payload is valid and trustworthy; <c>false</c> otherwise.</returns>
    public bool TryReadPayload(string? protectedPayload, out MemberQrPayload? payload)
    {
        payload = null;

        // Reject empty or null inputs immediately.
        if (string.IsNullOrWhiteSpace(protectedPayload))
        {
            return false;
        }

        try
        {
            // Unprotect decrypts the string. If it was tampered with or was encrypted
            // by a different application/key, this will throw CryptographicException.
            var json = protector.Unprotect(protectedPayload.Trim());

            // Deserialize the JSON back into the strongly-typed record.
            payload = JsonSerializer.Deserialize<MemberQrPayload>(json);

            // Final sanity check — both fields must be present.
            return payload is not null
                && !string.IsNullOrWhiteSpace(payload.MemberId)
                && !string.IsNullOrWhiteSpace(payload.Token);
        }
        catch
        {
            // Any decryption or deserialization failure means the QR is invalid/tampered.
            // Return false instead of propagating the exception.
            return false;
        }
    }

    /// <summary>
    /// Generates a new cryptographically-random, URL-safe token string.
    /// Used when creating new accounts or when a member requests a QR refresh.
    ///
    /// HOW IT WORKS:
    ///   1. RandomNumberGenerator.GetBytes(32) fills a 32-byte (256-bit) buffer
    ///      with OS-level random data — impossible to predict or brute-force.
    ///   2. Convert.ToBase64String encodes those bytes to a ~43-character string.
    ///   3. The padding ('=') is trimmed and '+'/'' are replaced so the token
    ///      is safe to use in URLs and JSON without escaping.
    /// </summary>
    /// <returns>A 43-character URL-safe Base64 token string.</returns>
    public static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')       // Remove Base64 padding characters
            .Replace('+', '-')  // Make URL-safe: replace '+' with '-'
            .Replace('/', '_'); // Make URL-safe: replace '/' with '_'
    }
}
