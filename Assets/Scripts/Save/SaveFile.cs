using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Aethvirse.Save
{
    /// <summary>
    /// Save container: "ASAV" + format + raw size + SHA-256(raw JSON) + gzip(JSON). Written to &lt;path&gt;.tmp
    /// then swapped in (atomic); the previous file is kept as &lt;path&gt;.bak. Reading validates everything and
    /// returns null for missing / foreign / truncated / corrupted files. Pure (unit tested).
    /// </summary>
    public static class SaveFile
    {
        static readonly byte[] Magic = { 0x41, 0x53, 0x41, 0x56 };
        public const int Format = 1;
        const int MaxRawBytes = 16 * 1024 * 1024;

        public static void Write(string path, string json)
        {
            byte[] raw = Encoding.UTF8.GetBytes(json);
            byte[] packed;
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, CompressionLevel.Optimal, leaveOpen: true)) gz.Write(raw, 0, raw.Length);
                packed = ms.ToArray();
            }
            string tmp = path + ".tmp";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write))
            using (var w = new BinaryWriter(fs))
            {
                w.Write(Magic);
                w.Write(Format);
                w.Write(raw.Length);
                w.Write(Hash(raw));
                w.Write(packed.Length);
                w.Write(packed);
                w.Flush();
                fs.Flush(true);
            }
            if (File.Exists(path))
            {
                File.Copy(path, path + ".bak", overwrite: true);
                File.Delete(path);
            }
            File.Move(tmp, path);
        }

        /// <summary>The JSON text, or null if the file is missing, foreign, truncated or corrupted.</summary>
        public static string Read(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
                using var r = new BinaryReader(fs);
                byte[] magic = r.ReadBytes(4);
                if (magic.Length != 4 || magic[0] != Magic[0] || magic[1] != Magic[1] || magic[2] != Magic[2] || magic[3] != Magic[3]) return null;
                if (r.ReadInt32() != Format) return null;
                int rawSize = r.ReadInt32();
                byte[] hash = r.ReadBytes(32);
                int packedSize = r.ReadInt32();
                if (rawSize <= 0 || rawSize > MaxRawBytes || packedSize <= 0 || packedSize > fs.Length - fs.Position) return null;
                byte[] packed = r.ReadBytes(packedSize);
                byte[] raw = new byte[rawSize];
                using (var gz = new GZipStream(new MemoryStream(packed), CompressionMode.Decompress))
                {
                    int read = 0;
                    while (read < rawSize)
                    {
                        int n = gz.Read(raw, read, rawSize - read);
                        if (n <= 0) return null;
                        read += n;
                    }
                }
                byte[] check = Hash(raw);
                for (int i = 0; i < 32; i++) if (check[i] != hash[i]) return null;
                return Encoding.UTF8.GetString(raw);
            }
            catch (Exception)
            {
                return null;
            }
        }

        static byte[] Hash(byte[] data)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(data);
        }
    }
}
