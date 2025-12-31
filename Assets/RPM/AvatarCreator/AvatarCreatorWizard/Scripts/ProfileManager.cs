using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using ReadyPlayerMe.AvatarCreator;
using ReadyPlayerMe.Core;
using UnityEngine;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public class ProfileManager : MonoBehaviour
    {
        private const string TAG = nameof(ProfileManager);
        private const string DIRECTORY_NAME = "Ready Player Me";
        private const string FILE_NAME = "User";

        [SerializeField] private ProfileUI profileUI;

        private string filePath;
        private string directoryPath;
        private string lastModifiedAvatarId;

        private void Awake()
        {
            directoryPath = $"{Application.persistentDataPath}/{DIRECTORY_NAME}";
            filePath = $"{directoryPath}/{FILE_NAME}";
        }

        private void OnEnable()
        {
            profileUI.SignedOut += AuthManager.Logout;
            AuthManager.OnSignedOut += DeleteSession;
        }

        private void OnDisable()
        {
            if (AuthManager.IsSignedIn)
            {
                SaveSession(AuthManager.UserSession);
            }
            profileUI.SignedOut -= AuthManager.Logout;
            AuthManager.OnSignedOut -= DeleteSession;
        }

        public bool LoadSession()
        {
            if (!File.Exists(filePath))
            {
                SDKLogger.Log(TAG, $"Session file not found in {filePath}");
                return false;
            }
            var bytes = File.ReadAllBytes(filePath);
            var json = Encoding.UTF8.GetString(bytes);
            var userSession = JsonConvert.DeserializeObject<UserSession>(json);
            AuthManager.SetUser(userSession);

            SetProfileData(userSession);

            SDKLogger.Log(TAG, $"Loaded session from {filePath}");
            return true;
        }

        public void SaveSession(UserSession userSession)
        {
            var json = JsonConvert.SerializeObject(userSession);
            DirectoryUtility.ValidateDirectory(directoryPath);
            File.WriteAllBytes(filePath, Encoding.UTF8.GetBytes(json));
            SetProfileData(userSession);

            SDKLogger.Log(TAG, $"Saved session to {filePath}");
        }

        private void SetProfileData(UserSession userSession)
        {
            profileUI.SetProfileData(
                userSession.Name,
                char.ToUpperInvariant(userSession.Name[0]).ToString()
            );
        }

        private void DeleteSession()
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            profileUI.ClearProfile();

            SDKLogger.Log(TAG, $"Deleted session at {filePath}");
        }
    }
}
