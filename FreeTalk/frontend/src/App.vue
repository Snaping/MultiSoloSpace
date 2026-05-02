<template>
  <div class="app-container" :style="appStyle">
    <header class="app-header">
      <div class="header-content">
        <router-link to="/" class="logo">
          <h1>FreeTalk</h1>
          <span class="subtitle">免费闲聊</span>
        </router-link>
        
        <div class="header-right">
          <div class="theme-info">
            <span class="theme-label">今日主题:</span>
            <span class="theme-name">{{ themeName }}</span>
          </div>
          
          <div class="user-info">
            <span class="user-label">我的身份:</span>
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
  </div>
</template>

<script setup>
import { ref, computed, onMounted, provide } from 'vue'
import { useRouter } from 'vue-router'
import { getTheme, getUserIdentity, isVIP as checkVIP, setVIP } from './api'
import VIPModal from './components/VIPModal.vue'

const router = useRouter()

const themeName = ref('加载中...')
const userName = ref('加载中...')
const isVIP = ref(false)
const showVIPModal = ref(false)
const themeStyle = ref({
  primary: '#1E3A8A',
  secondary: '#3B82F6',
  background: '#EFF6FF',
  accent: '#60A5FA',
  font: 'sans-serif',
  cardShadow: '0 4px 12px rgba(0,0,0,0.1)'
})

const appStyle = computed(() => ({
  backgroundColor: themeStyle.value.background,
  fontFamily: themeStyle.value.font,
  minHeight: '100vh'
}))

const vipBtnStyle = computed(() => ({
  backgroundColor: themeStyle.value.primary,
  color: '#fff'
}))

const vipBadgeStyle = computed(() => ({
  backgroundColor: themeStyle.value.accent,
  color: themeStyle.value.primary
}))

async function initApp() {
  try {
    const [themeRes, identityRes] = await Promise.all([
      getTheme(),
      getUserIdentity()
    ])
    
    if (themeRes.success) {
      themeName.value = themeRes.data.name
      themeStyle.value = themeRes.data.style
    }
    
    if (identityRes.success) {
      userName.value = identityRes.data.name
    }
    
    isVIP.value = checkVIP()
    
    provide('themeStyle', themeStyle)
    provide('userName', userName)
    provide('isVIP', isVIP)
  } catch (error) {
    console.error('初始化失败:', error)
  }
}

function upgradeToVIP() {
  setVIP(true)
  isVIP.value = true
  showVIPModal.value = false
}

onMounted(() => {
  initApp()
})
</script>

<style scoped>
.app-container {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  transition: all 0.3s ease;
}

.app-header {
  padding: 16px 24px;
  backdrop-filter: blur(10px);
  border-bottom: 1px solid rgba(0, 0, 0, 0.1);
  position: sticky;
  top: 0;
  z-index: 100;
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
  align-items: baseline;
  gap: 12px;
}

.logo h1 {
  font-size: 28px;
  font-weight: 700;
  color: inherit;
}

.subtitle {
  font-size: 14px;
  opacity: 0.7;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 24px;
  flex-wrap: wrap;
}

.theme-info,
.user-info {
  display: flex;
  align-items: center;
  gap: 8px;
}

.theme-label,
.user-label {
  font-size: 14px;
  opacity: 0.7;
}

.theme-name,
.user-name {
  font-weight: 600;
  font-size: 14px;
}

.vip-btn {
  padding: 8px 16px;
  border: none;
  border-radius: 20px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  font-size: 14px;
}

.vip-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.vip-badge {
  padding: 6px 12px;
  border-radius: 20px;
  font-weight: 600;
  font-size: 12px;
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
  }
  
  .logo h1 {
    font-size: 24px;
  }
  
  .app-main {
    padding: 16px;
  }
}
</style>
