namespace Fusion.Statistics {
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

  public class FusionNetworkObjectStatsGraphCombine : MonoBehaviour {
    
    [SerializeField] private Text _titleText;
    [SerializeField] private Dropdown _statDropdown;
    [SerializeField] private NetworkObjectStat _statsToRender;
    [SerializeField] private RectTransform _rect;
    [SerializeField] private RectTransform _combinedGraphRender;
    [SerializeField] private Button _toggleButton;

    private float _headerHeight = 50;
    private float _graphHeight = 150;

    private Dictionary<NetworkObjectStat, FusionNetworkObjectStatsGraph> _statsGraphs;
    [SerializeField]
    private FusionNetworkObjectStatsGraph _statsGraphPrefab;

    private ContentSizeFitter _parentContentSizeFitter;

    /// <summary>
    /// Gets the unique identifier of the network object.
    /// </summary>
    /// <value>
    /// The network object identifier.
    /// </value>
    public NetworkId NetworkObjectID => _networkObject.Id;

    private NetworkObject _networkObject;
    private FusionStatistics _fusionStatistics;
    private FusionNetworkObjectStatistics _objectStatisticsInstance;

    public void SetupNetworkObject(NetworkObject networkObject, FusionStatistics fusionStatistics, FusionNetworkObjectStatistics objectStatisticsInstance) {
      _networkObject = networkObject;
      _fusionStatistics = fusionStatistics;
      _objectStatisticsInstance = objectStatisticsInstance;
    }

    private void Start() {
      _statsGraphs = new Dictionary<NetworkObjectStat, FusionNetworkObjectStatsGraph>();
      _parentContentSizeFitter = GetComponentInParent<ContentSizeFitter>();
      
      List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

      options.Add(new Dropdown.OptionData("Toggle Stats"));

      foreach (var option in Enum.GetNames(typeof(NetworkObjectStat))) {
        options.Add(new Dropdown.OptionData(option));
      }

      _statDropdown.options = options;

      _statDropdown.onValueChanged.AddListener(OnDropDownChanged);
      
      UpdateHeight();

      _titleText.text = _networkObject.Name;
    }

    private void OnDropDownChanged(int arg0) {
      if (arg0 <= 0) return; // No stat selected.
      arg0--; // Remove the first label

      NetworkObjectStat stat = (NetworkObjectStat)(1 << arg0);

      if ((_statsToRender & stat) == stat) {
        _statsToRender &= ~stat; // Removed the flag
        DestroyStatGraph(stat);
      } else {
        _statsToRender |= stat; // Set the flag
        InstantiateStatGraph(stat);
      }
      
      UpdateHeight();

      // Set the first label again.
      _statDropdown.SetValueWithoutNotify(0);
    }

    private void InstantiateStatGraph(NetworkObjectStat stat) {
      FusionNetworkObjectStatsGraph graph = Instantiate(_statsGraphPrefab, _combinedGraphRender);
      graph.SetupNetworkObjectStat(NetworkObjectID, stat);
      _statsGraphs.Add(stat, graph);
    }

    private void DestroyStatGraph(NetworkObjectStat stat) {
      _statsGraphs[stat].gameObject.SetActive(false);
      Destroy(_statsGraphs[stat].gameObject);
      _statsGraphs.Remove(stat);
    }
    
    private void UpdateHeight(float overrideValue = -1) {
      var sizeDelta = _rect.sizeDelta;
      var height = overrideValue >= 0 ? overrideValue : _headerHeight + _statsGraphs.Count * _graphHeight;
      _rect.sizeDelta = new Vector2(sizeDelta.x,height);
      
      // Need to refresh vertical scroll
      _parentContentSizeFitter.enabled = false;
      _parentContentSizeFitter.enabled = true;
    }

    private void OnDisable() {
      if (_statsGraphs == null) return;
      foreach (var graph in _statsGraphs.Values) {
        graph.gameObject.SetActive(false);
      }
    }

    private void OnEnable() {
      if (_statsGraphs == null) return;
      foreach (var graph in _statsGraphs.Values) {
        graph.gameObject.SetActive(true);
      }
    }

    public void ToggleRenderDisplay() {
      var active = _combinedGraphRender.gameObject.activeSelf;
      _combinedGraphRender.gameObject.SetActive(!active);
      
      if (active) {
        OnDisable();
        UpdateHeight(_headerHeight);
        _toggleButton.transform.rotation = Quaternion.Euler(0, 0, 90);
      } else {
        OnEnable();
        UpdateHeight();
        _toggleButton.transform.rotation = Quaternion.identity;
      }
    }

    public void DestroyCombinedGraph() {
      _fusionStatistics.MonitorNetworkObject(_networkObject, _objectStatisticsInstance, false);
    }
  }
}