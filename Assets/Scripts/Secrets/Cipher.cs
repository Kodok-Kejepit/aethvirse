using System.Text;

namespace Aethvirse.Secrets
{
    public enum CipherKind { Caesar, Atbash, Reverse, Morse, Numbers }

    /// <summary>Ciphers used by locks, murals and broken Codex messages (§9.1; pure and tested). Letters A–Z only.</summary>
    public static class Cipher
    {
        static readonly string[] MorseTable =
        {
            ".-", "-...", "-.-.", "-..", ".", "..-.", "--.", "....", "..", ".---", "-.-", ".-..", "--",
            "-.", "---", ".--.", "--.-", ".-.", "...", "-", "..-", "...-", ".--", "-..-", "-.--", "--..",
        };

        public static string Encode(string plain, CipherKind kind, int key = 0)
        {
            plain = plain.ToUpperInvariant();
            var sb = new StringBuilder();
            switch (kind)
            {
                case CipherKind.Caesar:
                    foreach (char c in plain) sb.Append(IsLetter(c) ? (char)('A' + ((c - 'A' + key) % 26 + 26) % 26) : c);
                    break;
                case CipherKind.Atbash:
                    foreach (char c in plain) sb.Append(IsLetter(c) ? (char)('Z' - (c - 'A')) : c);
                    break;
                case CipherKind.Reverse:
                    for (int i = plain.Length - 1; i >= 0; i--) sb.Append(plain[i]);
                    break;
                case CipherKind.Morse:
                    foreach (char c in plain) { if (!IsLetter(c)) continue; if (sb.Length > 0) sb.Append("  "); sb.Append(MorseTable[c - 'A']); }
                    break;
                case CipherKind.Numbers:
                    foreach (char c in plain) { if (!IsLetter(c)) continue; if (sb.Length > 0) sb.Append('-'); sb.Append(c - 'A' + 1); }
                    break;
            }
            return sb.ToString();
        }

        public static string Decode(string coded, CipherKind kind, int key = 0)
        {
            switch (kind)
            {
                case CipherKind.Caesar: return Encode(coded, CipherKind.Caesar, -key);
                case CipherKind.Atbash: return Encode(coded, CipherKind.Atbash);
                case CipherKind.Reverse: return Encode(coded, CipherKind.Reverse);
                case CipherKind.Morse:
                {
                    var sb = new StringBuilder();
                    foreach (string part in coded.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))
                    {
                        int i = System.Array.IndexOf(MorseTable, part);
                        if (i >= 0) sb.Append((char)('A' + i));
                    }
                    return sb.ToString();
                }
                case CipherKind.Numbers:
                {
                    var sb = new StringBuilder();
                    foreach (string part in coded.Split('-'))
                        if (int.TryParse(part, out int n) && n >= 1 && n <= 26) sb.Append((char)('A' + n - 1));
                    return sb.ToString();
                }
            }
            return coded;
        }

        static bool IsLetter(char c) => c >= 'A' && c <= 'Z';
    }
}
