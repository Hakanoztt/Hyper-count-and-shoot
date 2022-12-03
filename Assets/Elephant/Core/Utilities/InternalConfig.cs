using System;

namespace ElephantSDK
{
    [Serializable]
    public class InternalConfig
    {
        private static InternalConfig _instance;

        public bool monitoring_enabled;
        public bool crash_log_enabled;
        public bool low_memory_logging_enabled;
        public bool idfa_consent_enabled;
        public int idfa_consent_type;
        public int idfa_consent_delay;
        public int idfa_consent_position;
        public string consent_text_body;
        public string consent_text_action_body;
        public string consent_text_action_button;
        public string terms_of_service_text;
        public string terms_of_service_url;
        public string privacy_policy_text;
        public string privacy_policy_url;
        public string min_app_version;
        public float focus_interval;

        private InternalConfig()
        {
            monitoring_enabled = true;
            crash_log_enabled = false;
            low_memory_logging_enabled = false;
            idfa_consent_enabled = false;
            idfa_consent_type = 3;
            idfa_consent_delay = 0;
            idfa_consent_position = 0;
            consent_text_body =
                "To play \"{{name}}\" you must agree to the {{terms}} and {{privacy}} from the developer of this app.";
            consent_text_action_body = "Press \"{{button}}\" to start playing!";
            consent_text_action_button = "Agree to Terms";
            terms_of_service_text = "Terms and Conditions";
            terms_of_service_url = "https://www.rollicgames.com/terms";
            privacy_policy_text = "Privacy Policy";
            privacy_policy_url = "https://www.rollicgames.com/privacy";
            min_app_version = "";
            focus_interval = 300;
        }

        public static InternalConfig GetInstance()
        {
            return _instance ?? (_instance = new InternalConfig());
        }
    }
}