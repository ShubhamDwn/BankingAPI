namespace BankingAPI.Services
{
    public static class GuidHelper
    {
        public static string GenerateDeviceGuid()
        {
            return Guid.NewGuid().ToString();
        }
    }

}
