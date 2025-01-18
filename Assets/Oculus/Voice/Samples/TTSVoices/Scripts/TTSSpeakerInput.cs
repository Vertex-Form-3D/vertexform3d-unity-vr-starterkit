/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;
using UnityEngine.UI;
using Meta.WitAi.TTS.Utilities;

namespace Meta.Voice.Samples.TTSVoices
{
    public class TTSSpeakerInput : MonoBehaviour
    {
        [SerializeField] private TTSSpeaker _speaker;
        [SerializeField] private InputField _input;
        [SerializeField] private Button _stopButton;
        [SerializeField] private Button _speakButton;
        [SerializeField] private Toggle _queueButton;

        [SerializeField] private string _dateId = "[DATE]";
        [SerializeField] private string[] _queuedText;

        // States
        private string _voice;
        private bool _loading;
        private bool _speaking;

        // Add delegates
        private void OnEnable()
        {
            RefreshButtons();
            _stopButton.onClick.AddListener(StopClick);
            _speakButton.onClick.AddListener(SpeakClick);
        }
        // Stop click
        private void StopClick() => _speaker.Stop();
        // Speak phrase click
        private void SpeakClick()
        {
            // Queue
            if (_queueButton != null && _queueButton.isOn)
            {
                _speaker.SpeakQueued(FormatText(_input.text));
                foreach (var text in _queuedText)
                {
                    _speaker.SpeakQueued(FormatText(text));
                }
            }
            // Set
            else
            {
                _speaker.Speak(FormatText(_input.text));
            }
        }
        // Format text with current datetime
        private string FormatText(string text)
        {
            string result = text;
            if (result.Contains(_dateId))
            {
                DateTime now = DateTime.Now;
                string dateString = $"{now.ToLongDateString()} at {now.ToShortTimeString()}";
                result = text.Replace(_dateId, dateString);
            }
            return result;
        }
        // Remove delegates
        private void OnDisable()
        {
            _stopButton.onClick.RemoveListener(StopClick);
            _speakButton.onClick.RemoveListener(SpeakClick);
        }

        // Preset text fields
        private void Update()
        {
            // On preset voice id update
            if (!string.Equals(_voice, _speaker.presetVoiceID))
            {
                _voice = _speaker.presetVoiceID;
                _input.placeholder.GetComponent<Text>().text = $"Write something to say in {_voice}'s voice";
            }
            // On state changes
            if (_loading != _speaker.IsLoading)
            {
                _loading = _speaker.IsLoading;
                RefreshButtons();
            }
            if (_speaking != _speaker.IsSpeaking)
            {
                _speaking = _speaker.IsSpeaking;
                RefreshButtons();
            }
        }
        // Refresh interactable based on states
        private void RefreshButtons()
        {
            _stopButton.interactable = _loading || _speaking;
        }
    }
}
