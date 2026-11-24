using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aethvirse.Core
{
    /// <summary>Boot is the first scene in every build; it hands over to the first gameplay scene.</summary>
    public static class BootLoader
    {
        public const string BootScene = "Boot";
        // M1: the movement playground. Later: the main menu.
        public const string FirstScene = "Playground";
        /// <summary>Where Boot goes: the title screen.</summary>
        public const string TitleScene = "Title";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void LoadFirstScene()
        {
            if (SceneManager.GetActiveScene().name == BootScene) SceneManager.LoadScene(TitleScene);
        }
    }
}
