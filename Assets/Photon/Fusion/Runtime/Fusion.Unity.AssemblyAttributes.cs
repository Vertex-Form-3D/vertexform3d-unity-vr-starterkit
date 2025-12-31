#if !FUSION_DEV

#region Assets/Photon/Fusion/Runtime/AssemblyAttributes/FusionAssemblyAttributes.Common.cs

// merged AssemblyAttributes

#region RegisterResourcesLoader.cs

// register a default loader; it will attempt to load the asset from their default paths if they happen to be Resources
[assembly: Fusion.FusionGlobalScriptableObjectResource(typeof(Fusion.FusionGlobalScriptableObject), Order = 2000, AllowFallback = true)]

#endregion



#endregion

#endif
