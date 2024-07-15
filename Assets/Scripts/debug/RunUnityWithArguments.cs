using UnityEditor;
//using UnityEditor.SceneManagement;

/*
public class RunWithArguments
{
    [MenuItem("Tools/Run With Arguments")]
    public static void RunGame()
    {
        // Set your command line arguments
        string arguments = "-diag-temp-memory-leak-validation";

        // Save current scene before running
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // Create a temporary configuration that includes the arguments
        EditorBuildSettingsScene[] originalScenes = EditorBuildSettings.scenes;
        EditorBuildSettingsScene[] tempScenes = new EditorBuildSettingsScene[originalScenes.Length];
        originalScenes.CopyTo(tempScenes, 0);

        // Add the arguments to the first scene's path as a hacky way to pass them
        tempScenes[0] = new EditorBuildSettingsScene(tempScenes[0].path + arguments, tempScenes[0].enabled);

        // Set the temporary scenes, build and run
        EditorBuildSettings.scenes = tempScenes;
        EditorApplication.isPlaying = true;

        // Restore the original scenes configuration after starting
        EditorApplication.update += RestoreScenes;

        void RestoreScenes()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorBuildSettings.scenes = originalScenes;
                EditorApplication.update -= RestoreScenes;
            }
        }
    }
}*/
