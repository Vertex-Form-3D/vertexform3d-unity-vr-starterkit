using System.Threading;
using ReadyPlayerMe.AvatarCreator;
using UnityEngine;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public class CameraZoom : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Transform nearTransform;
        [SerializeField] private Transform halfBodyTransform;
        [SerializeField] private Transform farTransform;
        [SerializeField] private float defaultDuration = 0.25f;

        private CancellationTokenSource ctx;

        private void OnDestroy()
        {
            ctx?.Cancel();
        }

        public void ToFaceView()
        {
            ctx?.Cancel();
            ctx = new CancellationTokenSource();
            _ = cameraTransform.LerpPosition(nearTransform.position, defaultDuration, ctx.Token);
        }

        public void ToFullbodyView()
        {
            ctx?.Cancel();
            ctx = new CancellationTokenSource();
            _ = cameraTransform.LerpPosition(farTransform.position, defaultDuration, ctx.Token);
        }

        public void ToHalfBody()
        {
            cameraTransform.position = halfBodyTransform.transform.position;
        }
    }
}
