using System;
using System.Security.Cryptography;

namespace RentTrackerBackend.Services
{
    public static class SequentialGuidGenerator
    {
        public static Guid NewSequentialGuid()
        {
            // Get current timestamp
            byte[] timestamp = BitConverter.GetBytes(DateTime.UtcNow.Ticks);
            Array.Reverse(timestamp); // Ensure sortability

            // Generate random bytes
            byte[] randomBytes = new byte[8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            // Combine timestamp and random bytes
            byte[] guidBytes = new byte[16];
            Buffer.BlockCopy(timestamp, 0, guidBytes, 0, 8);
            Buffer.BlockCopy(randomBytes, 0, guidBytes, 8, 8);

            return new Guid(guidBytes);
        }
    }
}