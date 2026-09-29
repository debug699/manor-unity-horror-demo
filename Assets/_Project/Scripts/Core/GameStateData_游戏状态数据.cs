using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manor.Core
{
    [Serializable]
    public sealed class GameStateData
    {
        public int saveVersion = 2;
        public string sceneId = ProjectIds.SceneManorDemo;
        public StoryStage storyStage = StoryStage.Awake;
        public string objectiveId = ProjectIds.ObjectiveFindWhereIAm;
        public List<string> collectedKeys = new List<string>();
        public List<string> collectedItems = new List<string>();
        public List<string> readClues = new List<string>();
        public List<string> openDoors = new List<string>();
        public bool gatePasswordKnown;
        public bool bridgeRestored;
        public bool gateUnlocked;
        public bool communicationComplete;
        public bool butcherChaseStarted;
        public float chaseElapsedSeconds;
        public bool dawnTriggered;
        public Vector3 lastSafePosition = new Vector3(0f, .05f, 45.5f);
        public string checkpointId = "CHECKPOINT_GATE_START";
        public string lastSaveUtc = string.Empty;

        public GameStateData Clone()
        {
            return new GameStateData
            {
                saveVersion = saveVersion,
                sceneId = sceneId,
                storyStage = storyStage,
                objectiveId = objectiveId,
                collectedKeys = new List<string>(collectedKeys ?? new List<string>()),
                collectedItems = new List<string>(collectedItems ?? new List<string>()),
                readClues = new List<string>(readClues ?? new List<string>()),
                openDoors = new List<string>(openDoors ?? new List<string>()),
                gatePasswordKnown = gatePasswordKnown,
                bridgeRestored = bridgeRestored,
                gateUnlocked = gateUnlocked,
                communicationComplete = communicationComplete,
                butcherChaseStarted = butcherChaseStarted,
                chaseElapsedSeconds = chaseElapsedSeconds,
                dawnTriggered = dawnTriggered,
                lastSafePosition = lastSafePosition,
                checkpointId = checkpointId,
                lastSaveUtc = lastSaveUtc
            };
        }
    }
}
