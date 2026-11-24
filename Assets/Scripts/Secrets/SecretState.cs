using System;
using System.Collections.Generic;

namespace Aethvirse.Secrets
{
    /// <summary>Found secrets and seen hints (saved).</summary>
    [Serializable]
    public sealed class SecretState
    {
        public List<string> found = new List<string>();
        public List<string> hintKeys = new List<string>();

        public bool IsFound(string id) => found.Contains(id);
        public bool MarkFound(string id)
        {
            if (found.Contains(id)) return false;
            found.Add(id);
            return true;
        }

        public static string HintKey(string secret, int index) => secret + "#" + index;
        public bool HintSeen(string secret, int index) => hintKeys.Contains(HintKey(secret, index));
        public void SeeHint(string secret, int index) { string k = HintKey(secret, index); if (!hintKeys.Contains(k)) hintKeys.Add(k); }
    }
}
