using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class ElephantIOS 
{
   
    #if UNITY_IOS
    [DllImport ("__Internal")]
    public static extern void TestFunction(string a);
    
    [DllImport ("__Internal")]
    public static extern string IDFA();
    
    [DllImport ("__Internal")]
    public static extern void ElephantPost(string url, string body, string gameID, string authToken, int tryCount);
    
    [DllImport ("__Internal")]
    public static extern void showIdfaConsent(int type, int delay, int position, string titleText, string descriptionText, 
        string buttonText, string termsText, string policyText, string termsUrl, string policyUrl);
    
    [DllImport ("__Internal")]
    public static extern string getConsentStatus();
    
    [DllImport ("__Internal")]
    public static extern string getBuildNumber();
    
    [DllImport ("__Internal")]
    public static extern void showAlertDialog(string title, string message);
    
    [DllImport ("__Internal")]
    public static extern void showForceUpdate(string title, string message);
    
    #endif
}
