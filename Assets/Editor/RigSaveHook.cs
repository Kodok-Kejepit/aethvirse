// Scenes are saved with the player rig inactive (World.RigActivator wakes it at runtime), so a region scene streamed
// in next to another never runs a second player, camera or HUD.
using Aethvirse.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class RigSaveHook
{
    static RigSaveHook()
    {
        EditorSceneManager.sceneSaving += (scene, path) =>
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.GetComponent<SceneRig>() != null && root.activeSelf) root.SetActive(false);
        };
    }
}
