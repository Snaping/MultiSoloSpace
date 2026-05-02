import React, { useState, useEffect, useRef } from 'react';
import { View, Text, Switch, StyleSheet, Alert, Platform } from 'react-native';
import { check, request, PERMISSIONS, RESULTS } from 'react-native-permissions';
import Contacts from 'react-native-contacts';

// 导入原生模块
import { NativeModules, NativeEventEmitter } from 'react-native';
const { CallMonitor, FlashLight, AudioMode } = NativeModules;
const callEventEmitter = new NativeEventEmitter(CallMonitor);

const App = () => {
  const [isEnabled, setIsEnabled] = useState(false);
  const [hasPermissions, setHasPermissions] = useState(false);
  const [isFlashing, setIsFlashing] = useState(false);
  const flashIntervalRef = useRef(null);

  // 请求所有必要权限
  const requestPermissions = async () => {
    try {
      let permissionsGranted = true;

      // 检查并请求电话状态权限
      if (Platform.OS === 'android') {
        const phonePermission = await request(PERMISSIONS.ANDROID.READ_PHONE_STATE);
        if (phonePermission !== RESULTS.GRANTED) {
          permissionsGranted = false;
        }

        // 检查并请求通讯录权限
        const contactsPermission = await request(PERMISSIONS.ANDROID.READ_CONTACTS);
        if (contactsPermission !== RESULTS.GRANTED) {
          permissionsGranted = false;
        }

        // 检查并请求相机/闪光灯权限
        const cameraPermission = await request(PERMISSIONS.ANDROID.CAMERA);
        if (cameraPermission !== RESULTS.GRANTED) {
          permissionsGranted = false;
        }
      }

      setHasPermissions(permissionsGranted);
      return permissionsGranted;
    } catch (error) {
      console.error('权限请求错误:', error);
      Alert.alert('错误', '无法获取必要权限');
      return false;
    }
  };

  // 检查权限
  const checkPermissions = async () => {
    try {
      let allGranted = true;

      if (Platform.OS === 'android') {
        const phoneStatus = await check(PERMISSIONS.ANDROID.READ_PHONE_STATE);
        const contactsStatus = await check(PERMISSIONS.ANDROID.READ_CONTACTS);
        const cameraStatus = await check(PERMISSIONS.ANDROID.CAMERA);

        allGranted = phoneStatus === RESULTS.GRANTED &&
                     contactsStatus === RESULTS.GRANTED &&
                     cameraStatus === RESULTS.GRANTED;
      }

      setHasPermissions(allGranted);
      return allGranted;
    } catch (error) {
      console.error('权限检查错误:', error);
      return false;
    }
  };

  // 检查号码是否在通讯录中
  const isContactNumber = async (phoneNumber) => {
    try {
      const contacts = await Contacts.getAll();
      // 标准化电话号码（去掉空格、连字符等）
      const normalizedNumber = phoneNumber.replace(/[\s\-\(\)]/g, '');
      
      for (const contact of contacts) {
        for (const phone of contact.phoneNumbers) {
          const normalizedContactNumber = phone.number.replace(/[\s\-\(\)]/g, '');
          if (normalizedContactNumber.includes(normalizedNumber) || 
              normalizedNumber.includes(normalizedContactNumber)) {
            return true;
          }
        }
      }
      return false;
    } catch (error) {
      console.error('检查通讯录错误:', error);
      return false;
    }
  };

  // 开始闪光灯爆闪
  const startFlashAlert = () => {
    if (flashIntervalRef.current) return;
    
    setIsFlashing(true);
    let isOn = false;
    
    flashIntervalRef.current = setInterval(() => {
      if (FlashLight) {
        if (isOn) {
          FlashLight.turnOff();
        } else {
          FlashLight.turnOn();
        }
        isOn = !isOn;
      }
    }, 300); // 每300毫秒切换一次，产生爆闪效果
  };

  // 停止闪光灯爆闪
  const stopFlashAlert = () => {
    if (flashIntervalRef.current) {
      clearInterval(flashIntervalRef.current);
      flashIntervalRef.current = null;
    }
    
    if (FlashLight) {
      FlashLight.turnOff();
    }
    
    setIsFlashing(false);
  };

  // 处理电话状态变化
  const handleCallStateChange = async (event) => {
    if (!isEnabled) return;
    
    console.log('电话状态变化:', event);
    
    // 检查是否是静音状态
    if (AudioMode) {
      const isMuted = await AudioMode.isMuted();
      if (!isMuted) {
        console.log('手机不是静音状态，不触发提醒');
        return;
      }
    }
    
    // 处理来电状态
    if (event.state === 'RINGING') {
      const isContact = await isContactNumber(event.phoneNumber);
      
      if (isContact) {
        console.log('通讯录联系人来电，开始闪光灯提醒');
        startFlashAlert();
      } else {
        console.log('非通讯录联系人来电，不触发提醒');
      }
    } else if (event.state === 'IDLE' || event.state === 'OFFHOOK') {
      // 电话结束或接听，停止闪光灯
      console.log('电话状态变化，停止闪光灯提醒');
      stopFlashAlert();
    }
  };

  // 切换服务开关
  const toggleService = async () => {
    if (!hasPermissions) {
      const granted = await requestPermissions();
      if (!granted) {
        Alert.alert('权限不足', '需要所有权限才能使用此功能');
        return;
      }
    }
    
    setIsEnabled(!isEnabled);
    
    // 如果关闭服务，确保停止闪光灯
    if (isEnabled) {
      stopFlashAlert();
    }
  };

  // 组件挂载时检查权限
  useEffect(() => {
    checkPermissions();
    
    // 注册电话状态监听器
    let subscription = null;
    if (CallMonitor) {
      subscription = callEventEmitter.addListener(
        'CallStateChanged',
        handleCallStateChange
      );
    }
    
    // 组件卸载时清理
    return () => {
      if (subscription) {
        subscription.remove();
      }
      stopFlashAlert();
    };
  }, []);

  return (
    <View style={styles.container}>
      <Text style={styles.title}>强提醒服务</Text>
      
      <View style={styles.card}>
        <Text style={styles.cardTitle}>服务状态</Text>
        <View style={styles.switchContainer}>
          <Text style={styles.switchLabel}>
            {isEnabled ? '已启用' : '已禁用'}
          </Text>
          <Switch
            trackColor={{ false: '#767577', true: '#81b0ff' }}
            thumbColor={isEnabled ? '#f5dd4b' : '#f4f3f4'}
            ios_backgroundColor="#3e3e3e"
            onValueChange={toggleService}
            value={isEnabled}
          />
        </View>
      </View>
      
      {isFlashing && (
        <View style={[styles.card, styles.alertCard]}>
          <Text style={styles.alertText}>⚠️ 正在进行闪光灯提醒</Text>
        </View>
      )}
      
      <View style={styles.infoCard}>
        <Text style={styles.infoTitle}>功能说明</Text>
        <Text style={styles.infoText}>• 当手机处于静音状态时</Text>
        <Text style={styles.infoText}>• 有通讯录中的联系人来电时</Text>
        <Text style={styles.infoText}>• 手机闪光灯会爆闪提醒</Text>
        <Text style={styles.infoText}>• 不会响铃或震动</Text>
      </View>
      
      <View style={styles.permissionCard}>
        <Text style={styles.permissionTitle}>权限状态</Text>
        <Text style={[styles.permissionText, hasPermissions ? styles.granted : styles.denied]}>
          {hasPermissions ? '✓ 所有权限已获取' : '✗ 需要获取权限'}
        </Text>
      </View>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
    padding: 20,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    textAlign: 'center',
    marginVertical: 20,
    color: '#333',
  },
  card: {
    backgroundColor: 'white',
    borderRadius: 10,
    padding: 20,
    marginBottom: 15,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  cardTitle: {
    fontSize: 18,
    fontWeight: '600',
    marginBottom: 15,
    color: '#333',
  },
  switchContainer: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  switchLabel: {
    fontSize: 16,
    color: '#666',
  },
  alertCard: {
    backgroundColor: '#ffcccc',
    borderColor: '#ff0000',
    borderWidth: 1,
  },
  alertText: {
    fontSize: 16,
    color: '#ff0000',
    fontWeight: 'bold',
    textAlign: 'center',
  },
  infoCard: {
    backgroundColor: 'white',
    borderRadius: 10,
    padding: 20,
    marginBottom: 15,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  infoTitle: {
    fontSize: 16,
    fontWeight: '600',
    marginBottom: 10,
    color: '#333',
  },
  infoText: {
    fontSize: 14,
    color: '#666',
    marginBottom: 5,
  },
  permissionCard: {
    backgroundColor: 'white',
    borderRadius: 10,
    padding: 20,
    marginBottom: 15,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  permissionTitle: {
    fontSize: 16,
    fontWeight: '600',
    marginBottom: 10,
    color: '#333',
  },
  permissionText: {
    fontSize: 14,
    marginBottom: 5,
  },
  granted: {
    color: 'green',
  },
  denied: {
    color: 'red',
  },
});

export default App;
