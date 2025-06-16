using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VertexFormCore.Editor
{
    public class PresentationTab : ITabGroup
    {
        private List<ToolkitItem> items = new List<ToolkitItem>();

        public string Name => "PRESENTATION TOOLS";
        public string Description => "Tools for creating and managing presentations.";
        public bool HasSubTabs => false;

        public List<SubTabCategory> GetSubTabCategories()
        {
            return new List<SubTabCategory>();
        }

        public List<ToolkitItem> GetItems()
        {
            return items;
        }

        public void InitializeItems()
        {
            items.Clear();

            items.Add(new ToolkitItem(
                "Make Slide Show",
                "Assigns selected slide objects to the 'Slides' list in the SlideshowHandler script. Each slide must be a child of the SlidesHolder object.\nDesign the UI for each slide (text, images, etc.) as needed. You can duplicate and modify the base slide GameObject to create additional slides.",
                "",
                () => CreateSlideShow(false)));

            items.Add(new ToolkitItem(
                "Create Video Player",
                "Creates a flat video screen with a VideoPlayer component.\nAttach an AudioSource with spatial audio enabled (set Spatial Blend to 1) to the VideoPlayer GameObject.\nAdjust 'Max Distance' in the AudioSource settings to control audio falloff.\nBlue gizmo spheres in the scene show the audible range. The volume decreases as the user moves away from the green sphere.",
                "",
                () => CreateVideoPlayer()));

            items.Add(new ToolkitItem(
                "Create 360 Video Player",
                "Creates a 360-degree video player using the skybox.\nAssign a 360-degree video clip to the VideoPlayer to render the video all around the user.",
                "",
                () => CreateSkybox360VideoPlayer()));

            items.Add(new ToolkitItem(
                "Create 3D Renderer Video Player",
                "Creates a 3D object with a video texture.\nAssign a VideoPlayer and connect it to the object's material using the VideoPlayerController script.\nAttach an AudioSource with spatial audio enabled (set Spatial Blend to 1).\nAdjust 'Max Distance' for sound range. Blue spheres indicate range in the scene. Volume fades with distance from the green sphere.",
                "",
                () => Create3DVideoPlayer()));

            items.Add(new ToolkitItem(
                "Create 2D Audio Player",
                "Creates a basic 2D audio player.\nAssign an AudioClip in the AudioPlayer script.\nEnable looping in the AudioSource if needed and adjust the volume using the provided slider.",
                "",
                () => CreateAudioPlayer()));

            items.Add(new ToolkitItem(
                "Create 3D Spatial Audio Player",
                "Creates a 3D audio player with spatial sound.\nAssign an AudioClip in the AudioPlayer script.\nEnable looping if needed in the AudioSource settings.\nAdjust 'Max Distance' to control how far the sound reaches. Blue spheres show the sound radius, and volume decreases with distance from the green sphere.",
                "",
                () => CreateSpatialAudioPlayer()));
        }

        #region PRESENTATION METHODS

        public void CreateSlideShow(bool isNetworked)
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/Slide Show"));
            g.name = "SlideShow";
            var handler = g.GetComponent<SlideShowHandler>();
            handler.isNetworked = isNetworked;
            handler.slides[0].SetActive(true);
            handler.HandleSlide(0);
        }

        public void CreateVideoPlayer()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/Video Player/Video Screen Player"));
            g.name = "Video Screen Player";
            EditorGUIUtility.PingObject(g);
        }

        public void Create3DVideoPlayer()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/Video Player/3D Render Video"));
            g.name = "3D Render Video";
            EditorGUIUtility.PingObject(g);
        }

        public void CreateSkybox360VideoPlayer()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/Video Player/SkyboxVideo"));
            g.name = "SkyboxVideo";
            EditorGUIUtility.PingObject(g);
        }

        public void CreateAudioPlayer()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/Video Player/AudioPlayer"));
            g.name = "AudioPlayer";
            EditorGUIUtility.PingObject(g);
        }

        public void CreateSpatialAudioPlayer()
        {
            GameObject g = Object.Instantiate(Resources.Load<GameObject>("CustomEditor/Video Player/Spatial AudioPlayer"));
            g.name = "Spatial AudioPlayer";
            EditorGUIUtility.PingObject(g);
        }

        #endregion
    }
}
