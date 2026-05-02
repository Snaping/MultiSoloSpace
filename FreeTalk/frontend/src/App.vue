<template>
  <div class="app-container" :style="appStyle">
    <header class="app-header" :style="headerStyle">
      <div class="header-content">
        <router-link to="/" class="logo">
          <span class="theme-icon">{{ themeIcon }}</span>
          <div class="logo-text">
            <h1 :style="headerTextStyle">FreeTalk</h1>
            <span class="subtitle" :style="headerTextStyle">免费闲聊</span>
          </div>
        </router-link>
        
        <div class="header-right">
          <div class="theme-selector-wrapper" @click="showThemeSelector = !showThemeSelector">
            <div class="theme-selector-trigger">
              <span class="trigger-icon">{{ themeIcon }}</span>
              <span class="trigger-label">{{ themeName }}</span>
              <span class="trigger-arrow" :class="{ open: showThemeSelector }">▼</span>
            </div>
            
            <div 
              v-if="showThemeSelector" 
              class="theme-selector-dropdown"
              :style="dropdownStyle"
              @click.stop
            >
              <div class="dropdown-header">
                <h4>选择主题</h4>
                <span class="reset-btn" @click="resetToDailyTheme">
                  🔄 恢复今日主题
                </span>
              </div>
              
              <div class="theme-list">
                <div 
                  v-for="theme in allThemes" 
                  :key="theme.id"
                  class="theme-item"
                  :class="{ active: theme.isActive }"
                  :style="getThemeItemStyle(theme)"
                  @click="selectTheme(theme.id)"
                >
                  <span class="theme-item-icon">{{ theme.icon }}</span>
                  <div class="theme-item-info">
                    <span class="theme-item-name">{{ theme.name }}</span>
                    <span class="theme-item-desc">{{ theme.description }}</span>
                  </div>
                  <span v-if="theme.isActive" class="theme-item-check">✓</span>
                  <span v-if="theme.isDaily && !theme.isActive" class="theme-item-daily">今日</span>
                </div>
              </div>
            </div>
          </div>
          
          <div class="user-info">
            <span class="user-label">身份:</span>
            <span class="user-name">{{ userName }}</span>
          </div>
          
          <button 
            v-if="!isVIP" 
            @click="showVIPModal = true"
            class="vip-btn"
            :style="vipBtnStyle"
          >
            成为会员
          </button>
          <span v-else class="vip-badge" :style="vipBadgeStyle">
            VIP会员
          </span>
        </div>
      </div>
    </header>
    
    <main class="app-main">
      <router-view :key="$route.fullPath" />
    </main>
    
    <VIPModal 
      v-if="showVIPModal" 
      @close="showVIPModal = false"
      @upgrade="upgradeToVIP"
    />
    
    <div 
      v-if="showThemeSelector" 
      class="theme-overlay" 
      @click="showThemeSelector = false"
    ></div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, provide, watch } from 'vue'
import { useRouter } from 'vue-router'
import { io } from 'socket.io-client'
import { getTheme, getUserIdentity, isVIP as checkVIP, setVIP, getAllThemes, switchTheme } from './api'
import VIPModal from './components/VIPModal.vue'

const router = useRouter()

const themeName = ref('加载中...')
const themeIcon = ref('🎨')
const themeDescription = ref('')
const userName = ref('加载中...')
const isVIP = ref(false)
const showVIPModal = ref(false)
const showThemeSelector = ref(false)
const allThemes = ref([])
let socket = null

const themeStyle = ref({
  primary: '#1E3A8A',
  secondary: '#3B82F6',
  background: '#EFF6FF',
  backgroundColor: '#EFF6FF',
  accent: '#60A5FA',
  accentLight: '#BFDBFE',
  success: '#22C55E',
  error: '#EF4444',
  warning: '#F59E0B',
  info: '#3B82F6',
  font: 'sans-serif',
  cardShadow: '0 4px 12px rgba(0,0,0,0.1)',
  cardBorder: '1px solid #ddd',
  buttonGradient: 'linear-gradient(135deg, #1E3A8A 0%, #3B82F6 100%)',
  buttonShadow: '0 4px 12px rgba(0,0,0,0.1)',
  inputBorder: '1px solid #ddd',
  inputFocusBorder: '2px solid #3B82F6',
  messageUserBg: 'linear-gradient(135deg, #1E3A8A 0%, #3B82F6 100%)',
  messageOtherBg: '#fff',
  headerBg: 'linear-gradient(180deg, #1E3A8A 0%, #3B82F6 100%)',
  headerText: '#FFFFFF',
  borderRadius: '12px',
  transition: 'all 0.3s ease',
  animationDuration: '0.5s',
  hoverScale: '1.02'
})

