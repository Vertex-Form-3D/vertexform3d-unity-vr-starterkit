/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using Sample = Meta.Voice.Hub.Content.Sample;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using PackageSample = UnityEditor.PackageManager.UI.Sample;

namespace Meta.Voice.Hub.Inspectors
{
    [CustomEditor(typeof(Sample))]
    public class SampleEditor : Editor
    {
        #if VSDK_INTERNAL
        private bool edit = true;
        #else
        private bool edit = false;
        #endif
        private Sample _sample;

        // Layout
        private const float _sampleButtonWidth = 150f;
        private const float _sampleButtonHeight = 30f;
        private const float _sampleIconMargin = 5f;
        private const float _sampleIconMaxWidth = 600f;
        private static GUIStyle _sampleTitleStyle;
        private static GUIStyle _sampleWordwrapStyle;

        private void OnEnable()
        {
            _sample = (Sample) target;
        }

        public override void OnInspectorGUI()
        {
            DrawSample(_sample, EditorGUIUtility.currentViewWidth - 40f, true);

            #if VSDK_INTERNAL
            GUILayout.Space(16);
            edit = EditorGUILayout.Foldout(edit, "Edit");
            #endif

            if (edit)
            {
                base.OnInspectorGUI();
            }
        }

        /// <summary>
        /// Draw method for a sample
        /// </summary>
        /// <param name="sample">Sample to be drawn</param>
        /// <param name="tileSize">Width of a tile</param>
        public static void DrawSample(Sample sample, float tileSize, bool showDescription = false)
        {
            // Generate all required styles
            InitStyles();

            // Begin
            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(tileSize));

            // Image
            if (sample.tileImage != null)
            {
                float imageWidth = tileSize - _sampleIconMargin * 2f;
                imageWidth = Mathf.Min(imageWidth, sample.tileImage.width, _sampleIconMaxWidth);
                float imageHeight = imageWidth * sample.tileImage.height / sample.tileImage.width;
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(sample.tileImage, GUILayout.Width(imageWidth), GUILayout.Height(imageHeight));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }
            
            // Info
            GUILayout.Label(sample.title, _sampleTitleStyle, GUILayout.Height(_sampleTitleStyle.lineHeight * 2));
            EditorGUILayout.Space();
            if (showDescription)
            {
                GUILayout.Label(sample.description, _sampleWordwrapStyle);
                EditorGUILayout.Space();
            }

            // Open sample scene
            GUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            if (CanSampleOpen(sample))
            {
                if (GUILayout.Button("Open Sample", GUILayout.Height(_sampleButtonHeight)))
                {
                    OpenSample(sample);
                }
            }
            else
            {
                // Get sample package if possible
                PackageSample package = GetPackageSample(sample);
                if (!string.IsNullOrEmpty(package.displayName))
                {
                    if (package.isImported)
                    {
                        if (GUILayout.Button("Open Sample", GUILayout.Height(_sampleButtonHeight)))
                        {
                            // TODO: Find sample scene in importPath
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Import from Package", GUILayout.Height(_sampleButtonHeight)))
                        {
                            ImportSample(package);
                        }
                    }
                }
                // Error log
                else
                {
                    Color defaultColor = GUI.color;
                    GUI.color = Color.red;
                    GUILayout.Label("Sample Not Found", _sampleWordwrapStyle);
                    GUI.color = defaultColor;
                }
            }
            EditorGUILayout.Space();
            GUILayout.EndHorizontal();
            EditorGUILayout.Space();

            // Complete
            GUILayout.EndVertical();
        }

        private static void InitStyles()
        {
            if (null == _sampleTitleStyle)
            {
                _sampleTitleStyle = new GUIStyle(EditorStyles.boldLabel);
                _sampleTitleStyle.wordWrap = true;
                _sampleTitleStyle.fontSize = 16;
                _sampleTitleStyle.alignment = TextAnchor.UpperLeft;
            }

            if (null == _sampleWordwrapStyle)
            {
                _sampleWordwrapStyle = new GUIStyle(EditorStyles.label);
                _sampleWordwrapStyle.wordWrap = true;
            }
        }

        /// <summary>
        /// Checks if a sample can be opened, if false the sample must be imported.
        /// </summary>
        /// <param name="sample">The sample data to be checked</param>
        /// <returns>True if sample is imported & can be opened</returns>
        public static bool CanSampleOpen(Sample sample)
        {
            return sample.sceneReference != null;
        }

        /// <summary>
        /// Opens a sample if possible
        /// </summary>
        /// <param name="sample">The sample to be opened</param>
        public static void OpenSample(Sample sample)
        {
            if (CanSampleOpen(sample))
            {
                string scenePath = AssetDatabase.GetAssetPath(sample.sceneReference);
                EditorSceneManager.OpenScene(scenePath);
            }
        }

        /// <summary>
        /// Imports a sample into the current scene
        /// </summary>
        public static void ImportSample(PackageSample sample)
        {
            if (!sample.isImported)
            {
                sample.Import();
            }
        }

        /// <summary>
        /// Returns package sample if possible
        /// </summary>
        /// <param name="sample"></param>
        /// <returns></returns>
        private static PackageSample GetPackageSample(Sample sample)
        {
            string sampleAssetPath = AssetDatabase.GetAssetPath(sample);
            PackageInfo info = PackageInfo.FindForAssetPath(sampleAssetPath);
            if (!string.IsNullOrEmpty(info.packageId))
            {
                foreach (var packageSample in PackageSample.FindByPackage(info.name, info.version))
                {
                    if (string.Equals(packageSample.displayName, sample.packageSampleName))
                    {
                        return packageSample;
                    }
                }
            }
            return new PackageSample();
        }
    }
}
