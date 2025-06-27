using System.Collections.Concurrent;

namespace BankingAPI.Services;

public static class OtpService
{
    private static readonly ConcurrentDictionary<string, string> _otpStore = new();

    public static string GenerateOtp()
    {
        var rnd = new Random();
        return rnd.Next(100000, 999999).ToString();
    }

    public static void SendOtp(string mobileNumber, string otp)
    {
        // Simulate sending OTP
        Console.WriteLine($"OTP for {mobileNumber} is {otp}"); // replace with real SMS integration
    }

    public static void StoreOtp(string customerId, string otp)
    {
        _otpStore[customerId] = otp;
    }

    public static bool VerifyOtp(string customerId, string enteredOtp)
    {
        return _otpStore.TryGetValue(customerId, out var correctOtp) && correctOtp == enteredOtp;
    }

    public static void RemoveOtp(string customerId)
    {
        _otpStore.TryRemove(customerId, out _);
    }
}

