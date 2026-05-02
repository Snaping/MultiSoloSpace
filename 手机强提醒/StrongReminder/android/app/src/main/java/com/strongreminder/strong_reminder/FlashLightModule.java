package com.strongreminder.strong_reminder;

import android.content.Context;
import android.content.pm.PackageManager;
import android.hardware.camera2.CameraAccessException;
import android.hardware.camera2.CameraManager;
import android.os.Build;
import android.util.Log;

import androidx.annotation.NonNull;
import androidx.annotation.RequiresApi;

import com.facebook.react.bridge.ReactApplicationContext;
import com.facebook.react.bridge.ReactContextBaseJavaModule;
import com.facebook.react.bridge.ReactMethod;

import javax.annotation.Nonnull;

public class FlashLightModule extends ReactContextBaseJavaModule {

    private static final String TAG = "FlashLightModule";
    private final ReactApplicationContext reactContext;
    private CameraManager cameraManager;
    private String cameraId;
    private boolean isFlashOn = false;
    private boolean hasFlash = false;

    public FlashLightModule(@Nonnull ReactApplicationContext reactContext) {
        super(reactContext);
        this.reactContext = reactContext;
        initCamera();
    }

    @NonNull
    @Override
    public String getName() {
        return "FlashLight";
    }

    private void initCamera() {
        try {
            // 检查设备是否有闪光灯
            hasFlash = reactContext.getPackageManager()
                .hasSystemFeature(PackageManager.FEATURE_CAMERA_FLASH);
            
            if (!hasFlash) {
                Log.w(TAG, "设备没有闪光灯");
                return;
            }

            // 获取CameraManager
            cameraManager = (CameraManager) reactContext.getSystemService(Context.CAMERA_SERVICE);
            
            // 获取后置摄像头ID
            String[] cameraIds = cameraManager.getCameraIdList();
            for (String id : cameraIds) {
                android.hardware.camera2.CameraCharacteristics characteristics = 
                    cameraManager.getCameraCharacteristics(id);
                
                // 检查是否是后置摄像头并且有闪光灯
                Boolean flashAvailable = characteristics.get(
                    android.hardware.camera2.CameraCharacteristics.FLASH_INFO_AVAILABLE);
                Integer lensFacing = characteristics.get(
                    android.hardware.camera2.CameraCharacteristics.LENS_FACING);
                
                if (flashAvailable != null && flashAvailable && 
                    lensFacing != null && lensFacing == android.hardware.camera2.CameraCharacteristics.LENS_FACING_BACK) {
                    cameraId = id;
                    Log.d(TAG, "找到可用的后置摄像头: " + cameraId);
                    break;
                }
            }
            
            if (cameraId == null) {
                Log.w(TAG, "没有找到可用的后置摄像头");
                hasFlash = false;
            }
        } catch (Exception e) {
            Log.e(TAG, "初始化相机时出错: " + e.getMessage());
            hasFlash = false;
        }
    }

    @ReactMethod
    public void turnOn() {
        if (!hasFlash || cameraId == null || cameraManager == null) {
            Log.w(TAG, "无法打开闪光灯: 设备不支持或未初始化");
            return;
        }

        try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                cameraManager.setTorchMode(cameraId, true);
                isFlashOn = true;
                Log.d(TAG, "闪光灯已打开");
            }
        } catch (CameraAccessException e) {
            Log.e(TAG, "打开闪光灯时出错: " + e.getMessage());
        }
    }

    @ReactMethod
    public void turnOff() {
        if (!hasFlash || cameraId == null || cameraManager == null) {
            Log.w(TAG, "无法关闭闪光灯: 设备不支持或未初始化");
            return;
        }

        try {
            if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.M) {
                cameraManager.setTorchMode(cameraId, false);
                isFlashOn = false;
                Log.d(TAG, "闪光灯已关闭");
            }
        } catch (CameraAccessException e) {
            Log.e(TAG, "关闭闪光灯时出错: " + e.getMessage());
        }
    }

    @ReactMethod
    public void toggle() {
        if (isFlashOn) {
            turnOff();
        } else {
            turnOn();
        }
    }

    @ReactMethod
    public boolean isAvailable() {
        return hasFlash && cameraId != null && cameraManager != null;
    }

    @ReactMethod
    public boolean isOn() {
        return isFlashOn;
    }
}
