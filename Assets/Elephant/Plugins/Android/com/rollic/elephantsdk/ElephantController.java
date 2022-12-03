package com.rollic.elephantsdk;

import android.app.AlertDialog;
import android.content.Context;
import android.content.DialogInterface;
import android.content.Intent;
import android.net.Uri;
import android.text.Html;
import android.text.method.LinkMovementMethod;
import android.util.Log;
import android.widget.TextView;

import com.android.volley.AuthFailureError;
import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.Response;
import com.android.volley.VolleyError;
import com.android.volley.toolbox.StringRequest;
import com.android.volley.toolbox.Volley;
import com.google.android.gms.ads.identifier.AdvertisingIdClient;
import com.google.android.gms.common.GooglePlayServicesNotAvailableException;
import com.google.android.gms.common.GooglePlayServicesRepairableException;

import org.json.JSONException;
import org.json.JSONObject;

import java.io.IOException;
import java.lang.reflect.Field;
import java.nio.charset.StandardCharsets;
import java.util.Map;
import java.util.HashMap;

public class ElephantController {

    private static final String LOG_TAG = "[ELEPHANT SDK]";
    private RequestQueue queue;

    private Context ctx;

    private ElephantController(Context ctx) {
        this.ctx = ctx;
        this.queue = Volley.newRequestQueue(this.ctx);
    }


    public static ElephantController create(Context ctx) {
        return new ElephantController(ctx);
    }


    public void ElephantPost(final String url, final String body, final String gameID, final String authToken, int _tryCount) {

        try {
            final int tryCount = _tryCount + 1;
            StringRequest stringRequest = new StringRequest(Request.Method.POST, url,
                    new Response.Listener<String>() {
                        @Override
                        public void onResponse(String response) {
                            // Display the first 500 characters of the response string.
                            Log.e(LOG_TAG, "onResponse: " + response);
                        }
                    }, new Response.ErrorListener() {
                @Override
                public void onErrorResponse(VolleyError error) {

                    try {
                        JSONObject failedReq = new JSONObject();
                        failedReq.accumulate("url", url);
                        failedReq.accumulate("data", body);
                        failedReq.accumulate("tryCount", tryCount);
                        com.unity3d.player.UnityPlayer.UnitySendMessage("Elephant","FailedRequest", failedReq.toString());
                    } catch (JSONException e) {
                        e.printStackTrace();
                    }

                    Log.e(LOG_TAG, "error: " + error.networkResponse);
                }
            }) {

                @Override
                public Map<String, String> getHeaders() throws AuthFailureError {
                    Map<String, String> headers = new HashMap<>();
                    headers.put("Content-Type", "application/json; charset=utf-8");
                    headers.put("Authorization", authToken);
                    headers.put("GameID", gameID);
                    return headers;
                }

                @Override
                public byte[] getBody() throws AuthFailureError {
                    return body.getBytes(StandardCharsets.UTF_8);
                }
            };


            queue.add(stringRequest);

        }catch (Exception e){
            e.printStackTrace();
        }

    }
    
    public void showAlertDialog(String title, String message) {
        if (message.contains("{{tos}}")) {
            message = message.replace("{{tos}}", "<a href=\"" + title +"\">Terms of Service</a>");

            AlertDialog alertDialog = new AlertDialog.Builder(ctx)
                    .setTitle(title)
                    .setMessage(Html.fromHtml(message))
                    .setCancelable(true)
                    .setPositiveButton(android.R.string.yes, new DialogInterface.OnClickListener() {
                        public void onClick(DialogInterface dialog, int which) {
                            dialog.dismiss();
                        }
                    }).show();

            ((TextView) alertDialog.findViewById(android.R.id.message)).setMovementMethod(LinkMovementMethod.getInstance());
        } else {
            new AlertDialog.Builder(ctx)
                    .setTitle(title)
                    .setMessage(message)
                    .setCancelable(true)
                    .setPositiveButton(android.R.string.yes, new DialogInterface.OnClickListener() {
                        public void onClick(DialogInterface dialog, int which) {
                            dialog.dismiss();
                        }
                    }).show();
        }
    }

    public void showForceUpdate(String title, String message) {
        new AlertDialog.Builder(ctx)
                .setTitle(title)
                .setMessage(message)
                .setCancelable(false)
                .setPositiveButton(android.R.string.yes, new DialogInterface.OnClickListener() {
                    public void onClick(DialogInterface dialog, int which) {
                        final String appPackageName = ctx.getPackageName();
                        try {
                            ctx.startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse("market://details?id=" + appPackageName)));
                        } catch (android.content.ActivityNotFoundException anfe) {
                            ctx.startActivity(new Intent(Intent.ACTION_VIEW, Uri.parse("https://play.google.com/store/apps/details?id=" + appPackageName)));
                        }

                    }
                }).show();

    }

    public String getBuildNumber() {
        if (getBuildConfigValue() == null) {
            return "";
        }

        return getBuildConfigValue() + "";
    }
    
    private Object getBuildConfigValue() {
        try {
            Class<?> clazz = Class.forName(ctx.getPackageName() + ".BuildConfig");
            Field field = clazz.getField("VERSION_CODE");
            return field.get(null);
        } catch (ClassNotFoundException e) {
            e.printStackTrace();
        } catch (NoSuchFieldException e) {
            e.printStackTrace();
        } catch (IllegalAccessException e) {
            e.printStackTrace();
        }
        return null;
    }

    public String FetchAdId() {
        String adId = "";

        try {
            AdvertisingIdClient.Info adIdInfo = AdvertisingIdClient.getAdvertisingIdInfo(ctx);
            if (adIdInfo == null) {
                return adId;
            }
         
            adId = adIdInfo.getId() != null ? adIdInfo.getId() : "";
            
        } catch (IOException e) {
            e.printStackTrace();
        } catch (GooglePlayServicesNotAvailableException e) {
            e.printStackTrace();
        } catch (GooglePlayServicesRepairableException e) {
            e.printStackTrace();
        }

        return adId;
    }

    public String test() {
        Log.e(LOG_TAG, "test called");

        return "Hello from Elephant android plugin ";
    }


}
