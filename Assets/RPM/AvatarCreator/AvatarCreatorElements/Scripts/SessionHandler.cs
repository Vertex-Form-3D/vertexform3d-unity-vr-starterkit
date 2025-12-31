using System;
using System.Threading;
using System.Threading.Tasks;
using ReadyPlayerMe.AvatarCreator;
using UnityEngine;
using UnityEngine.Events;
using TaskExtensions = ReadyPlayerMe.AvatarCreator.TaskExtensions;

namespace ReadyPlayerMe.Samples.AvatarCreatorElements
{
    public class SessionHandler : MonoBehaviour
    {
        private readonly string sessionStoreKey = "StoredSession";

        public UnityEvent<UserSession> OnLogin;

        private async void Start()
        {
            using var cancellationTokenSource = new CancellationTokenSource();
            await TaskExtensions.HandleCancellation(Login(cancellationTokenSource.Token));
        }

        private async Task Login(CancellationToken token)
        {
            if (PlayerPrefs.HasKey(sessionStoreKey))
            {
                AuthManager.SetUser(JsonUtility.FromJson<UserSession>(PlayerPrefs.GetString(sessionStoreKey)));
            }
            else
            {
                await AuthManager.LoginAsAnonymous(token);
            }
            OnLogin?.Invoke(AuthManager.UserSession);
        }

        private void OnApplicationQuit()
        {
            PlayerPrefs.SetString(sessionStoreKey, JsonUtility.ToJson(AuthManager.UserSession));
        }

    }
}
