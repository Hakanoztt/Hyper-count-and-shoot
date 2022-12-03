namespace ElephantSDK
{
    public class IdfaConsentResult
    {
        private static IdfaConsentResult _instance;
        private Status _status = Status.Waiting;
        private string _idfaResultValue = "";

        public enum Status
        {
            Waiting = 0,
            Resolved = 1
        }

        public static IdfaConsentResult GetInstance()
        {
            return _instance ?? (_instance = new IdfaConsentResult());
        }

        public void SetStatus(Status status)
        {
            _status = status;
        }

        public Status GetStatus()
        {
            return _status;
        }

        public void SetIdfaResultValue(string value)
        {
            _idfaResultValue = value;
        }

        public string GetIdfaResultValue()
        {
            return _idfaResultValue;
        }
    }
}