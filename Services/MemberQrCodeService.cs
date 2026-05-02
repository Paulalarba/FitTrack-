using System.Security.Cryptography;
using System.Text.Json;
using FitTrack.Models;
using Microsoft.AspNetCore.DataProtection;

namespace FitTrack.Services;

public sealed record MemberQrPayload(string MemberId, string Token);

public class MemberQrCodeService
{
    private readonly IDataProtector protector;

    public MemberQrCodeService(IDataProtectionProvider dataProtectionProvider)
    {
        protector = dataProtectionProvider.CreateProtector("FitTrack.MemberQrCode.v1");
    }

    public string CreateProtectedPayload(ApplicationUser user)
    {
        if (string.IsNullOrWhiteSpace(user.QrCodeToken))
        {
            throw new InvalidOperationException("The member does not have a QR code token.");
        }

        var payload = JsonSerializer.Serialize(new MemberQrPayload(user.Id, user.QrCodeToken));
        return protector.Protect(payload);
    }

    public bool TryReadPayload(string? protectedPayload, out MemberQrPayload? payload)
    {
        payload = null;

        if (string.IsNullOrWhiteSpace(protectedPayload))
        {
            return false;
        }

        try
        {
            var json = protector.Unprotect(protectedPayload.Trim());
            payload = JsonSerializer.Deserialize<MemberQrPayload>(json);
            return payload is not null
                && !string.IsNullOrWhiteSpace(payload.MemberId)
                && !string.IsNullOrWhiteSpace(payload.Token);
        }
        catch
        {
            return false;
        }
    }

    public static string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
