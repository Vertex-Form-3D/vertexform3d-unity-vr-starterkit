using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VertextFormCore
{

    [CreateAssetMenu(menuName = "Data", fileName = "DataBase", order = 1)]
    public class SerializedDataBase : ScriptableObject
    {

        [SerializeField] public List<PlaceData> _placeItemDatas;
        [SerializeField] public List<HubItemData> _hubItemDatas;
        [SerializeField] public List<string> _otherScenesDatas;

        internal List<string> allStringList;

        private void OnValidate()
        {
#if UNITY_EDITOR
            allStringList = new List<string>();
            for (int i = 0; i < _placeItemDatas.Count; i++)
            {
                allStringList.Add(_placeItemDatas[i].placeName);
            }
            for (int i = 0; i < _hubItemDatas.Count; i++)
            {
                allStringList.Add(_hubItemDatas[i].HubName);
            }

            for (int i = 0; i < _otherScenesDatas.Count; i++)
            {
                allStringList.Add(_otherScenesDatas[i]);
            }
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

    }
}