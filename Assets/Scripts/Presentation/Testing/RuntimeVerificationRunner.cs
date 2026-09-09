using System;
using KOA.Core.AI;
using UnityEngine;

namespace KOA.Presentation.Testing
{
    /// <summary>
    /// Runtime gate สำหรับ Windows Development Build: เร่งเวลาเกมให้ครบ 20 นาที
    /// แล้วออกด้วย exit code ที่ CI อ่านได้ โดยเปิดใช้เฉพาะเมื่อส่ง -koaVerifyRuntime
    /// </summary>
    public sealed class RuntimeVerificationRunner : MonoBehaviour
    {
        public const string CommandLineFlag = "-koaVerifyRuntime";
        private const float DefaultTargetSimulatedSeconds = 20f * 60f;
        private const float VerificationTimeScale = 60f;
        private const float RealTimeTimeoutSeconds = 120f;

        private float _realStartTime;
        private int _frameCount;
        private float _targetSimulatedSeconds = DefaultTargetSimulatedSeconds;
        private BotDifficulty _requestedDifficulty = BotDifficulty.Medium;
        private bool _difficultyConfigured;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void InstallForVerification()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), CommandLineFlag) < 0) return;
            var runner = new GameObject("[KOA_RuntimeVerification]");
            DontDestroyOnLoad(runner);
            runner.AddComponent<RuntimeVerificationRunner>();
        }

        private void Start()
        {
            foreach (string argument in Environment.GetCommandLineArgs())
            {
                const string prefix = "-koaBotDifficulty=";
                if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                    && Enum.TryParse(argument.Substring(prefix.Length), true, out BotDifficulty parsed))
                {
                    _requestedDifficulty = parsed;
                }

                const string durationPrefix = "-koaTargetSeconds=";
                if (argument.StartsWith(durationPrefix, StringComparison.OrdinalIgnoreCase)
                    && float.TryParse(argument.Substring(durationPrefix.Length), System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out float requestedDuration))
                {
                    _targetSimulatedSeconds = Mathf.Max(DefaultTargetSimulatedSeconds, requestedDuration);
                }
            }

            _realStartTime = Time.realtimeSinceStartup;
            Time.timeScale = VerificationTimeScale;
            Debug.Log($"KOA_RUNTIME_VERIFICATION:START target={_targetSimulatedSeconds:F0}s scale={VerificationTimeScale:F0}x difficulty={_requestedDifficulty}");
        }

        private void Update()
        {
            _frameCount++;
            float realElapsed = Time.realtimeSinceStartup - _realStartTime;

            var bootstrap = VerticalSliceBootstrap.Instance;
            if (!_difficultyConfigured && bootstrap != null && bootstrap.BotBrain != null)
            {
                bootstrap.BotBrain.Difficulty = _requestedDifficulty;
                _difficultyConfigured = true;
            }

            if (Time.timeSinceLevelLoad >= _targetSimulatedSeconds)
            {
                Complete(realElapsed);
                return;
            }

            if (realElapsed >= RealTimeTimeoutSeconds)
                Fail($"timeout after {realElapsed:F1}s; simulated={Time.timeSinceLevelLoad:F1}s");
        }

        private void Complete(float realElapsed)
        {
            var bootstrap = VerticalSliceBootstrap.Instance;
            if (bootstrap == null || bootstrap.MatchSimulation == null || bootstrap.CurrentPlayerHero == null || bootstrap.BotHero == null)
            {
                Fail("bootstrap or core match state is missing");
                return;
            }
            if (!_difficultyConfigured || bootstrap.BotBrain == null || bootstrap.BotBrain.Difficulty != _requestedDifficulty)
            {
                Fail($"requested bot difficulty {_requestedDifficulty} was not configured");
                return;
            }
            if (!bootstrap.MatchSimulation.IsGameOver)
            {
                Fail($"match did not reach win/lose state at difficulty {_requestedDifficulty}");
                return;
            }

            float averageFps = realElapsed > 0f ? _frameCount / realElapsed : 0f;
            Debug.Log(
                $"KOA_RUNTIME_VERIFICATION:PASS simulated={Time.timeSinceLevelLoad:F1}s real={realElapsed:F1}s " +
                $"avgFps={averageFps:F1} minions={bootstrap.MatchSimulation.ActiveMinions.Count} gameOver={bootstrap.MatchSimulation.IsGameOver} difficulty={_requestedDifficulty}");
            Time.timeScale = 1f;
            Application.Quit(0);
        }

        private static void Fail(string reason)
        {
            Debug.LogError($"KOA_RUNTIME_VERIFICATION:FAIL {reason}");
            Time.timeScale = 1f;
            Application.Quit(1);
        }
    }
}
