package com.strongreminder.strong_reminder;

import android.content.Context;
import android.media.AudioManager;
import android.os.Build;
import android.util.Log;

import androidx.annotation.NonNull;

import com.facebook.react.bridge.ReactApplicationContext;
import com.facebook.react.bridge.ReactContextBaseJavaModule;
import com.facebook.react.bridge.ReactMethod;

import javax.annotation.Nonnull;

public class AudioModeModule extends ReactContextBaseJavaModule {

    private static final String TAG = "AudioModeModule";
    private final ReactApplicationContext reactContext;
    private AudioManager audioManager;

    public AudioModeModule(@Nonnull ReactApplicationContext reactContext) {
        super(reactContext);
        this.reactContext = reactContext;
        initAudioManager();
    }

    @NonNull
    @Override
    public String getName() {
        return "AudioMode";
    }

    private void initAudioManager() {
        try {
            audioManager = (AudioManager) reactContext.getSystemService(Context.AUDIO_SERVICE);
            Log.d(TAG, "AudioManager已初始化");
        } catch (Exception e) {
            Log.e(TAG, "初始化AudioManager时出错: " + e.getMessage());
        }
    }

    @ReactMethod
    public boolean isMuted() {
        if (audioManager == null) {
            Log.w(TAG, "AudioManager未初始化");
            return false;
        }

        try {
            int ringerMode = audioManager.getRingerMode();
            Log.d(TAG, "当前铃声模式: " + ringerMode);
            
            // 检查是否是静音或震动模式
            boolean isMuted = (ringerMode == AudioManager.RINGER_MODE_SILENT || 
                              ringerMode == AudioManager.RINGER_MODE_VIBRATE);
            
            Log.d(TAG, "是否静音/震动: " + isMuted);
            return isMuted;
        } catch (Exception e) {
            Log.e(TAG, "检查静音状态时出错: " + e.getMessage());
            return false;
        }
    }

    @ReactMethod
    public boolean isSilentMode() {
        if (audioManager == null) {
            Log.w(TAG, "AudioManager未初始化");
            return false;
        }

        try {
            int ringerMode = audioManager.getRingerMode();
            boolean isSilent = (ringerMode == AudioManager.RINGER_MODE_SILENT);
            Log.d(TAG, "是否纯静音模式: " + isSilent);
            return isSilent;
        } catch (Exception e) {
            Log.e(TAG, "检查纯静音模式时出错: " + e.getMessage());
            return false;
        }
    }

    @ReactMethod
    public boolean isVibrateMode() {
        if (audioManager == null) {
            Log.w(TAG, "AudioManager未初始化");
            return false;
        }

        try {
            int ringerMode = audioManager.getRingerMode();
            boolean isVibrate = (ringerMode == AudioManager.RINGER_MODE_VIBRATE);
            Log.d(TAG, "是否震动模式: " + isVibrate);
            return isVibrate;
        } catch (Exception e) {
            Log.e(TAG, "检查震动模式时出错: " + e.getMessage());
            return false;
        }
    }

    @ReactMethod
    public boolean isNormalMode() {
        if (audioManager == null) {
            Log.w(TAG, "AudioManager未初始化");
            return false;
        }

        try {
            int ringerMode = audioManager.getRingerMode();
            boolean isNormal = (ringerMode == AudioManager.RINGER_MODE_NORMAL);
            Log.d(TAG, "是否正常模式: " + isNormal);
            return isNormal;
        } catch (Exception e) {
            Log.e(TAG, "检查正常模式时出错: " + e.getMessage());
            return false;
        }
    }

    @ReactMethod
    public String getRingerMode() {
        if (audioManager == null) {
            Log.w(TAG, "AudioManager未初始化");
            return "unknown";
        }

        try {
            int ringerMode = audioManager.getRingerMode();
            String modeString;
            
            switch (ringerMode) {
                case AudioManager.RINGER_MODE_SILENT:
                    modeString = "silent";
                    break;
                case AudioManager.RINGER_MODE_VIBRATE:
                    modeString = "vibrate";
                    break;
                case AudioManager.RINGER_MODE_NORMAL:
                    modeString = "normal";
                    break;
                default:
                    modeString = "unknown";
            }
            
            Log.d(TAG, "铃声模式: " + modeString);
            return modeString;
        } catch (Exception e) {
            Log.e(TAG, "获取铃声模式时出错: " + e.getMessage());
            return "unknown";
        }
    }

    @ReactMethod
    public int getStreamVolume(int streamType) {
        if (audioManager == null) {
            Log.w(TAG, "AudioManager未初始化");
            return 0;
        }

        try {
            int volume = audioManager.getStreamVolume(streamType);
            Log.d(TAG, "流音量 " + streamType + ": " + volume);
            return volume;
        } catch (Exception e) {
            Log.e(TAG, "获取流音量时出错: " + e.getMessage());
            return 0;
        }
    }

    @ReactMethod
    public int getRingVolume() {
        return getStreamVolume(AudioManager.STREAM_RING);
    }

    @ReactMethod
    public int getNotificationVolume() {
        return getStreamVolume(AudioManager.STREAM_NOTIFICATION);
    }
}