const appStyle = computed(() => ({
  background: themeStyle.value.background,
  backgroundColor: themeStyle.value.backgroundColor,
  fontFamily: themeStyle.value.font,
  minHeight: '100vh',
  transition: themeStyle.value.transition
}))

const headerStyle = computed(() => ({
  background: themeStyle.value.headerBg,
  transition: themeStyle.value.transition
}))

const headerTextStyle = computed(() => ({
  color: themeStyle.value.headerText
}))

const vipBtnStyle = computed(() => ({
  background: themeStyle.value.buttonGradient,
  boxShadow: themeStyle.value.buttonShadow,
  color: '#fff'
}))

const vipBadgeStyle = computed(() => ({
  background: themeStyle.value.accentLight,
  color: themeStyle.value.primary,
  border: `2px solid ${themeStyle.value.primary}`
}))

const dropdownStyle = computed(() => ({
  border: themeStyle.value.cardBorder,
  boxShadow: themeStyle.value.cardShadow
}))

function getThemeItemStyle(theme) {
  const style = {
    borderLeft: theme.isActive ? `4px solid ${theme.style.primary}` : '4px solid transparent'
  }
  
  if (theme.isActive) {
    style.background = theme.style.secondary + '20'
  }
  
  return style
}

async function loadAllThemes() {
  try {
    const res = await getAllThemes()
    if (res.success) {
      allThemes.value = res.data
    }
  } catch (error) {
    console.error('加载主题列表失败:', error)
  }
}

async function selectTheme(themeId) {
  try {
    const res = await switchTheme(themeId)
    if (res.success) {
      updateTheme(res.data)
      showThemeSelector.value = false
    }
  } catch (error) {
    console.error('切换主题失败:', error)
  }
}

async function resetToDailyTheme() {
  try {
    const res = await switchTheme('daily')
    if (res.success) {
      updateTheme(res.data)
      showThemeSelector.value = false
    }
  } catch (error) {
    console.error('重置主题失败:', error)
  }
}

function updateTheme(themeData) {
  themeName.value = themeData.name
  themeIcon.value = themeData.icon || '🎨'
  themeDescription.value = themeData.description || ''
  
  if (themeData.style) {
    themeStyle.value = { ...themeData.style }
  }
  
  loadAllThemes()
}

function initWebSocket() {
  try {
    socket = io({
      transports: ['websocket', 'polling']
    })
    
    socket.on('themeChanged', (data) => {
      console.log('收到主题变更通知:', data)
      updateTheme({
        ...data,
        style: data.style
      })
    })
  } catch (error) {
    console.error('WebSocket连接失败:', error)
  }
}

async function initApp() {
  try {
    const [themeRes, identityRes] = await Promise.all([
      getTheme(),
      getUserIdentity()
    ])
    
    if (themeRes.success) {
      updateTheme(themeRes.data)
    }
    
    if (identityRes.success) {
      userName.value = identityRes.data.name
    }
    
    isVIP.value = checkVIP()
    
    await loadAllThemes()
    
    provide('themeStyle', themeStyle)
    provide('userName', userName)
    provide('isVIP', isVIP)
    
    initWebSocket()
  } catch (error) {
    console.error('初始化失败:', error)
  }
}

function upgradeToVIP() {
  setVIP(true)
  isVIP.value = true
  showVIPModal.value = false
}

watch(showThemeSelector, (newVal) => {
  if (newVal) {
    loadAllThemes()
  }
})

onMounted(() => {
  initApp()
})
</script>

<style scoped>
.app-container {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
}

.app-header {
  padding: 12px 24px;
  backdrop-filter: blur(20px);
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  position: sticky;
  top: 0;
  z-index: 100;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
}

.header-content {
  max-width: 1400px;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 16px;
}

.logo {
  text-decoration: none;
  display: flex;
  align-items: center;
  gap: 16px;
}

.theme-icon {
  font-size: 36px;
  animation: bounce 2s ease-in-out infinite;
}

@keyframes bounce {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-5px); }
}

.logo-text {
  display: flex;
  flex-direction: column;
  line-height: 1.2;
}

.logo-text h1 {
  font-size: 28px;
  font-weight: 800;
  margin: 0;
  letter-spacing: 1px;
}

