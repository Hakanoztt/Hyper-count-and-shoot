using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElephantAndroid
{
    #if UNITY_ANDROID
    
    private static AndroidJavaObject currentActivity = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
    private static AndroidJavaObject elephantController;
    
    public static void Init()
    {
        try
        {
            AndroidJavaClass elephantCoreClass = new AndroidJavaClass("com.rollic.elephantsdk.ElephantController");
            ElephantAndroid.elephantController = elephantCoreClass.CallStatic<AndroidJavaObject>("create", currentActivity);
        }
        catch (Exception ex)
        {
            Debug.Log(ex);
        }
        
    }
    
    public static void showAlertDialog(string title, string message)
    {
        if (elephantController != null)
        {
            elephantController.Call("showAlertDialog", title, message);    
        }
    }

    public static void showForceUpdate(string title, string message)
    {
        if (elephantController != null)
        {
            elephantController.Call("showForceUpdate", title, message);    
        }
    }

    public static void ElephantPost(string url, string body, string gameID, string authToken, int tryCount)
    {
        if (elephantController != null)
        {
            elephantController.Call("ElephantPost", url, body, gameID, authToken, tryCount);    
        }
    }

    public static string getBuildNumber()
    {
        var buildNumber = "";
        if (elephantController != null)
        {
            buildNumber = elephantController.Call<string>("getBuildNumber");    
        }

        return buildNumber;
    }
    
     public static string FetchAdId()
    {
        var adId = "";
        if (elephantController != null)
        {
            adId = elephantController.Call<string>("FetchAdId");
        }

        return adId;
    }
    
    #endif
    
}
