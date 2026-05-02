import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import api from '@/api'

export const useUserStore = defineStore('user', () => {
  const token = ref(localStorage.getItem('token') || '')
  const userInfo = ref(JSON.parse(localStorage.getItem('userInfo') || 'null'))

  const isLoggedIn = computed(() => !!token.value)
  const isVip = computed(() => userInfo.value?.is_vip || false)
  const expireAt = computed(() => userInfo.value?.expire_at)
  const username = computed(() => userInfo.value?.username || '')

  async function login(username, password) {
    try {
      const res = await api.post('/auth/login', { username, password })
      
      if (res.data.success) {
        token.value = res.data.data.token
        userInfo.value = res.data.data.user
        
        localStorage.setItem('token', token.value)
        localStorage.setItem('userInfo', JSON.stringify(userInfo.value))
        
        return { success: true, message: '登录成功' }
      } else {
        return { success: false, message: res.data.message }
      }
    } catch (error) {
      return { 
        success: false, 
        message: error.response?.data?.message || '登录失败，请检查网络连接' 
      }
    }
  }

  async function register(username, password) {
    try {
      const res = await api.post('/auth/register', { username, password })
      
      if (res.data.success) {
        token.value = res.data.data.token
        userInfo.value = res.data.data.user
        
        localStorage.setItem('token', token.value)
        localStorage.setItem('userInfo', JSON.stringify(userInfo.value))
        
        return { success: true, message: '注册成功' }
      } else {
        return { success: false, message: res.data.message }
      }
    } catch (error) {
      return { 
        success: false, 
        message: error.response?.data?.message || '注册失败，请检查网络连接' 
      }
    }
  }

  async function activateCard(code) {
    try {
      const res = await api.post('/auth/activate', { code })
      
      if (res.data.success) {
        if (userInfo.value) {
          userInfo.value.is_vip = true
          userInfo.value.expire_at = res.data.data.expire_at
          localStorage.setItem('userInfo', JSON.stringify(userInfo.value))
        }
        return { success: true, message: res.data.message, data: res.data.data }
      } else {
        return { success: false, message: res.data.message }
      }
    } catch (error) {
      return { 
        success: false, 
        message: error.response?.data?.message || '激活失败，请检查网络连接' 
      }
    }
  }

  async function verifyToken() {
    try {
      const res = await api.get('/auth/verify')
      
      if (res.data.success) {
        userInfo.value = res.data.data.user
        localStorage.setItem('userInfo', JSON.stringify(userInfo.value))
        return { success: true }
      } else {
        logout()
        return { success: false }
      }
    } catch (error) {
      logout()
      return { success: false }
    }
  }

  function logout() {
    token.value = ''
    userInfo.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('userInfo')
  }

  function getRemainingTime() {
    if (!expireAt.value) return null
    
    const now = new Date()
    const expire = new Date(expireAt.value)
    const diff = expire - now
    
    if (diff <= 0) return null
    
    const days = Math.floor(diff / (1000 * 60 * 60 * 24))
    const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60))
    const minutes = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60))
    
    return { days, hours, minutes }
  }

  return {
    token,
    userInfo,
    isLoggedIn,
    isVip,
    expireAt,
    username,
    login,
    register,
    activateCard,
    verifyToken,
    logout,
    getRemainingTime
  }
})