.subtitle {
  font-size: 13px;
  opacity: 0.85;
  font-weight: 500;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 20px;
  flex-wrap: wrap;
}

.theme-selector-wrapper {
  position: relative;
}

.theme-selector-trigger {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 16px;
  background: rgba(255, 255, 255, 0.15);
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-radius: 25px;
  cursor: pointer;
  transition: all 0.3s ease;
  backdrop-filter: blur(10px);
}

.theme-selector-trigger:hover {
  background: rgba(255, 255, 255, 0.25);
  transform: translateY(-1px);
  box-shadow: 0 4px 15px rgba(0, 0, 0, 0.1);
}

.trigger-icon {
  font-size: 20px;
}

.trigger-label {
  font-weight: 600;
  font-size: 14px;
  color: inherit;
}

.trigger-arrow {
  font-size: 10px;
  opacity: 0.7;
  transition: transform 0.3s ease;
}

.trigger-arrow.open {
  transform: rotate(180deg);
}

.theme-selector-dropdown {
  position: absolute;
  top: 100%;
  right: 0;
  margin-top: 12px;
  width: 380px;
  max-height: 480px;
  background: #fff;
  border-radius: 16px;
  overflow: hidden;
  z-index: 1000;
  animation: dropdownIn 0.3s ease;
}

@keyframes dropdownIn {
  from {
    opacity: 0;
    transform: translateY(-10px) scale(0.95);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

.dropdown-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 20px;
  background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%);
  border-bottom: 1px solid #eee;
}

.dropdown-header h4 {
  margin: 0;
  font-size: 15px;
  font-weight: 700;
  color: #333;
}

.reset-btn {
  font-size: 12px;
  color: #666;
  cursor: pointer;
  padding: 4px 10px;
  border-radius: 12px;
  background: #fff;
  transition: all 0.2s ease;
}

.reset-btn:hover {
  background: #e9ecef;
  color: #333;
}

.theme-list {
  max-height: 400px;
  overflow-y: auto;
  padding: 8px;
}

.theme-item {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 14px 16px;
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.2s ease;
  margin-bottom: 4px;
  border-left: 4px solid transparent;
}

.theme-item:hover {
  background: #f8f9fa;
  transform: translateX(4px);
}

.theme-item.active {
  background: linear-gradient(135deg, rgba(59, 130, 246, 0.1) 0%, rgba(147, 51, 234, 0.1) 100%);
}

.theme-item-icon {
  font-size: 28px;
  width: 40px;
  height: 40px;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.05);
  border-radius: 10px;
}

.theme-item-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.theme-item-name {
  font-weight: 600;
  font-size: 15px;
  color: #333;
}

.theme-item-desc {
  font-size: 12px;
  color: #888;
}

.theme-item-check {
  font-size: 16px;
  color: #22C55E;
  font-weight: bold;
}

.theme-item-daily {
  font-size: 11px;
  padding: 3px 10px;
  background: linear-gradient(135deg, #FCD34D 0%, #F59E0B 100%);
  color: #fff;
  border-radius: 10px;
  font-weight: 600;
}

.theme-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 999;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 14px;
  background: rgba(255, 255, 255, 0.15);
  border-radius: 20px;
  border: 1px solid rgba(255, 255, 255, 0.2);
}

.user-label {
  font-size: 13px;
  opacity: 0.8;
  color: inherit;
}

.user-name {
  font-weight: 700;
  font-size: 14px;
  color: inherit;
}

.vip-btn {
  padding: 10px 20px;
  border: none;
  border-radius: 25px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.3s ease;
  font-size: 14px;
  letter-spacing: 0.5px;
}

.vip-btn:hover {
  transform: translateY(-2px);
}

.vip-badge {
  padding: 8px 16px;
  border-radius: 25px;
  font-weight: 700;
  font-size: 13px;
  display: flex;
  align-items: center;
  gap: 6px;
}

.vip-badge::before {
  content: '✨';
}

.app-main {
  flex: 1;
  padding: 24px;
}

@media (max-width: 768px) {
  .app-header {
    padding: 12px 16px;
  }
  
  .header-content {
    flex-direction: column;
    align-items: flex-start;
  }
  
  .header-right {
    width: 100%;
    justify-content: space-between;
    gap: 12px;
  }
  
  .theme-selector-dropdown {
    width: calc(100vw - 32px);
    right: auto;
    left: 0;
  }
  
  .logo-text h1 {
    font-size: 22px;
  }
  
  .app-main {
    padding: 16px;
  }
  
  .theme-icon {
    font-size: 28px;
  }
}
</style>
