using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ReadyPlayerMe.AvatarCreator;

namespace ReadyPlayerMe.Samples.AvatarCreatorWizard
{
    public static class AssetTypeHelper
    {
        public static IEnumerable<AssetType> GetAssetTypesByFilter(AssetFilter filter)
        {
            return Enum.GetValues(typeof(AssetType))
                .Cast<AssetType>()
                .Where(assetType =>
                {
                    var fieldInfo = typeof(AssetType).GetField(assetType.ToString());
                    var attribute = fieldInfo?.GetCustomAttribute<AssetTypeFilterAttribute>();
                    return attribute?.filter == filter;
                });
        }
    }
}