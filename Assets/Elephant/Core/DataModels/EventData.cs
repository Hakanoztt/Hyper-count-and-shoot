using System;

namespace ElephantSDK
{
    [Serializable]
    public class EventData : BaseData 
    {
        public string type;
        public int level;
        
        public string  key_string1;
        public string  value_string1;
        public string  key_string2;
        public string  value_string2;
        public string key_string3;
        public string  value_string3;
        public string  key_string4;
        public string  value_string4;

        public string  key_int1;
        public int  value_int1;
        public string  key_int2;
        public int  value_int2;
        public string  key_int3;
        public int  value_int3;
        public string  key_int4;
        public int  value_int4;

        public string  key_double1;
        public double  value_double1;
        public string  key_double2;
        public double  value_double2;
        public string  key_double3;
        public double  value_double3;
        public string  key_double4;
        public double  value_double4;
        
        public string custom_data;
        
        private EventData()
        {
            
        }

        public static EventData CreateEventData()
        {
            var a = new EventData();
            a.FillBaseData(ElephantCore.Instance.GetCurrentSession().GetSessionID());
            return a;
        }
    }
}
