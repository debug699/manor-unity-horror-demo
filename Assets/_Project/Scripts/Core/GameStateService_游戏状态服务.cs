using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manor.Core
{
    public interface IGameStateService
    {
        GameStateData Snapshot { get; }
        event Action StateChanged;
        bool HasKey(string keyId);
        bool HasItem(string itemId);
        bool HasReadClue(string clueId);
        bool IsDoorOpen(string doorId);
        bool GatePasswordKnown { get; }
        bool BridgeRestored { get; }
        bool GateUnlocked { get; }
        bool CommunicationComplete { get; }
        bool ButcherChaseStarted { get; }
        float ChaseElapsedSeconds { get; }
        bool DawnTriggered { get; }
        bool AddKey(string keyId);
        bool AddItem(string itemId);
        bool MarkClueRead(string clueId);
        bool SetDoorOpen(string doorId, bool isOpen);
        bool SetGatePasswordKnown(bool known);
        bool SetBridgeRestored(bool restored);
        bool SetGateUnlocked(bool unlocked);
        bool SetCommunicationComplete(bool complete);
        bool SetButcherChaseStarted(bool started);
        bool SetChaseElapsedSeconds(float seconds);
        bool SetDawnTriggered(bool triggered);
        bool SetCheckpoint(string checkpointId, Vector3 safePosition);
        void SetScene(string sceneId);
        void SetObjective(string objectiveId);
        bool AdvanceStory(StoryStage nextStage);
        void Restore(GameStateData data);
    }

    public sealed class GameStateService : IGameStateService
    {
        private GameStateData _state;

        public GameStateService(GameStateData initialState = null)
        {
            _state = initialState?.Clone() ?? new GameStateData();
            EnsureCollections();
        }

        public GameStateData Snapshot => _state.Clone();
        public event Action StateChanged;

        public bool HasKey(string keyId) => Contains(_state.collectedKeys, keyId);
        public bool HasItem(string itemId) => Contains(_state.collectedItems, itemId);
        public bool HasReadClue(string clueId) => Contains(_state.readClues, clueId);
        public bool IsDoorOpen(string doorId) => Contains(_state.openDoors, doorId);
        public bool GatePasswordKnown => _state.gatePasswordKnown;
        public bool BridgeRestored => _state.bridgeRestored;
        public bool GateUnlocked => _state.gateUnlocked;
        public bool CommunicationComplete => _state.communicationComplete;
        public bool ButcherChaseStarted => _state.butcherChaseStarted;
        public float ChaseElapsedSeconds => _state.chaseElapsedSeconds;
        public bool DawnTriggered => _state.dawnTriggered;

        public bool AddKey(string keyId)
        {
            return AddUnique(_state.collectedKeys, keyId);
        }

        public bool AddItem(string itemId) => AddUnique(_state.collectedItems, itemId);

        public bool MarkClueRead(string clueId)
        {
            return AddUnique(_state.readClues, clueId);
        }

        public bool SetDoorOpen(string doorId, bool isOpen)
        {
            string normalized = RequireId(doorId, nameof(doorId));
            bool currentlyOpen = Contains(_state.openDoors, normalized);
            if (currentlyOpen == isOpen) return false;
            if (isOpen) _state.openDoors.Add(normalized);
            else _state.openDoors.Remove(normalized);
            StateChanged?.Invoke();
            return true;
        }

        public bool SetGatePasswordKnown(bool known) => SetFlag(ref _state.gatePasswordKnown, known);
        public bool SetBridgeRestored(bool restored) => SetFlag(ref _state.bridgeRestored, restored);
        public bool SetGateUnlocked(bool unlocked) => SetFlag(ref _state.gateUnlocked, unlocked);
        public bool SetCommunicationComplete(bool complete) => SetFlag(ref _state.communicationComplete, complete);
        public bool SetButcherChaseStarted(bool started) => SetFlag(ref _state.butcherChaseStarted, started);
        public bool SetDawnTriggered(bool triggered) => SetFlag(ref _state.dawnTriggered, triggered);

        public bool SetCheckpoint(string checkpointId, Vector3 safePosition)
        {
            string normalized = RequireId(checkpointId, nameof(checkpointId));
            if (_state.checkpointId == normalized && _state.lastSafePosition == safePosition) return false;
            _state.checkpointId = normalized;
            _state.lastSafePosition = safePosition;
            StateChanged?.Invoke();
            return true;
        }

        public bool SetChaseElapsedSeconds(float seconds)
        {
            float clamped = Math.Max(0f, seconds);
            if (Math.Abs(_state.chaseElapsedSeconds - clamped) < .01f) return false;
            _state.chaseElapsedSeconds = clamped;
            StateChanged?.Invoke();
            return true;
        }

        public void SetScene(string sceneId)
        {
            string normalized = RequireId(sceneId, nameof(sceneId));
            if (_state.sceneId == normalized) return;
            _state.sceneId = normalized;
            StateChanged?.Invoke();
        }

        public void SetObjective(string objectiveId)
        {
            string normalized = RequireId(objectiveId, nameof(objectiveId));
            if (_state.objectiveId == normalized) return;
            _state.objectiveId = normalized;
            StateChanged?.Invoke();
        }

        public bool AdvanceStory(StoryStage nextStage)
        {
            if (nextStage <= _state.storyStage) return false;
            _state.storyStage = nextStage;
            StateChanged?.Invoke();
            return true;
        }

        public void Restore(GameStateData data)
        {
            _state = data?.Clone() ?? throw new ArgumentNullException(nameof(data));
            EnsureCollections();
            StateChanged?.Invoke();
        }

        private bool AddUnique(List<string> collection, string id)
        {
            string normalized = RequireId(id, nameof(id));
            if (Contains(collection, normalized)) return false;
            collection.Add(normalized);
            StateChanged?.Invoke();
            return true;
        }

        private bool SetFlag(ref bool field, bool value)
        {
            if (field == value) return false;
            field = value;
            StateChanged?.Invoke();
            return true;
        }

        private static bool Contains(List<string> collection, string id)
        {
            if (collection == null || string.IsNullOrWhiteSpace(id)) return false;
            return collection.Contains(StableId.Normalize(id));
        }

        private static string RequireId(string value, string parameterName)
        {
            string normalized = StableId.Normalize(value);
            if (!StableId.IsValidValue(normalized)) throw new ArgumentException("Stable ID must use uppercase ASCII letters, digits, and underscores.", parameterName);
            return normalized;
        }

        private void EnsureCollections()
        {
            _state.collectedKeys ??= new List<string>();
            _state.collectedItems ??= new List<string>();
            _state.readClues ??= new List<string>();
            _state.openDoors ??= new List<string>();
        }
    }
}
