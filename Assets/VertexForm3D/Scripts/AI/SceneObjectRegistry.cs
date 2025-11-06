using System.Collections.Generic;
using UnityEngine;

namespace VertexFormCore.AI
{
    /// <summary>
    /// 场景对象注册器 - 自动收集并标注场景中可控制的对象
    /// </summary>
    public class SceneObjectRegistry : MonoBehaviour
    {
        // 单例实例
        public static SceneObjectRegistry Instance { get; private set; }

        // 注册的场景对象列表
        private Dictionary<string, List<SceneObject>> m_RegisteredObjects = new Dictionary<string, List<SceneObject>>();

        // 所有注册对象的列表
        private List<SceneObject> m_AllObjects = new List<SceneObject>();

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeRegistry();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 初始化注册器
        /// </summary>
        private void InitializeRegistry()
        {
            Debug.Log("SceneObjectRegistry: 初始化场景对象注册器");
            
            // 自动扫描场景中的可控制对象
            ScanSceneObjects();
        }

        /// <summary>
        /// 扫描场景中的可控制对象
        /// </summary>
        private void ScanSceneObjects()
        {
            // 查找所有带有SceneObject标签的对象
            GameObject[] objects = GameObject.FindGameObjectsWithTag("SceneObject");
            
            foreach (GameObject obj in objects)
            {
                SceneObject sceneObj = obj.GetComponent<SceneObject>();
                if (sceneObj != null)
                {
                    RegisterObject(sceneObj);
                }
            }
            
            Debug.Log("SceneObjectRegistry: 扫描到 " + m_AllObjects.Count + " 个可控制对象");
        }

        /// <summary>
        /// 注册场景对象
        /// </summary>
        /// <param name="sceneObject">要注册的场景对象</param>
        public void RegisterObject(SceneObject sceneObject)
        {
            if (sceneObject == null || m_AllObjects.Contains(sceneObject))
                return;

            // 添加到所有对象列表
            m_AllObjects.Add(sceneObject);

            // 添加到标签分类
            foreach (string tag in sceneObject.Tags)
            {
                string lowerTag = tag.ToLower();
                if (!m_RegisteredObjects.ContainsKey(lowerTag))
                {
                    m_RegisteredObjects[lowerTag] = new List<SceneObject>();
                }
                m_RegisteredObjects[lowerTag].Add(sceneObject);
            }

            Debug.Log("SceneObjectRegistry: 注册对象 " + sceneObject.name + "，标签: " + string.Join(", ", sceneObject.Tags));
        }

        /// <summary>
        /// 注销场景对象
        /// </summary>
        /// <param name="sceneObject">要注销的场景对象</param>
        public void UnregisterObject(SceneObject sceneObject)
        {
            if (sceneObject == null || !m_AllObjects.Contains(sceneObject))
                return;

            // 从所有对象列表中移除
            m_AllObjects.Remove(sceneObject);

            // 从标签分类中移除
            foreach (string tag in sceneObject.Tags)
            {
                string lowerTag = tag.ToLower();
                if (m_RegisteredObjects.ContainsKey(lowerTag))
                {
                    m_RegisteredObjects[lowerTag].Remove(sceneObject);
                    if (m_RegisteredObjects[lowerTag].Count == 0)
                    {
                        m_RegisteredObjects.Remove(lowerTag);
                    }
                }
            }

            Debug.Log("SceneObjectRegistry: 注销对象 " + sceneObject.name);
        }

        /// <summary>
        /// 根据标签查找场景对象
        /// </summary>
        /// <param name="tag">标签</param>
        /// <returns>匹配的场景对象列表</returns>
        public List<SceneObject> FindObjectsByTag(string tag)
        {
            string lowerTag = tag.ToLower();
            if (m_RegisteredObjects.ContainsKey(lowerTag))
            {
                return new List<SceneObject>(m_RegisteredObjects[lowerTag]);
            }
            return new List<SceneObject>();
        }

        /// <summary>
        /// 根据关键词搜索场景对象（模糊匹配）
        /// </summary>
        /// <param name="keyword">关键词</param>
        /// <returns>匹配的场景对象列表</returns>
        public List<SceneObject> SearchObjects(string keyword)
        {
            string lowerKeyword = keyword.ToLower();
            List<SceneObject> results = new List<SceneObject>();

            foreach (SceneObject obj in m_AllObjects)
            {
                // 检查对象名称
                if (obj.name.ToLower().Contains(lowerKeyword))
                {
                    results.Add(obj);
                    continue;
                }

                // 检查对象标签
                foreach (string tag in obj.Tags)
                {
                    if (tag.ToLower().Contains(lowerKeyword))
                    {
                        results.Add(obj);
                        break;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// 获取所有注册的场景对象
        /// </summary>
        /// <returns>所有注册的场景对象列表</returns>
        public List<SceneObject> GetAllObjects()
        {
            return new List<SceneObject>(m_AllObjects);
        }

        /// <summary>
        /// 清理注册器
        /// </summary>
        private void OnDestroy()
        {
            m_RegisteredObjects.Clear();
            m_AllObjects.Clear();
        }
    }
}