using Aethvirse.Stats;
using Aethvirse.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aethvirse.Save
{
    /// <summary>
    /// Moves the player between scenes (regions, Gates, the academy, the Penalty Zone): the player's state is
    /// carried in memory like a save, and restored when the player spawns in the new scene.
    /// </summary>
    public static class SceneTravel
    {
        /// <summary><paramref name="position"/> null = the scene's own spawn point.</summary>
        public static void Go(string scene, Vector3? position, bool rememberReturn = false)
        {
            PlayerStats player = PlayerStats.Current;
            GameSession s = GameSession.Instance;
            if (rememberReturn && player != null)
            {
                s.ReturnScene = SceneManager.GetActiveScene().name;
                s.ReturnPosition = player.transform.position;
            }
            var data = new SaveData();
            if (player != null) player.Capture(data.player);
            data.player.hasPosition = position.HasValue;
            if (position.HasValue) data.player.position = position.Value;
            SaveManager.SetPending(data);
            Combat.TimeFx.ResetAll();
            SceneManager.LoadScene(scene);
        }

        /// <summary>Back to where <see cref="Go"/> was called with rememberReturn.</summary>
        public static void Return()
        {
            GameSession s = GameSession.Instance;
            string scene = string.IsNullOrEmpty(s.ReturnScene) ? Core.BootLoader.FirstScene : s.ReturnScene;
            Vector3 at = s.ReturnPosition;
            s.ReturnScene = "";
            Go(scene, at);
        }
    }
}
