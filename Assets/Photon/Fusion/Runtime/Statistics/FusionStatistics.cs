namespace Fusion.Statistics {
  using System;
  using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
  using UnityEngine.Serialization;
  
  [RequireComponent(typeof(NetworkRunner))]
  [DisallowMultipleComponent]
  [AddComponentMenu("Fusion/Statistics/Fusion Statistics")]
  public class FusionStatistics : SimulationBehaviour, ISpawned {
    internal List<FusionStatsGraphBase> ActiveGraphs => _statsGraph;
    
    // Setup prefabs
    private GameObject _statsCanvasPrefab;
    private FusionNetworkObjectStatsGraphCombine _objectGraphCombinePrefab;

    private const string STATS_CANVAS_PREFAB_PATH = "FusionStatsResources/FusionStatsRenderPanel";
    private const string STATS_OBJECT_COMBINE_PREFAB_PATH = "FusionStatsResources/NetworkObjectStatistics";
    
    private List<FusionStatsGraphBase> _statsGraph;
    private FusionStatsPanelHeader _header;
    private FusionStatsConfig _config;
    private FusionStatsCanvas _statsCanvas;
    private GameObject _statsPanelObject;
    private Dictionary<FusionNetworkObjectStatistics, FusionNetworkObjectStatsGraphCombine> _objectStatsGraphCombines;

    [InlineHelp]
    [ExpandableEnum]
    [SerializeField] private RenderSimStats _statsEnabled;

    [InlineHelp]
    [SerializeField] private CanvasAnchor _canvasAnchor = CanvasAnchor.TopRight;
    

    [FormerlySerializedAs("_statsConfig")] [SerializeField]
    [Header("Custom configuration to override default values.\nSelect only one stat flag per configuration.")]
    private List<FusionStatisticsStatCustomConfig> _statsCustomConfig = new List<FusionStatisticsStatCustomConfig>();

    internal List<FusionStatisticsStatCustomConfig> StatsCustomConfig => _statsCustomConfig;

    /// <summary>
    /// Gets a value indicating whether the statistics panel is active.
    /// </summary>
    public bool IsPanelActive => _statsPanelObject != false;

    [System.Serializable]
    public struct FusionStatisticsStatCustomConfig {
      public RenderSimStats Stat;
      public float Threshold1;
      public float Threshold2;
      public float Threshold3;
      public bool IgnoreZeroOnBuffer;
      public bool IgnoreZeroOnAverageCalculation;
      public int AccumulateTimeMs;
    }
    
    private void Awake() {
      _statsGraph = new List<FusionStatsGraphBase>();
      _statsCanvasPrefab = Resources.Load<GameObject>(STATS_CANVAS_PREFAB_PATH);
      _objectGraphCombinePrefab = Resources.Load<FusionNetworkObjectStatsGraphCombine>(STATS_OBJECT_COMBINE_PREFAB_PATH);

      if (_statsCanvasPrefab == null || _objectGraphCombinePrefab == null) {
        Log.Error($"Error loading the required assets for Fusion Statistics, destroying stats instance. Make sure that the following paths are valid for the Fusion Statistics resource assets: \n 1. {STATS_CANVAS_PREFAB_PATH} \n 2. {STATS_OBJECT_COMBINE_PREFAB_PATH}");
        Destroy(this);
      }
    }

    void ISpawned.Spawned() {
      SetupStatisticsPanel();
    }

    /// <summary>
    /// Sets the custom configuration for Fusion Statistics.
    /// </summary>
    /// <param name="customConfig">The list of custom configurations for Fusion Statistics.</param>
    public void SetStatsCustomConfig(List<FusionStatisticsStatCustomConfig> customConfig) {
      if (customConfig == default) {
        Log.Warn("Trying to set a null Fusion Statistics custom stats config");
        return;
      }
      
      _statsCustomConfig = customConfig;
      ApplyCustomConfig();
    }

    /// <summary>
    /// Sets the anchor position of the Fusion Statistics canvas.
    /// </summary>
    /// <param name="anchor">The anchor position of the canvas (TopLeft or TopRight).</param>
    public void SetCanvasAnchor(CanvasAnchor anchor) {
      _canvasAnchor = anchor;
      if (_statsCanvas == false) return;
      _statsCanvas.SetCanvasAnchor(anchor);
    }

    private void ApplyCustomConfig() {
      if (!_header) return;
      _header.ApplyStatsConfig(_statsCustomConfig);
    }

    /// <summary>
    /// Called from a custom editor script.
    /// Will update any editor information into the fusion statistics.
    /// </summary>
    public void OnEditorChange() {
      RenderEnabledStats();
      ApplyCustomConfig();
      SetCanvasAnchor(_canvasAnchor);
    }
    
    private void RenderEnabledStats() {
      if (IsPanelActive == false) return;
      _header.SetStatsToRender(_statsEnabled);
    }
    
    internal void UpdateStatsEnabled(RenderSimStats stats) {
      _statsEnabled = stats;
    }

    /// <summary>
    /// Sets up the statistics panel for Fusion statistic tracking.
    /// </summary>
    public void SetupStatisticsPanel() {
      if (IsPanelActive) return;

      // Was not registered on the Runner yet
      if (Runner == null) {
        var runner = GetComponent<NetworkRunner>();
        
        if (runner.IsRunning == false) {
          Log.Warn($"Network Runner on ({runner.gameObject}) is not yet running.");
          return;
        }
        
        runner.AddGlobal(this);
        // Return because when spawned is called the setup method will be called again.
        return;
      }
      
      _objectStatsGraphCombines = new Dictionary<FusionNetworkObjectStatistics, FusionNetworkObjectStatsGraphCombine>();
      
      _statsPanelObject = Instantiate(_statsCanvasPrefab, transform);
      _statsCanvas = _statsPanelObject.GetComponentInChildren<FusionStatsCanvas>();
      _statsCanvas.SetupStatsCanvas(this, _canvasAnchor, DestroyStatisticsPanel);
      _header = _statsPanelObject.GetComponentInChildren<FusionStatsPanelHeader>();
      _header.SetupHeader(Runner.LocalPlayer.ToString(), this);
      _config = _statsPanelObject.GetComponentInChildren<FusionStatsConfig>(true);

      _statsPanelObject.AddComponent<FusionBasicBillboard>();
      ApplyCustomConfig();

      Runner.AddVisibilityNodes(_statsPanelObject);
      
      if (_statsEnabled != 0)
        RenderEnabledStats();
      
      // Setup Event system
      if (!EventSystem.current) {
        Log.Debug("Fusion Statistics: No event system detected, creating one.");
        new GameObject("EventSystem-FusionStatistics", typeof(EventSystem), typeof(StandaloneInputModule));
      }
    }

    /// <summary>
    /// Sets the world anchor for Fusion Statistics. Set null to return to screen space overlay.
    /// </summary>
    /// <param name="anchor">The FusionStatsWorldAnchor component that defines the anchor object. Null to return to screen space overlay.</param>
    /// <param name="scale">The scale of the statistics panel.</param>
    public void SetWorldAnchor(FusionStatsWorldAnchor anchor, float scale) {
      _config.SetWorldCanvasScale(scale);

      if (anchor == null) {
        _config.ResetToCanvasAnchor();
      } else {
        _config.SetWorldAnchor(anchor.transform);
      }
    }

    /// <summary>
    /// Destroys the statistics panel.
    /// </summary>
    public void DestroyStatisticsPanel() {
      var keys = _objectStatsGraphCombines?.Keys.ToArray();
      if (keys != null) {
        foreach (var fusionNetworkObjectStatistics in keys) {
          MonitorNetworkObject(fusionNetworkObjectStatistics.NetworkObject, fusionNetworkObjectStatistics, false);
        }
      }

      _objectStatsGraphCombines?.Clear();
      _statsGraph.Clear();
      
      Destroy(_statsPanelObject);
      _statsPanelObject = null;

      if (Runner) {
        if (Runner.TryGetFusionStatistics(out var statisticsManager)) {
          statisticsManager.ObjectStatisticsManager.ClearMonitoredNetworkObjects();
        }
      }
    }

    public bool MonitorNetworkObject(NetworkObject networkObject, FusionNetworkObjectStatistics objectStatisticsInstance, bool monitor) {

      if (Runner.TryGetFusionStatistics(out var statisticsManager)) {
        statisticsManager.ObjectStatisticsManager.MonitorNetworkObjectStatistics(networkObject.Id, monitor);
      }
      
      if (monitor) {
        
        // If Id already monitored on the stats, return false to destroy the object statistics instance.
        if (_objectStatsGraphCombines.ContainsKey(objectStatisticsInstance))
          return false;
        
        var graphCombine = Instantiate(_objectGraphCombinePrefab, _header.ContentRect);
        graphCombine.SetupNetworkObject(networkObject, this, objectStatisticsInstance);
        _objectStatsGraphCombines.Add(objectStatisticsInstance, graphCombine);
      } else {
        
        if (_objectStatsGraphCombines.Remove(objectStatisticsInstance, out var graphCombine)) {
          Destroy(graphCombine.gameObject);
          Destroy(objectStatisticsInstance);
        }
      }

      return true;
    }

    void UpdateAllGraphs(FusionStatisticsManager statisticsManager) {
      var now = DateTime.Now;
      foreach (var statsGraphBase in _statsGraph) {
        statsGraphBase.UpdateGraph(Runner, statisticsManager, ref now);
      }
    }

    public void RegisterGraph(FusionStatsGraphBase graph) {
      _statsGraph.Add(graph);
    }

    public void UnregisterGraph(FusionStatsGraphBase graph) {
      _statsGraph.Remove(graph);
    }

    private void Update() {
      // Safety exit
      if (!Runner) return;
      
      
      Profiler.BeginSample("Fusion Statistics Update Graph");

      // Collect and update
      if (Runner.TryGetFusionStatistics(out var statisticsManager)) {
        UpdateAllGraphs(statisticsManager);
      }
      
      Profiler.EndSample();
    }
  }
}