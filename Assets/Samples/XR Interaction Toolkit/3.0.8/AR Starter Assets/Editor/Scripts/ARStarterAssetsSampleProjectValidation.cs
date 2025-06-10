using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils.Editor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using UnityEditor.XR.Interaction.Toolkit.ProjectValidation;
using UnityEngine;

namespace UnityEditor.XR.Interaction.Toolkit.Samples.ARStarterAssets.Editor
{
    /// <summary>
    /// Unity Editor class which registers Project Validation rules for the AR Starter Assets sample,
    /// checking that other required samples are installed.
    /// </summary>
    static class ARStarterAssetsSampleProjectValidation
    {
        const string k_SampleDisplayName = "AR Starter Assets";
        const string k_Category = "XR Interaction Toolkit";
        const string k_StarterAssetsSampleName = "Starter Assets";
        const string k_XRIPackageName = "com.unity.xr.interaction.toolkit";
        const string k_ARFPackageName = "com.unity.xr.arfoundation";
        const string k_ARFPackageMinVersionString = "4.2.8";
        const float k_TimeOutInSeconds = 3f;

        static readonly PackageVersion s_ARFPackageMinVersion = new PackageVersion(k_ARFPackageMinVersionString);

        static readonly BuildTargetGroup[] s_BuildTargetGroups =
            ((BuildTargetGroup[])Enum.GetValues(typeof(BuildTargetGroup))).Distinct().ToArray();

        static readonly List<BuildValidationRule> s_BuildValidationRules = new List<BuildValidationRule>
        {
            new BuildValidationRule
            {
                IsRuleEnabled = () => s_ARFPackageAddRequest == null || s_ARFPackageAddRequest.IsCompleted,
                Message = $"[{k_SampleDisplayName}] AR Foundation ({k_ARFPackageName}) package must be installed or updated to use this sample.",
                Category = k_Category,
                CheckPredicate = () => PackageVersionUtility.GetPackageVersion(k_ARFPackageName) >= s_ARFPackageMinVersion,
                FixIt = () =>
                {
                    var packString = k_ARFPackageName;
                    var searchResult = Client.Search(k_ARFPackageName, true);
                    var timeout = Time.realtimeSinceStartup + k_TimeOutInSeconds;
                    while (!searchResult.IsCompleted && timeout > Time.realtimeSinceStartup)
                    {
                        System.Threading.Thread.Sleep(10);
                    }

                    if (searchResult.IsCompleted)
                    {
                        var version = searchResult.Result
                            .Where((info) => string.Compare(k_ARFPackageName, info.name) == 0)
#if UNITY_2022_2_OR_NEWER
                            .Select(info => info.versions.recommended)
#else
                            .Select(info =>info.versions.verified)
#endif
                            .FirstOrDefault();

                        if (!string.IsNullOrEmpty(version))
                        {
                            var verifiedVersion = new PackageVersion(version);
                            if (verifiedVersion >= s_ARFPackageMinVersion)
                            {
                                packString = k_ARFPackageName + "@" + version;
                            }
                            else
                            {
                                Debug.LogError($"Package installation error: {k_ARFPackageMinVersionString}@{version} is below the minimum version of {k_ARFPackageMinVersionString}. Please install manually from Package Manager or update to a newer version of the Unity Editor.");
                                return;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Timeout trying to get package list after {k_TimeOutInSeconds} seconds.");
                    }

                    s_ARFPackageAddRequest = Client.Add(packString);
                    if (s_ARFPackageAddRequest.Error != null)
                    {
                        Debug.LogError($"Package installation error: {s_ARFPackageAddRequest.Error}: {s_ARFPackageAddRequest.Error.message}");
                    }
                },
                FixItAutomatic = true,
                Error = true,
            },
            new BuildValidationRule
            {
                Message = $"[{k_SampleDisplayName}] {k_StarterAssetsSampleName} sample from XR Interaction Toolkit ({k_XRIPackageName}) package must be imported or updated to use this sample. {GetImportSampleVersionMessage(k_Category, k_StarterAssetsSampleName, PackageVersionUtility.GetPackageVersion(k_XRIPackageName))}",
                Category = k_Category,
                CheckPredicate = () => ProjectValidationUtility.SampleImportMeetsMinimumVersion(k_Category, k_StarterAssetsSampleName, PackageVersionUtility.GetPackageVersion(k_XRIPackageName)),
                FixIt = () =>
                {
                    if (TryFindSample(k_XRIPackageName, string.Empty, k_StarterAssetsSampleName, out var sample))
                    {
                        sample.Import(Sample.ImportOptions.OverridePreviousImports);
                    }
                },
                FixItAutomatic = true,
                Error = !ProjectValidationUtility.HasSampleImported(k_Category, k_StarterAssetsSampleName),
            },
        };

        static AddRequest s_ARFPackageAddRequest;

        [InitializeOnLoadMethod]
        static void RegisterProjectValidationRules()
        {
            foreach (var buildTargetGroup in s_BuildTargetGroups)
            {
                BuildValidator.AddRules(buildTargetGroup, s_BuildValidationRules);
            }
        }

        static bool TryFindSample(string packageName, string packageVersion, string sampleDisplayName, out Sample sample)
        {
            sample = default;

            var packageSamples = Sample.FindByPackage(packageName, packageVersion);
            if (packageSamples == null)
            {
                Debug.LogError($"Couldn't find samples of the {ToString(packageName, packageVersion)} package; aborting project validation rule.");
                return false;
            }

            foreach (var packageSample in packageSamples)
            {
                if (packageSample.displayName == sampleDisplayName)
                {
                    sample = packageSample;
                    return true;
                }
            }

            Debug.LogError($"Couldn't find {sampleDisplayName} sample in the {ToString(packageName, packageVersion)} package; aborting project validation rule.");
            return false;
        }

        static string ToString(string packageName, string packageVersion)
        {
            return string.IsNullOrEmpty(packageVersion) ? packageName : $"{packageName}@{packageVersion}";
        }

        static string GetImportSampleVersionMessage(string packageFolderName, string sampleDisplayName, PackageVersion version)
        {
            if (ProjectValidationUtility.SampleImportMeetsMinimumVersion(packageFolderName, sampleDisplayName, version) || !ProjectValidationUtility.HasSampleImported(packageFolderName, sampleDisplayName))
                return string.Empty;

            return $"An older version of {sampleDisplayName} has been found. This may cause errors.";
        }
    }
}
