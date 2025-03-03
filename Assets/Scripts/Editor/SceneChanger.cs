using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace VertextFormCore
{
    /// <summary>
    /// Scene Manager class
    /// 
    /// This is a utility class and is used to quickly switch between scenes or clear you player preferences.
    /// </summary>
    public class SceneChanger : EditorWindow
    {
        /// <summary>
        /// Adding Scene Manager in Menu Item. This will initialize and render the Editor Window for
        /// Accessing Scenes
        /// </summary>
        [MenuItem("Utilities/Scene Changer")]
        static void Init()
        {
            SceneChanger window = (SceneChanger)EditorWindow.GetWindow(typeof(SceneChanger));
            window.Show();
        }

        /// <summary>
        /// This will render all Scenes buttons added in the build settings. If you click a scene button it will save
        /// currently active scene so you wont lose your progress and switch to the scene you clicked.
        /// 
        /// Additionally it can clear player prefs
        /// </summary>
        private void OnGUI()
        {
            // Responsible for showing available scenes
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                int currentIndex = i;
                string currentSceneName = System.IO.Path.GetFileNameWithoutExtension(EditorBuildSettings.scenes[currentIndex].path.ToString());

                GUI.enabled = !(System.IO.Path.GetFileNameWithoutExtension(SceneManager.GetActiveScene().path) == currentSceneName);

                EditorGUILayout.BeginHorizontal();
                if (EditorBuildSettings.scenes[currentIndex].enabled)
                {
                    if (GUILayout.Button(currentSceneName))
                    {
                        if (SceneManager.GetActiveScene().isDirty)
                        {
                            if (EditorUtility.DisplayDialog(
                        "Unsaved Scene",
                        "Your current scene is unsaved. If you do not save your scene your unsaved data will be lost",
                        "Save"
                        ))
                            {
                                EditorSceneManager.SaveOpenScenes();
                            }
                        }

                        EditorSceneManager.OpenScene(EditorBuildSettings.scenes[currentIndex].path.TrimEnd());
                    }
                    GUI.enabled = true;
                    if (GUILayout.Button("Select"))
                    {
                        EditorGUIUtility.PingObject(AssetDatabase.LoadMainAssetAtPath(EditorBuildSettings.scenes[currentIndex].path));
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            // Adding Space in between two seperate functionalities
            GUILayout.Space(10f);
            GUILayout.Label("Save Data Controls");
            GUILayout.Space(10f);

            if (GUILayout.Button("Clear Player Prefs"))
            {
                // if User presses Yes on this Dialog box all data stored in Player Preferences will get deleted.
                if (EditorUtility.DisplayDialog(
                    "Delete Player Prefs",
                    "Do you really want to delete all your data stored in Player Preferences",
                    "Yes"
                    ))
                {
                    DeleteAllPlayerPrefs();
                }
            }

            if (GUILayout.Button("Open Persistent Path"))
            {
                OpenPersistentPath();
            }

        }

        /// <summary>
        /// Delete Player Preferences. This will reset all your save data.
        /// </summary>
        void DeleteAllPlayerPrefs()
        {
            PlayerPrefs.DeleteAll();
        }

        void OpenPersistentPath()
        {
            Application.OpenURL(Application.persistentDataPath);
        }
    }
}