namespace Fusion.Statistics {
  using System;
  using System.Collections.Generic;
  using UnityEngine;
  using UnityEngine.Serialization;
  using UnityEngine.UI;

  public class FusionStatsConfig : MonoBehaviour {
    
    public bool IsWorldAnchored => _worldTransformAnchor != null;

    [SerializeField] private Button _worldAnchorButtonPrefab;
    [SerializeField] private Transform _worldAnchorListContainer;
    [SerializeField] private GameObject _configPanel;
    [SerializeField] private Canvas _canvas;
    [SerializeField] private RectTransform _renderPanelRectTransform;

    private Transform _worldTransformAnchor;
    private float _worldCanvasScale = 0.005f;

    private FusionStatistics _fusionStatistics;
    
    private static List<Transform> _worldAnchorCandidates = new List<Transform>();
    private static event Action _onWorldAnchorCandidatesUpdate;

    internal static void SetWorldAnchorCandidate(Transform candidate, bool register) {
      if (register) {
        if (_worldAnchorCandidates.Contains(candidate) == false)
          _worldAnchorCandidates.Add(candidate);
      } else {
        _worldAnchorCandidates.Remove(candidate);
      }
      
      _onWorldAnchorCandidatesUpdate?.Invoke();
    }

    internal void SetupStatisticReference(FusionStatistics fusionStatistics) {
      _fusionStatistics = fusionStatistics;
    }

    public void ToggleConfigPanel() {
      _configPanel.SetActive(!_configPanel.activeSelf);
    }

    public void ToggleUseWorldAnchor(bool value) {
      // If true, the buttons will trigger the re-parenting logic.
      if (value == false) {
        ResetToCanvasAnchor();
      }
    }
    
    internal void SetWorldAnchor(Transform worldTransformAnchor) {
      _canvas.renderMode = RenderMode.WorldSpace;
      _renderPanelRectTransform.localScale = Vector3.one * _worldCanvasScale;
      _renderPanelRectTransform.localPosition = Vector3.zero;
      
      
      if (worldTransformAnchor == _worldTransformAnchor) return;
      _renderPanelRectTransform.SetParent(worldTransformAnchor);
      _worldTransformAnchor = worldTransformAnchor;
      _renderPanelRectTransform.localPosition = Vector3.zero;
    }

    public void SetWorldCanvasScale(float value) {
      _worldCanvasScale = value;
    }

    internal void ResetToCanvasAnchor() {
      // Was called from editor destroy
      if (!_fusionStatistics)
        return;

      var childPanel = (RectTransform)_renderPanelRectTransform.GetChild(0);
      
      _renderPanelRectTransform.SetParent(_fusionStatistics.transform);
      _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
      _renderPanelRectTransform.localScale = Vector3.one;
      _renderPanelRectTransform.localPosition = Vector3.zero;
      childPanel.localPosition = Vector3.zero;
      childPanel.anchoredPosition = Vector3.zero;
      _worldTransformAnchor = default;
    }

    private void UpdateWorldAnchorButtons() {
      // Clear all old buttons, ok because it should not be frequent
      for (int i = _worldAnchorListContainer.childCount-1; i >= 0 ; i--) {
        Destroy(_worldAnchorListContainer.GetChild(i).gameObject);
      }

      foreach (var candidate in _worldAnchorCandidates) {
        var button = Instantiate(_worldAnchorButtonPrefab, _worldAnchorListContainer);
        button.onClick.AddListener(() => SetWorldAnchor(candidate));
        button.GetComponentInChildren<Text>().text = candidate.name;
      }
    }

    private void OnEnable() {
      _onWorldAnchorCandidatesUpdate -= UpdateWorldAnchorButtons;
      _onWorldAnchorCandidatesUpdate += UpdateWorldAnchorButtons;
      UpdateWorldAnchorButtons();
    }

    private void OnDestroy() {
      _onWorldAnchorCandidatesUpdate -= UpdateWorldAnchorButtons;
    }
  }
}