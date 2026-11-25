using System.Collections;
using System.IO;
using System.Text;
using Aethvirse.Player;
using Aethvirse.World;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Aethvirse.Core
{
    /// <summary>
    /// Self-test for development: run the player with <c>-autoshot &lt;folder&gt;</c> and it walks a fixed route —
    /// title, a new game in the academy, the status / bag / map panels, Ashfall, the north border (streaming) and the
    /// next region — saving a screenshot and a diagnostics line at each stop, then quits. Lets every change be checked
    /// by looking at real frames instead of guessing.
    /// </summary>
    public sealed class AutoShot : MonoBehaviour
    {
        string _dir;
        readonly StringBuilder _log = new StringBuilder();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            string[] args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-autoshot");
            if (i < 0 || i + 1 >= args.Length) return;
            var go = new GameObject("AutoShot");
            DontDestroyOnLoad(go);
            var a = go.AddComponent<AutoShot>();
            a._dir = args[i + 1];
            Directory.CreateDirectory(a._dir);
            a.StartCoroutine(a.Run());
        }

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(_dir, name + ".png"));
            DisplayInfo d = Screen.mainWindowDisplayInfo;
            int players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None).Length;
            int cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None).Length;
            var scenes = new StringBuilder();
            for (int i = 0; i < SceneManager.sceneCount; i++) scenes.Append(SceneManager.GetSceneAt(i).name).Append(SceneManager.GetSceneAt(i) == SceneManager.GetActiveScene() ? "*" : "").Append(' ');
            Transform p = PlayerTarget.Current;
            _log.AppendLine($"{name}: screen {Screen.width}x{Screen.height} {Screen.fullScreenMode} display {d.width}x{d.height} | scenes {scenes}| region {RegionController.Current?.id} | players {players} cameras {cameras} | pos {(p != null ? p.position.ToString("0") : "-")} | fps {1f / Mathf.Max(Time.smoothDeltaTime, 0.0001f):0}");
            File.WriteAllText(Path.Combine(_dir, "diag.txt"), _log.ToString());
            yield return new WaitForSecondsRealtime(0.5f);
        }

        static void Teleport(Vector3 to)
        {
            var pc = PlayerTarget.Current != null ? PlayerTarget.Current.GetComponent<PlayerController>() : null;
            if (pc != null) pc.Teleport(to);
        }

        IEnumerator Run()
        {
            yield return new WaitForSecondsRealtime(4f);
            yield return Shot("01_title");
            UI.TitleMenu.NewGame();
            yield return new WaitForSecondsRealtime(6f);
            yield return Shot("02_academy");
            StatusPanel.Toggle();
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("03_status");
            StatusPanel.Toggle();
            UI.InventoryPanel.Toggle();
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("04_bag");
            UI.InventoryPanel.Toggle();
            UI.MapView.Toggle();
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("05_map");
            UI.MapView.Toggle();

            // Open every world (the F11 test cheat) and go to Ashfall.
            WorldFlags f = GameSession.Instance.Flags;
            foreach (RegionData r in WorldAtlas.Instance.AllRegions()) f.SetFlag(Secrets.SecretDatabase.PageFullFlag(r.id));
            Save.SceneTravel.Go("R1_Ashfall", new Vector3(0f, 0.2f, -100f));
            yield return new WaitForSecondsRealtime(6f);
            yield return Shot("06_ashfall");
            Teleport(new Vector3(0f, 0.3f, 125f));
            yield return new WaitForSecondsRealtime(7f);
            yield return Shot("07_border");
            Teleport(new Vector3(0f, 0.3f, 205f));
            yield return new WaitForSecondsRealtime(4f);
            yield return Shot("08_next_region");
            Teleport(new Vector3(0f, 0.3f, 470f));
            yield return new WaitForSecondsRealtime(8f);
            yield return Shot("09_far_north");
            _log.AppendLine("done");
            File.WriteAllText(Path.Combine(_dir, "diag.txt"), _log.ToString());
            Application.Quit();
        }
    }
}
