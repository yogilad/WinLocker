using System;
using System.Threading;
using System.Threading.Tasks;


namespace WinLocker
{
    class Locker
    {
        private SessionStatus m_sessionStat = new SessionStatus();
        private const int m_timeIntervalMilliSeconds = 1000;
        private const int m_minLockTimeSeconds = 60;
        private const int m_aboutToLockTime = 20;
        private int m_lockTimeSeconds = 5 * 60;
        private bool m_aboutToLockFiredState = false;
        private LockerState m_state = LockerState.Active;
        private DateTime m_suspendUntil;

        public event EventHandler StateChangedEvent;
        public event EventHandler AboutToLockEvent;

        public class StateChangedEventArgs : EventArgs
        {
            public bool Automatic { set; get; }
        }
        public class AboutToLockEventArgs : EventArgs
        {
            public int Seconds { set; get; }
            public bool ShowMessage { set; get; }
        }

        public int LockTimeSeconds
        {
            get => m_lockTimeSeconds;
            set
            {
                if (value >= m_minLockTimeSeconds)
                    m_lockTimeSeconds = value;
            }
        }

        public int AboutToLockTime => m_aboutToLockTime;

        public DateTime SuspendTime => m_suspendUntil;
        public LockerState State => m_state;
        public void Enable()
        {
            m_state = LockerState.Active;
            FireStateChangedEvent(false);
        }

        public void Disable()
        {
            m_state = LockerState.Inactive;
            FireStateChangedEvent(false);
        }

        public void Suspend(int minutes)
        {
            if (minutes < 1)
                return;

            m_suspendUntil = DateTime.Now + TimeSpan.FromMinutes(minutes);
            m_state = LockerState.StandBy;
            FireStateChangedEvent(false);
        }

        public void ToggleState(int suspendMinutes = 0)
        {
            switch (m_state)
            {
                case LockerState.Active:
                    if (suspendMinutes <= 0)
                    {
                        m_state = LockerState.Inactive;
                        FireStateChangedEvent(false);
                    }
                    else
                    {
                        Suspend(suspendMinutes);
                    }

                    break;

                case LockerState.Inactive:
                    m_state = LockerState.Active;
                    FireStateChangedEvent(false);
                    break;

                case LockerState.StandBy:
                    m_state = LockerState.Active;
                    FireStateChangedEvent(false);
                    break;
            }
        }

        private void FireStateChangedEvent(bool automatic)
        {
            var args = new StateChangedEventArgs() { Automatic = automatic};

            StateChangedEvent?.Invoke(this, args);
        }

        private void FireAboutToLockEventIfStateChanged(bool showState)
        {
            if (m_aboutToLockFiredState != showState)
            {
                var args = new AboutToLockEventArgs() { Seconds = m_aboutToLockTime, ShowMessage = showState };

                AboutToLockEvent?.Invoke(this, args);
                m_aboutToLockFiredState = showState;
            }
        }

        public void RunAsync()
        {
            var runner = new Task(Run);

            runner.Start();
        }

        private async void Run()
        {
            while (true)
            {
                bool checkIdleTime = true;

                if (m_sessionStat.IsLocked)
                {
                    Console.WriteLine("Desktop is locked");
                    checkIdleTime = false;
                }

                switch (m_state)
                {
                    case LockerState.Inactive:
                        Console.WriteLine("WinLocker is inactive");
                        checkIdleTime = false;
                        break;

                    case LockerState.StandBy:
                        if (m_suspendUntil > DateTime.Now)
                        {
                            Console.WriteLine("WinLocker is in Standby mode until {0}", m_suspendUntil);
                            checkIdleTime = false;
                        }
                        else
                        {
                            Console.WriteLine("WinLocker left standby mode");
                            m_state = LockerState.Active;
                            FireStateChangedEvent(true);
                        }
                        
                        break;
                }

                if (checkIdleTime)
                {
                    var idleTime = InputTimer.GetInputIdleTime();

                    Console.WriteLine("Desktop is idle for {0}", idleTime.ToString());
                    if (idleTime.TotalSeconds >= m_lockTimeSeconds - m_aboutToLockTime)
                    {
                        FireAboutToLockEventIfStateChanged(true);

                        if (idleTime.TotalSeconds >= m_lockTimeSeconds)
                        {
                            Console.WriteLine("Locking Desktop");
                            SessionStatus.LockWorkStation();
                        }
                    }
                    else 
                    {
                        FireAboutToLockEventIfStateChanged(false);
                    }
                }

                await Task.Delay(m_timeIntervalMilliSeconds);
            }
        }
    }
}