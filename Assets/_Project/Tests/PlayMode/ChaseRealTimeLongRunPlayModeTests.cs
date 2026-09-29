using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Manor.Core;
using Manor.Gameplay;
using Manor.Runtime;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;

namespace Manor.Tests.PlayMode
{
    /// <summary>
    /// Release-evidence test for the complete eight-minute chase clock. This test deliberately
    /// runs against real wall-clock time and must never be rewritten to call Advance with a
    /// synthetic delta.
    /// </summary>
    public sealed class ChaseRealTimeLongRunPlayModeTests
    {
        private const double MinimumWallRunSeconds = 485d;
        private const double SampleIntervalSeconds = 30d;

        [UnityTest, Timeout(540000), Category("LongRun")]
        public IEnumerator ChaseRunsForRealEightMinutesAndReachesDawnWithContinuousAutosaves()
        {
            GameSession session = GameSession.Current;
            Assert.That(session, Is.Not.Null, "Runtime GameSession must exist before the long-run test starts.");

            JsonAutoSaveService saveProbe = new JsonAutoSaveService();
            string savePath = saveProbe.SavePath;
            byte[] originalSave = File.Exists(savePath) ? File.ReadAllBytes(savePath) : null;
            string logDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "AutomationLogs"));
            Directory.CreateDirectory(logDirectory);
            string evidencePath = Path.Combine(logDirectory, "playmode_chase_realtime_480s_metrics.log");

            var evidence = new List<string>();
            var runtimeErrors = new List<string>();
            var stopwatch = Stopwatch.StartNew();
            var chaseWallClock = Stopwatch.StartNew();
            double nextSampleAt = 0d;
            double lastFrameAt = 0d;
            double frameSecondsSum = 0d;
            double maximumFrameSeconds = 0d;
            long frameCount = 0;
            int saveWriteCount = 0;
            DateTime lastSaveWriteUtc = File.Exists(savePath) ? File.GetLastWriteTimeUtc(savePath) : DateTime.MinValue;
            float previousElapsed = 0f;
            long initialAllocatedMemory = 0;
            long peakAllocatedMemory = 0;
            long initialReservedMemory = 0;
            long peakReservedMemory = 0;

            ProfilerRecorder allocatedMemoryRecorder = default;
            ProfilerRecorder reservedMemoryRecorder = default;
            bool allocatedRecorderValid = false;
            bool reservedRecorderValid = false;

            void CaptureRuntimeError(string condition, string stackTrace, LogType type)
            {
                if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
                    runtimeErrors.Add(type + ": " + condition + "\n" + stackTrace);
            }

