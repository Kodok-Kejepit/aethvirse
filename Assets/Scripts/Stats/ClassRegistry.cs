using System.Collections.Generic;
using UnityEngine;

namespace Aethvirse.Stats
{
    /// <summary>All playable classes (Resources/ClassRegistry): class select, saves and the player look them up here.</summary>
    [CreateAssetMenu(menuName = "AETHVIRSE/Class Registry", fileName = "ClassRegistry")]
    public sealed class ClassRegistry : ScriptableObject
    {
        public List<ClassData> classes = new List<ClassData>();

        static ClassRegistry _loaded;
        public static ClassRegistry Instance => _loaded != null ? _loaded : _loaded = Resources.Load<ClassRegistry>("ClassRegistry");

        public ClassData Get(string id) => classes.Find(c => c != null && c.id == id);

        /// <summary>Issued starter weapon per class id.</summary>
        public static string StarterWeapon(string classId) => classId switch
        {
            "phantom" => "issued_daggers",
            "arcanist" => "issued_staff",
            "ranger" => "issued_bow",
            _ => "issued_greatsword",
        };
    }
}
