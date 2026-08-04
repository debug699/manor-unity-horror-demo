using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manor.Core
{
    [Serializable]
    public sealed class GameStateData
    {
        public int saveVersion = 1;
        public string sceneId = ProjectIds.SceneManorDemo;
        public StoryStage storyStage = StoryStage.Awake;
        public string objectiveId = ProjectIds.ObjectiveFindWhereIAm;
        public List<string> collectedKeys = new List<string>();
        public List<string> readClues = new List<string>();
        public List<string> openDoors = new List<string>();
        public Vector3 lastSafePosition;
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
                readClues = new List<string>(readClues ?? new List<string>()),
                openDoors = new List<string>(openDoors ?? new List<string>()),
                lastSafePosition = lastSafePosition,
                lastSaveUtc = lastSaveUtc
            };
        }
    }
}
