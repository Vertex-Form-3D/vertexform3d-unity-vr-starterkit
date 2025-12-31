namespace Fusion.Statistics {
using System;
using UnityEngine;
using UnityEngine.UI;

  [Flags]
  internal enum NetworkObjectStat{ 
    InBandwidth = 1 << 0,
    OutBandwidth = 1 << 1,
    InPackets = 1 << 2,
    OutPackets = 1 << 3,
    AverageInPacketSize = 1 << 4,
    AverageOutPacketSize = 1 << 5
  }

  public class FusionNetworkObjectStatsGraph : FusionStatsGraphBase {
    [SerializeField] private Text _description;
    private NetworkId _id;
    private NetworkObjectStat _stat;
    private FusionNetworkObjectStatsGraphCombine _combineParentGraph;

    public override void UpdateGraph(NetworkRunner runner, FusionStatisticsManager statisticsManager, ref DateTime now) {
      AddValueToBuffer(GetNetworkObjectStatValue(statisticsManager), ref now);
    }

    private float GetNetworkObjectStatValue(FusionStatisticsManager statisticsManager) {
      if (statisticsManager.ObjectStatisticsManager.GetNetworkObjectStatistics(_id, out var snapshot)) {
        switch (_stat) {
          case NetworkObjectStat.InBandwidth:
            return snapshot.InBandwidth;
          case NetworkObjectStat.OutBandwidth:
            return snapshot.OutBandwidth;
          case NetworkObjectStat.InPackets:
            return snapshot.InPackets;
          case NetworkObjectStat.OutPackets:
            return snapshot.OutPackets;
          case NetworkObjectStat.AverageInPacketSize:
            return snapshot.InBandwidth / Mathf.Max(1, snapshot.InPackets);
          case NetworkObjectStat.AverageOutPacketSize:
            return snapshot.OutBandwidth / Mathf.Max(1, snapshot.OutPackets);
        }
      }

      return -1;
    }

    internal void SetupNetworkObjectStat(NetworkId id, NetworkObjectStat stat) {
      _id = id;
      _stat = stat;
      _description.text = _stat.ToString();
      
      string valueTextFormat;
      float threshold1 = 0, threshold2 = 0, threshold3 = 0;
      float valueTextMultiplier = 1;
      bool ignoreZeroOnAverage = false, ignoreZeroOnBuffer = false;
      int accumulateTimeMs = 0;

      switch (stat) {
        
        case NetworkObjectStat.InBandwidth:
        case NetworkObjectStat.OutBandwidth:
          valueTextFormat = "{0:0} B";
          accumulateTimeMs = 1000;
          _description.text += " (Per second)";
          break;
        case NetworkObjectStat.AverageInPacketSize:
        case NetworkObjectStat.AverageOutPacketSize:
          valueTextFormat = "{0:0} B";
          ignoreZeroOnAverage = true;
          ignoreZeroOnBuffer = true;
          break;
        
        case NetworkObjectStat.InPackets:
        case NetworkObjectStat.OutPackets:
          valueTextFormat = "{0:0}";
          accumulateTimeMs = 1000;
          _description.text += " (Per second)";
          break;
          
        default:
          valueTextFormat = "{0:0}";
          break;
      }
      
      SetValueTextFormat(valueTextFormat);
      SetValueTextMultiplier(valueTextMultiplier);
      SetThresholds(threshold1, threshold2, threshold3);
      SetIgnoreZeroValues(ignoreZeroOnAverage, ignoreZeroOnBuffer);
      Initialize(accumulateTimeMs);
    }
  }
}