using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;


public class AddresableHelpers 
{

    /// <summary>
    /// this should called after catlog updated
    /// </summary>
    /// <param name="locators"></param>
    /// <returns></returns>
    public static List<string> GetRemoteBundlesKey(List<IResourceLocator> locators=null)
   {
        if (locators == null)
            locators = new List<IResourceLocator>(Addressables.ResourceLocators);

        List<string> remoteBundlesKeys = new List<string>();       

        // Get the resource locator for the key.
        foreach (var locator in locators)
        {
            foreach (var key in locator.Keys)
            { 
                string keyName = key.ToString();
                if (keyName.Contains("/Remote"))
                remoteBundlesKeys.Add(keyName);               
            }
        }
        return remoteBundlesKeys;
   }      
    public static void LogBundlesKeys(MonoBehaviour context,List<IResourceLocator> locators = null) {        
        List<string> list = GetRemoteBundlesKey(locators);
        for (int i = 0; i < list.Count; i++) {
            Debuger.Log(context,list[i]);        
        }

    }


    //prefs
    public static void ClearBundlePlayerPrefCash() {
        List<string> list = GetRemoteBundlesKey();
        for (int i = 0; i < list.Count; i++)
        {
            PlayerPrefs.SetInt(list[i], (int)CashStatus.NotCased);
        }
    }

    public static void SignUpdatesKeysAsUnCashed(List<string> keys)
    {
        for (int i = 0; i < keys.Count; i++)
        {
            //flag it as un cashed
            PlayerPrefs.SetInt(keys[i], (int)CashStatus.NotCased);
        }
    }

    public static void SignBundleKeysAsCashed(string key)
    {
       PlayerPrefs.SetInt(key, (int)CashStatus.cased);
    }

}