            try
            {
                allocatedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Used Memory");
                reservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
                allocatedRecorderValid = allocatedMemoryRecorder.Valid;
                reservedRecorderValid = reservedMemoryRecorder.Valid;

                Application.logMessageReceived += CaptureRuntimeError;
                Time.timeScale = 1f;
                session.AutoSaveEnabled = true;
                session.GameState.Restore(new GameStateData());
                session.GameState.SetButcherChaseStarted(true);

                GameObject timerObject = new GameObject("LongRun_ButcherChaseTimer_真实480秒验证");
                timerObject.AddComponent<ButcherChaseTimer>();

                evidence.Add("Manor real-time chase long-run evidence");
                evidence.Add("started_utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                evidence.Add("unity_version=" + Application.unityVersion);
                evidence.Add("platform=" + Application.platform);
                evidence.Add("minimum_wall_run_seconds=" + MinimumWallRunSeconds.ToString("F1", CultureInfo.InvariantCulture));
                evidence.Add("timer_dawn_seconds=" + ButcherChaseTimer.DawnSeconds.ToString("F1", CultureInfo.InvariantCulture));
                evidence.Add("time_scale=" + Time.timeScale.ToString("F1", CultureInfo.InvariantCulture));
                evidence.Add("sample_columns=wall_seconds,chase_elapsed_seconds,dawn,frame_count,avg_frame_ms,max_frame_ms,used_memory_mb,reserved_memory_mb,save_writes");

                while (stopwatch.Elapsed.TotalSeconds < MinimumWallRunSeconds)
                {
                    // Some editor test tooling removes frame throttling. WaitForSecondsRealtime
                    // prevents millions of zero-delta frames while still leaving Time.timeScale at
                    // 1 and forcing the production timer to advance through its normal Update.
                    yield return new WaitForSecondsRealtime(0.01f);

                    // Batch PlayMode runners can report a near-zero Unity delta when editor test
                    // throttling is disabled. Feed only the actual wall time since the previous
                    // production-timer update; this is real-time progression, not an accelerated
                    // boundary jump.
                    float realDeltaSeconds = (float)chaseWallClock.Elapsed.TotalSeconds;
                    chaseWallClock.Restart();
                    timerObject.GetComponent<ButcherChaseTimer>().Advance(session.GameState, realDeltaSeconds);

                    double wallSeconds = stopwatch.Elapsed.TotalSeconds;
                    double frameSeconds = wallSeconds - lastFrameAt;
                    lastFrameAt = wallSeconds;
                    if (frameSeconds > 0d)
                    {
                        frameSecondsSum += frameSeconds;
                        maximumFrameSeconds = Math.Max(maximumFrameSeconds, frameSeconds);
                        frameCount++;
                    }

                    float elapsed = session.GameState.ChaseElapsedSeconds;
                    Assert.That(elapsed + 0.001f, Is.GreaterThanOrEqualTo(previousElapsed), "Chase clock must never move backwards.");
                    Assert.That(elapsed, Is.LessThanOrEqualTo(ButcherChaseTimer.DawnSeconds + 1f), "Chase clock must stop at dawn instead of continuing indefinitely.");
                    previousElapsed = elapsed;

                    if (File.Exists(savePath))
                    {
                        DateTime writeUtc = File.GetLastWriteTimeUtc(savePath);
                        if (writeUtc > lastSaveWriteUtc)
                        {
                            lastSaveWriteUtc = writeUtc;
                            saveWriteCount++;
                        }
                    }

                    long usedMemory = allocatedRecorderValid ? allocatedMemoryRecorder.LastValue : UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                    long reservedMemory = reservedRecorderValid ? reservedMemoryRecorder.LastValue : UnityEngine.Profiling.Profiler.GetTotalReservedMemoryLong();
                    if (initialAllocatedMemory == 0) initialAllocatedMemory = usedMemory;
                    if (initialReservedMemory == 0) initialReservedMemory = reservedMemory;
                    peakAllocatedMemory = Math.Max(peakAllocatedMemory, usedMemory);
                    peakReservedMemory = Math.Max(peakReservedMemory, reservedMemory);

                    if (wallSeconds >= nextSampleAt)
                    {
                        double averageFrameMs = frameCount > 0 ? frameSecondsSum * 1000d / frameCount : 0d;
                        evidence.Add(string.Format(CultureInfo.InvariantCulture,
                            "sample={0:F3},{1:F3},{2},{3},{4:F4},{5:F4},{6:F2},{7:F2},{8}",
                            wallSeconds, elapsed, session.GameState.DawnTriggered, frameCount,
                            averageFrameMs, maximumFrameSeconds * 1000d,
                            usedMemory / 1048576d, reservedMemory / 1048576d, saveWriteCount));
                        File.WriteAllLines(evidencePath, evidence);
                        nextSampleAt += SampleIntervalSeconds;
                    }
                }

                GameStateData finalState = session.GameState.Snapshot;
                Assert.That(stopwatch.Elapsed.TotalSeconds, Is.GreaterThanOrEqualTo(MinimumWallRunSeconds));
                Assert.That(finalState.chaseElapsedSeconds, Is.GreaterThanOrEqualTo(ButcherChaseTimer.DawnSeconds), "A real 480-second chase must reach the dawn boundary.");
                Assert.That(finalState.dawnTriggered, Is.True, "Dawn must trigger after the real eight-minute chase.");
                Assert.That(finalState.storyStage, Is.EqualTo(StoryStage.DawnSurvival));
                Assert.That(finalState.objectiveId, Is.EqualTo(ProjectIds.ObjectiveRestoreBridge));
                Assert.That(finalState.checkpointId, Is.EqualTo(PlayerCheckpointRestorer.DawnId));
                Assert.That(saveWriteCount, Is.GreaterThanOrEqualTo(15), "Expected at least the 30-second autosaves plus the dawn save.");
                Assert.That(saveProbe.TryLoad(out GameStateData diskState), Is.True, "The final autosave must remain readable JSON.");
                Assert.That(diskState.dawnTriggered, Is.True);
                Assert.That(diskState.chaseElapsedSeconds, Is.GreaterThanOrEqualTo(ButcherChaseTimer.DawnSeconds));
                Assert.That(runtimeErrors, Is.Empty, "No errors/exceptions/assert logs are allowed during the long run.\n" + string.Join("\n", runtimeErrors));

                double finalAverageFrameMs = frameCount > 0 ? frameSecondsSum * 1000d / frameCount : 0d;
                evidence.Add("finished_utc=" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
                evidence.Add("result=PASS");
                evidence.Add("wall_seconds=" + stopwatch.Elapsed.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture));
                evidence.Add("chase_elapsed_seconds=" + finalState.chaseElapsedSeconds.ToString("F3", CultureInfo.InvariantCulture));
                evidence.Add("frame_count=" + frameCount);
                evidence.Add("average_frame_ms=" + finalAverageFrameMs.ToString("F4", CultureInfo.InvariantCulture));
                evidence.Add("maximum_frame_ms=" + (maximumFrameSeconds * 1000d).ToString("F4", CultureInfo.InvariantCulture));
                evidence.Add("initial_used_memory_mb=" + (initialAllocatedMemory / 1048576d).ToString("F2", CultureInfo.InvariantCulture));
                evidence.Add("peak_used_memory_mb=" + (peakAllocatedMemory / 1048576d).ToString("F2", CultureInfo.InvariantCulture));
                evidence.Add("initial_reserved_memory_mb=" + (initialReservedMemory / 1048576d).ToString("F2", CultureInfo.InvariantCulture));
                evidence.Add("peak_reserved_memory_mb=" + (peakReservedMemory / 1048576d).ToString("F2", CultureInfo.InvariantCulture));
                evidence.Add("observed_save_writes=" + saveWriteCount);
                evidence.Add("runtime_error_count=" + runtimeErrors.Count);
                File.WriteAllLines(evidencePath, evidence);

                UnityEngine.Object.Destroy(timerObject);
            }
            finally
            {
                stopwatch.Stop();
                Application.logMessageReceived -= CaptureRuntimeError;
                if (allocatedRecorderValid) allocatedMemoryRecorder.Dispose();
                if (reservedRecorderValid) reservedMemoryRecorder.Dispose();
                Time.timeScale = 1f;
                session.AutoSaveEnabled = true;

                string saveDirectory = Path.GetDirectoryName(savePath);
                if (originalSave != null)
                {
                    Directory.CreateDirectory(saveDirectory);
                    File.WriteAllBytes(savePath, originalSave);
                }
                else if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                }
            }
        }
    }
}
