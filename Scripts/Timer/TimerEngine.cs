﻿using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using UnityUtils.LowLevel;

namespace ImprovedTimers
{
    internal static class TimerBootstrapper
    {
        static PlayerLoopSystem timerSystem;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        [System.Obsolete]
        internal static void Initialize() 
        {
            PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();

            if (!InsertTimerManager<Update>(ref currentPlayerLoop, 0))
            {
                Debug.LogWarning("Improved Timers Not initialized, unable to register TimerManager into the Update loop.");
                return;
            }
            PlayerLoop.SetPlayerLoop(currentPlayerLoop);
            PlayerLoopUtils.PrintPlayerLoop(currentPlayerLoop);

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged -= OnPlayModeState;
            EditorApplication.playModeStateChanged += OnPlayModeState;

            static void OnPlayModeState(PlayModeStateChange state)
            {
                if (state == PlayModeStateChange.ExitingPlayMode)
                {
                    PlayerLoopSystem currentPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
                    RemoveTimerManager<Update>(ref currentPlayerLoop);
                    PlayerLoop.SetPlayerLoop(currentPlayerLoop);

                    TimerManager.Clear();
                }
            }

#endif
        }

        static void RemoveTimerManager<T>(ref PlayerLoopSystem loop)
        {
            PlayerLoopUtils.RemoveSystem<T>(ref loop, in timerSystem);
        }

        static bool InsertTimerManager<T> (ref PlayerLoopSystem loop, int index)
        {
            timerSystem = new PlayerLoopSystem()
            {
                type = typeof(TimerManager),
                updateDelegate = TimerManager.UpdateTimers,
                subSystemList = null
            };
            return PlayerLoopUtils.InsertSystem<T>(ref loop, in timerSystem, index);
        }
    }
}
