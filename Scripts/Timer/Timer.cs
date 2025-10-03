using System;
using UnityEngine;

namespace ImprovedTimers
{
    public class CountdownTimer : Timer
    {
        public CountdownTimer(float value) : base(value) 
        {
            
        }

        public override void Tick()
        {
            if (IsRunning && CurrentTime > 0)
            {
                CurrentTime -= Time.deltaTime;
            }

            if (IsRunning && CurrentTime <= 0)
            {
                Stop();
            }
        }
        public override bool IsFinished => CurrentTime <= 0;
    }
    public abstract class Timer : IDisposable
    {   
        public float CurrentTime { get; protected set; }
        public bool IsRunning { get; protected set; }

        protected float initialTime;

        public float Progress => Mathf.Clamp(CurrentTime / initialTime, 0f, 1f);

        public Action OnTimerStart = delegate { Debug.Log($"{Time.deltaTime} secs"); };
        public Action OnTimerStop = delegate { };

        protected Timer(float value)
        {
            initialTime = value;
        }

        public void Start()
        {
            CurrentTime = initialTime;
            if (!IsRunning)
            {
                IsRunning = true;
                TimerManager.RegisterTimer(this);
                OnTimerStart.Invoke();
            }
        }

        public void Stop()
        {
            if (IsRunning)
            {
                IsRunning = false;
                TimerManager.DeregisterTimer(this);
                OnTimerStop.Invoke();
            }
        }

        public abstract void Tick();
        public abstract bool IsFinished { get; }

        public void Resume() => IsRunning = true;
        public void Pause() => IsRunning = false;

        public virtual void Reset() => CurrentTime = initialTime;
        public virtual void Reset(float newTime)
        {
            initialTime = newTime;
            Reset();
        }

        bool disposed;

        ~Timer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;

            if (disposing)
            {
                TimerManager.DeregisterTimer(this);
            }

            disposed = true;
        }
    }
}
