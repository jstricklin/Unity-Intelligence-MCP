using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UnityIntelligenceMCP.Utilities
{
    public static class FileHasher
    {
        public static async Task<string> ComputeSHA256Async(string filePath)
        {
            using var sha256 = SHA256.Create();
            await using var stream = File.OpenRead(filePath);
            var hashBytes = await sha256.ComputeHashAsync(stream);
            return BytesToHex(hashBytes);
        }

        private static string BytesToHex(byte[] bytes)
        {
            var builder = new StringBuilder();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}
