using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Secrets
{
    /// <summary>All secrets (Resources/SecretDatabase) + journal pages: one page per region with a cryptic synopsis.</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Secret Database", fileName = "SecretDatabase")]
    public sealed class SecretDatabase : ScriptableObject
    {
        [System.Serializable]
        public sealed class Page
        {
            public string region = "";
            [TextArea] public string synopsis = "";
        }

        public List<SecretData> secrets = new List<SecretData>();
        public List<Page> pages = new List<Page>();

        static SecretDatabase _loaded;
        public static SecretDatabase Instance => _loaded != null ? _loaded : _loaded = Resources.Load<SecretDatabase>("SecretDatabase");

        public SecretData Get(string id) => secrets.Find(s => s != null && s.id == id);
        public IEnumerable<SecretData> InRegion(string region)
        {
            foreach (SecretData s in secrets) if (s != null && s.region == region) yield return s;
        }

        public Page PageFor(string region) => pages.Find(p => p.region == region);

        /// <summary>(found, total) on a region's page.</summary>
        public (int found, int total) Progress(string region, SecretState state)
        {
            int f = 0, t = 0;
            foreach (SecretData s in InRegion(region)) { t++; if (state.IsFound(s.id)) f++; }
            return (f, t);
        }

        public static string PageFullFlag(string region) => $"journal_{region}_full";
    }
}
