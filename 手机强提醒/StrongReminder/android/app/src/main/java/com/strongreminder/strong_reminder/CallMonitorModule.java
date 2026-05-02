package com.strongreminder.strong_reminder;

import android.content.BroadcastReceiver;
import android.content.Context;
import android.content.Intent;
import android.content.IntentFilter;
import android.telephony.TelephonyManager;
import android.util.Log;

import com.facebook.react.bridge.LifecycleEventListener;
import com.facebook.react.bridge.ReactApplicationContext;
import com.facebook.react.bridge.ReactContextBaseJavaModule;
import com.facebook.react.modules.core.DeviceEventManagerModule;

import javax.annotation.Nonnull;

public class CallMonitorModule extends ReactContextBaseJavaModule implements LifecycleEventListener {

    private static final String TAG = "CallMonitorModule";
    private final ReactApplicationContext reactContext;
    private BroadcastReceiver callReceiver;
    private boolean isReceiverRegistered = false;

    public CallMonitorModule(@Nonnull ReactApplicationContext reactContext) {
        super(reactContext);
        this.reactContext = reactContext;
        reactContext.addLifecycleEventListener(this);
        registerCallReceiver();
    }

    @Nonnull
    @Override
    public String getName() {
        return "CallMonitor";
    }

    private void registerCallReceiver() {
        if (isReceiverRegistered) {
            return;
        }

        callReceiver = new BroadcastReceiver() {
            @Override
            public void onReceive(Context context, Intent intent) {
                try {
                    String state = intent.getStringExtra(TelephonyManager.EXTRA_STATE);
                    String phoneNumber = intent.getStringExtra(TelephonyManager.EXTRA_INCOMING_NUMBER);
                    
                    Log.d(TAG, "电话状态变化: " + state + ", 号码: " + phoneNumber);
                    
                    // 发送事件到JavaScript
                    if (state != null) {
                        com.facebook.react.bridge.WritableMap params = 
                            com.facebook.react.bridge.Arguments.createMap();
                        
                        // 转换状态为更易理解的格式
                        String normalizedState;
                        if (state.equals(TelephonyManager.EXTRA_STATE_RINGING)) {
                            normalizedState = "RINGING";
                        } else if (state.equals(TelephonyManager.EXTRA_STATE_OFFHOOK)) {
                            normalizedState = "OFFHOOK";
                        } else {
                            normalizedState = "IDLE";
                        }
                        
                        params.putString("state", normalizedState);
                        if (phoneNumber != null) {
                            params.putString("phoneNumber", phoneNumber);
                        }
                        
                        sendEvent("CallStateChanged", params);
                    }
                } catch (Exception e) {
                    Log.e(TAG, "处理电话状态变化时出错: " + e.getMessage());
                }
            }
        };

        IntentFilter filter = new IntentFilter();
        filter.addAction(TelephonyManager.ACTION_PHONE_STATE_CHANGED);
        reactContext.registerReceiver(callReceiver, filter);
        isReceiverRegistered = true;
        Log.d(TAG, "电话状态监听器已注册");
    }

    private void unregisterCallReceiver() {
        if (isReceiverRegistered && callReceiver != null) {
            try {
                reactContext.unregisterReceiver(callReceiver);
                isReceiverRegistered = false;
                Log.d(TAG, "电话状态监听器已注销");
            } catch (Exception e) {
                Log.e(TAG, "注销电话状态监听器时出错: " + e.getMessage());
            }
        }
    }

    private void sendEvent(String eventName, @javax.annotation.Nullable com.facebook.react.bridge.WritableMap params) {
        if (reactContext.hasActiveCatalystInstance()) {
            reactContext
                .getJSModule(DeviceEventManagerModule.RCTDeviceEventEmitter.class)
                .emit(eventName, params);
            Log.d(TAG, "事件已发送: " + eventName);
        } else {
            Log.d(TAG, "Catalyst实例未激活，无法发送事件: " + eventName);
        }
    }

    @Override
    public void onHostResume() {
        registerCallReceiver();
    }

    @Override
    public void onHostPause() {
        // 不注销，以便在后台也能监听电话
    }

    @Override
    public void onHostDestroy() {
        unregisterCallReceiver();
    }

    @Override
    public void onCatalystInstanceDestroy() {
        super.onCatalystInstanceDestroy();
        unregisterCallReceiver();
    }
}
