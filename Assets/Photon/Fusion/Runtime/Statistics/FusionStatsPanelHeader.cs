namespace Fusion.Statistics {
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

  /// <summary>
  /// List of all simulation stats able to render on a graph.
  /// </summary>
  [Flags]
  public enum RenderSimStats {
    /// <summary>
    /// Incoming packets.
    /// </summary>
    InPackets = 1 << 0,
    
    /// <summary>
    /// Outgoing packets.
    /// </summary>
    OutPackets = 1 << 1,
    
    /// <summary>
    /// Round Trip Time.
    /// </summary>
    RTT = 1 << 2,
    
    /// <summary>
    /// In Bandwidth in Bytes.
    /// </summary>
    InBandwidth = 1 << 3,
    
    /// <summary>
    /// Out Bandwidth in Bytes.
    /// </summary>
    OutBandwidth = 1 << 4,
    
    /// <summary>
    /// Amount of re-simulation ticks executed.
    /// </summary>
    Resimulations = 1 << 5,
    
    /// <summary>
    /// Amount of forward ticks executed.
    /// </summary>
    ForwardTicks = 1 << 6,
    
    /// <summary>
    /// Average measured time between two input/state packets (from same client) received by the server.
    /// </summary>
    InputReceiveDelta = 1 << 7,
    
    /// <summary>
    /// Time sync abruptly reset count.
    /// </summary>
    TimeResets = 1 << 8,
    
    /// <summary>
    /// Average measured time between two state packets (from server) received by the client.
    /// </summary>
    StateReceiveDelta = 1 << 9,
    
    /// <summary>
    /// Average buffering for prediction.
    /// </summary>
    SimulationTimeOffset = 1 << 10,
    
    /// <summary>
    /// How much the simulation is currently sped up / slowed down.
    /// </summary>
    SimulationSpeed = 1 << 11,
    
    /// <summary>
    /// Average buffering for interpolation.
    /// </summary>
    InterpolationOffset = 1 << 12,
    
    /// <summary>
    /// How much interpolation is currently sped up / slowed down.
    /// </summary>
    InterpolationSpeed = 1 << 13,
    
    /// <summary>
    /// Input in bandwidth.
    /// </summary>
    InputInBandwidth = 1 << 14,
    
    /// <summary>
    /// Input out bandwidth.
    /// </summary>
    InputOutBandwidth = 1 << 15,
    
    /// <summary>
    /// Average size for received packet.
    /// </summary>
    AverageInPacketSize = 1 << 16,
    
    /// <summary>
    /// Average size for sent packet.
    /// </summary>
    AverageOutPacketSize = 1 << 17,
    
    /// <summary>
    /// Amount of object updates received.
    /// </summary>
    InObjectUpdates = 1 << 18,
    
    /// <summary>
    /// Amount of object updates sent.
    /// </summary>
    OutObjectUpdates = 1 << 19,
    
    /// <summary>
    /// Memory in use for the object allocator.
    /// </summary>
    ObjectsAllocatedMemoryInUse = 1 << 20,
    
    /// <summary>
    /// Memory in use for the general allocator.
    /// </summary>
    GeneralAllocatedMemoryInUse = 1 << 21,
    
    /// <summary>
    /// Memory free for the object allocator.
    /// </summary>
    ObjectsAllocatedMemoryFree = 1 << 22,
    
    /// <summary>
    /// Memory free for the general allocator.
    /// </summary>
    GeneralAllocatedMemoryFree = 1 << 23,
    
    /// <summary>
    /// Amount of written words. How many networked changes are being sent.
    /// </summary>
    WordsWrittenCount = 1 << 24,
    
    /// <summary>
    /// Size of all last written words in Bytes.
    /// </summary>
    WordsWrittenSize = 1 << 25,
    
    /// <summary>
    /// Amount of read words. How many networked changes are being received.
    /// </summary>
    WordsReadCount = 1 << 26,
    
    /// <summary>
    /// Size of all last read words in Bytes.
    /// </summary>
    WordsReadSize = 1 << 27,
  }
  
  public class FusionStatsPanelHeader : MonoBehaviour {
    public event Action OnRenderStatsUpdate;
    
    [SerializeField] private Text _statsHeaderTitle;
    [SerializeField] private Dropdown _statsDropdown;
    [SerializeField] private FusionStatsGraphDefault _defaultGraphPrefab;

    public RectTransform ContentRect;

    private Dictionary<RenderSimStats,FusionStatsGraphDefault> _defaultStatsGraph;
    private FusionStatistics _fusionStatistics;
    private RenderSimStats _statsToRender;

    public void SetupHeader(string title, FusionStatistics fusionStatistics) {
      _statsHeaderTitle.text = title;
      _fusionStatistics = fusionStatistics;
      
      SetupDropdown();
    }

    private void SetupDropdown() {
      _defaultStatsGraph = new Dictionary<RenderSimStats, FusionStatsGraphDefault>();
      
      List<Dropdown.OptionData> options = new List<Dropdown.OptionData>();

      options.Add(new Dropdown.OptionData("Toggle Stats"));

      foreach (var option in Enum.GetNames(typeof(RenderSimStats))) {
        options.Add(new Dropdown.OptionData(option));
      }

      _statsDropdown.options = options;

      _statsDropdown.onValueChanged.AddListener(OnDropDownChanged);
    }

    internal void SetStatsToRender(RenderSimStats stats) {
      // Early exit
      if (stats == _statsToRender) return;
      
      // For each possible stat
      foreach (RenderSimStats renderSimStat in Enum.GetValues(typeof(RenderSimStats))) {
        // If it is set on the stats received
        if ((stats & renderSimStat) == renderSimStat) {
          // And if it is not already set on the stats to render... add it
          if ((_statsToRender & renderSimStat) != renderSimStat) {
            AddStat(renderSimStat);
          }
        }
        // else if is NOT set on the stats received
        else {
          // And if it is set on the stats to render... remove
          if ((_statsToRender & renderSimStat) == renderSimStat) {
            RemoveStat(renderSimStat);
          }
        }
      }
      
      // Make sure they are equal now.
      _statsToRender = stats;
    }

    private void AddStat(RenderSimStats stat) {
      _statsToRender |= stat; // Set the flag
      InstantiateStatGraph(stat);
      InvokeRenderStatsUpdate();
    }

    private void RemoveStat(RenderSimStats stat) {
      _statsToRender &= ~stat; // Removed the flag
      DestroyStatGraph(stat);
      InvokeRenderStatsUpdate();
    }

    private void InvokeRenderStatsUpdate() {
      OnRenderStatsUpdate?.Invoke();
    }
    
    private void OnDropDownChanged(int arg0) {
      if (arg0 <= 0) return; // No stat selected.
      arg0--; // Remove the first label
      
      RenderSimStats stat = (RenderSimStats)(1 << arg0);

      if ((_statsToRender & stat) == stat) {
        RemoveStat(stat);
      } else {
        AddStat(stat);
      }

      // Set the first label again.
      _statsDropdown.SetValueWithoutNotify(0);
      
      _fusionStatistics.UpdateStatsEnabled(_statsToRender);
    }

    private void InstantiateStatGraph(RenderSimStats stat) {
      FusionStatsGraphDefault graph = Instantiate(_defaultGraphPrefab, ContentRect);
      graph.SetupDefaultGraph(stat);
      TryApplyCustomStatConfig(graph);
      _defaultStatsGraph.Add(stat, graph);
    }

    private void DestroyStatGraph(RenderSimStats stat) {
      if (_defaultStatsGraph.Remove(stat, out var statsGraphDefault)) {
        Destroy(statsGraphDefault.gameObject);
      }
    }

    private void TryApplyCustomStatConfig(FusionStatsGraphDefault graph) {
      // Need to do this way because unity cannot serialize a dictionary.
      foreach (var config in _fusionStatistics.StatsCustomConfig) {
        if (config.Stat == graph.Stat) {
          ApplyCustomStatsConfig(graph, config);
        }
      }
    }

    private void ApplyCustomStatsConfig(FusionStatsGraphDefault graph, FusionStatistics.FusionStatisticsStatCustomConfig config) {
      graph.ApplyCustomStatsConfig(config);
    }

    internal void ApplyStatsConfig(List<FusionStatistics.FusionStatisticsStatCustomConfig> statsConfig) {
      foreach (var config in statsConfig) {
        if (_defaultStatsGraph.TryGetValue(config.Stat, out var graph)) {
          ApplyCustomStatsConfig(graph, config);
        }
      }
    }
  }
}