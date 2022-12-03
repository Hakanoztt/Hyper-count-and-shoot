using System.Collections.Generic;

namespace ElephantSDK
{
    public class MonitoringUtils
    {
        private static MonitoringUtils _instance;

        private readonly List<double> _fpsSessionLog;
        private readonly List<int> _currentLevelLog;

        private int _currentLevel;

        private MonitoringUtils()
        {
            _fpsSessionLog = new List<double>();
            _currentLevelLog = new List<int>();
            _currentLevel = -1;
        }

        public static MonitoringUtils GetInstance()
        {
            return _instance ?? (_instance = new MonitoringUtils());
        }

        public void LogFps(double fpsValue)
        {
            _fpsSessionLog.Add(fpsValue);
        }

        public void LogCurrentLevel()
        {
            _currentLevelLog.Add(_currentLevel);
        }

        public List<double> GetFpsSessionLog()
        {
            return _fpsSessionLog;
        }
        
        public List<int> GetCurrentLevelLog()
        {
            return _currentLevelLog;
        }

        public void SetCurrentLevel(int currentLevel)
        {
            _currentLevel = currentLevel;
        }

        public int GetCurrentLevel()
        {
            return _currentLevel;
        }

        public void Flush()
        {
            _fpsSessionLog?.Clear();
            _currentLevelLog?.Clear();
        }
    }
}