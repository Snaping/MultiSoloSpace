<template>
  <div class="game-detail-container">
    <el-container>
      <el-header class="header">
        <div class="header-left">
          <el-button text @click="goBack" class="back-btn">
            <el-icon><ArrowLeft /></el-icon>
            返回
          </el-button>
          <div class="game-header" v-if="game">
            <div class="game-icon">{{ game.icon }}</div>
            <div class="game-info">
              <h1 class="game-name">{{ game.name }}</h1>
              <div class="game-tags">
                <el-tag :type="getCategoryType(game.category)" size="small">
                  {{ game.category }}
                </el-tag>
                <el-tag type="info" size="small">
                  {{ formatPlayers(game.players) }} 在线
                </el-tag>
              </div>
            </div>
          </div>
        </div>

        <div class="header-right">
          <div class="user-vip-status">
            <el-icon><Crown /></el-icon>
            <span class="vip-text" v-if="remainingTime">
              剩余 {{ remainingTime.days }}天 {{ remainingTime.hours }}小时
            </span>
            <span class="vip-text expired" v-else>
              VIP已过期
            </span>
          </div>
        </div>
      </el-header>

      <el-main class="main-content">
        <div class="content-wrapper" v-loading="loading">
          <div class="section routes-section">
            <h2 class="section-title">
              <el-icon><Connection /></el-icon>
              选择加速线路
            </h2>
            <p class="section-desc">选择最优线路，获得最佳游戏体验</p>

            <div class="routes-grid">
              <div
                v-for="route in routes"
                :key="route.id"
                class="route-card"
                :class="{ active: selectedRoute?.id === route.id }"
                @click="selectRoute(route)"
              >
                <div class="route-header">
                  <div class="route-location">
                    <el-icon><Location /></el-icon>
                    {{ route.location }}
                  </div>
                  <div class="route-ping" :class="getPingClass(route.ping)">
                    <el-icon><Promotion /></el-icon>
                    {{ route.ping }}ms
                  </div>
                </div>
                <div class="route-name">{{ route.name }}</div>
                <div class="route-status">
                  <el-tag type="success" size="small" effect="dark">
                    在线
                  </el-tag>
                </div>
              </div>

              <el-empty v-if="routes.length === 0" description="暂无可用线路" />
            </div>
          </div>

          <div class="section action-section">
            <div class="action-content">
              <div class="selected-info" v-if="selectedRoute">
                <div class="info-item">
                  <span class="label">当前游戏</span>
                  <span class="value">{{ game?.name }}</span>
                </div>
                <div class="info-item">
                  <span class="label">线路选择</span>
                  <span class="value">{{ selectedRoute.name }} ({{ selectedRoute.location }})</span>
                </div>
                <div class="info-item">
                  <span class="label">预计延迟</span>
                  <span class="value" :class="getPingClass(selectedRoute.ping)">
                    {{ selectedRoute.ping }}ms
                  </span>
                </div>
              </div>

              <div class="action-buttons">
                <el-button
                  type="primary"
                  size="large"
                  :loading="connecting"
                  :disabled="!selectedRoute"
                  class="accelerate-btn"
                  @click="toggleAcceleration"
                >
                  <template v-if="isAccelerating">
                    <el-icon class="rotating"><Loading /></el-icon>
                    断开加速
                  </template>
                  <template v-else>
                    <el-icon><Lightning /></el-icon>
                    开始加速
                  </template>
                </el-button>
              </div>
            </div>
          </div>

          <div class="section stats-section" v-if="isAccelerating">
            <h2 class="section-title">
              <el-icon><DataAnalysis /></el-icon>
              加速状态
            </h2>
            
            <div class="stats-grid">
              <div class="stat-card">
                <div class="stat-icon delay">
                  <el-icon><Timer /></el-icon>
                </div>
                <div class="stat-info">
                  <span class="stat-value">{{ realTimeStats.delay }}ms</span>
                  <span class="stat-label">当前延迟</span>
                </div>
              </div>

              <div class="stat-card">
                <div class="stat-icon loss">
                  <el-icon><TrendCharts /></el-icon>
                </div>
                <div class="stat-info">
                  <span class="stat-value">{{ realTimeStats.loss }}%</span>
                  <span class="stat-label">丢包率</span>
                </div>
              </div>

              <div class="stat-card">
                <div class="stat-icon speed">
                  <el-icon><Upload /></el-icon>
                </div>
                <div class="stat-info">
                  <span class="stat-value">{{ realTimeStats.upload }} MB/s</span>
                  <span class="stat-label">上传速度</span>
                </div>
              </div>

              <div class="stat-card">
                <div class="stat-icon duration">
                  <el-icon><Clock /></el-icon>
                </div>
                <div class="stat-info">
                  <span class="stat-value">{{ realTimeStats.duration }}</span>
                  <span class="stat-label">加速时长</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </el-main>
    </el-container>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'
import api from '@/api'

const route = useRoute()
const router = useRouter()
const userStore = useUserStore()

const loading = ref(false)
const connecting = ref(false)
const game = ref(null)
const routes = ref([])
const selectedRoute = ref(null)
const isAccelerating = ref(false)
const accelerationStartTime = ref(null)
const statsInterval = ref(null)

const realTimeStats = ref({
  delay: 0,
  loss: 0,
  upload: 0,
  duration: '00:00:00'
})

const remainingTime = computed(() => userStore.getRemainingTime())

const formatPlayers = (count) => {
  if (count >= 10000) {
    return (count / 10000).toFixed(1) + '万'
  }
  return count.toLocaleString()
}

const getCategoryType = (category) => {
  const types = {
    'MOBA': 'primary',
    'FPS': 'success',
    'RPG': 'warning'
  }
  return types[category] || 'info'
}

const getPingClass = (ping) => {
  if (ping <= 30) return 'excellent'
  if (ping <= 60) return 'good'
  if (ping <= 100) return 'normal'
  return 'bad'
}

const goBack = () => {
  if (isAccelerating.value) {
    ElMessage.warning('请先断开加速连接')
    return
  }
  router.push('/home')
}

const loadGameData = async () => {
  loading.value = true
  try {
    const gameId = route.params.id
    
    const [gameRes, routesRes] = await Promise.all([
      api.get(`/games/${gameId}`),
      api.get(`/games/${gameId}/routes`)
    ])

    if (gameRes.data.success) {
      game.value = gameRes.data.data
    }

    if (routesRes.data.success) {
      routes.value = routesRes.data.data
      if (routes.value.length > 0) {
        selectedRoute.value = routes.value.reduce((best, current) => 
          current.ping < best.ping ? current : best
        )
      }
    }
  } catch (error) {
    ElMessage.error('加载游戏信息失败')
  } finally {
    loading.value = false
  }
}

const selectRoute = (route) => {
  if (isAccelerating.value) {
    ElMessage.warning('加速中无法切换线路')
    return
  }
  selectedRoute.value = route
}

const toggleAcceleration = async () => {
  if (!selectedRoute.value) {
    ElMessage.warning('请先选择线路')
    return
  }

  if (isAccelerating.value) {
    stopAcceleration()
  } else {
    startAcceleration()
  }
}

const startAcceleration = async () => {
  connecting.value = true
  
  await new Promise(resolve => setTimeout(resolve, 1500))
  
  connecting.value = false
  isAccelerating.value = true
  accelerationStartTime.value = Date.now()
  
  realTimeStats.value = {
    delay: selectedRoute.value.ping,
    loss: 0.1,
    upload: 1.2,
    duration: '00:00:00'
  }
  
  statsInterval.value = setInterval(updateStats, 1000)
  
  ElMessage.success(`已连接到 ${selectedRoute.value.name}`)
}

const stopAcceleration = () => {
  isAccelerating.value = false
  
  if (statsInterval.value) {
    clearInterval(statsInterval.value)
    statsInterval.value = null
  }
  
  ElMessage.info('已断开加速连接')
}

const updateStats = () => {
  const baseDelay = selectedRoute.value?.ping || 30
  const variation = Math.floor(Math.random() * 10) - 5
  
  realTimeStats.value.delay = Math.max(10, baseDelay + variation)
  realTimeStats.value.loss = (Math.random() * 0.5).toFixed(1)
  realTimeStats.value.upload = (0.8 + Math.random() * 1.0).toFixed(1)
  
  const elapsed = Math.floor((Date.now() - accelerationStartTime.value) / 1000)
  const hours = Math.floor(elapsed / 3600).toString().padStart(2, '0')
  const minutes = Math.floor((elapsed % 3600) / 60).toString().padStart(2, '0')
  const seconds = (elapsed % 60).toString().padStart(2, '0')
  realTimeStats.value.duration = `${hours}:${minutes}:${seconds}`
}

onMounted(() => {
  loadGameData()
})

onUnmounted(() => {
  if (statsInterval.value) {
    clearInterval(statsInterval.value)
  }
})
</script>

<style scoped>
.game-detail-container {
  min-height: 100vh;
  background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%);
}

.header {
  height: 80px;
  background: rgba(255, 255, 255, 0.03);
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 30px;
  backdrop-filter: blur(20px);
}

.header-left {
  display: flex;
  align-items: center;
  gap: 20px;
}

.back-btn {
  color: rgba(255, 255, 255, 0.7);
  font-size: 16px;
}

.back-btn:hover {
  color: #667eea;
}

.game-header {
  display: flex;
  align-items: center;
  gap: 16px;
}

.game-header .game-icon {
  width: 56px;
  height: 56px;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.2), rgba(118, 75, 162, 0.2));
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 28px;
}

.game-header .game-name {
  font-size: 22px;
  color: #fff;
  font-weight: 600;
  margin: 0 0 6px 0;
}

.game-tags {
  display: flex;
  gap: 8px;
}

.header-right {
  display: flex;
  align-items: center;
}

.user-vip-status {
  display: flex;
  align-items: center;
  gap: 8px;
  background: linear-gradient(90deg, rgba(102, 126, 234, 0.2), rgba(118, 75, 162, 0.2));
  padding: 10px 20px;
  border-radius: 24px;
  border: 1px solid rgba(102, 126, 234, 0.3);
}

.user-vip-status .el-icon {
  color: #667eea;
}

.vip-text {
  color: #fff;
  font-size: 14px;
  font-weight: 500;
}

.vip-text.expired {
  color: #f56c6c;
}

.main-content {
  padding: 30px;
}

.content-wrapper {
  max-width: 1200px;
  margin: 0 auto;
}

.section {
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 16px;
  padding: 24px;
  margin-bottom: 24px;
}

.section-title {
  font-size: 18px;
  color: #fff;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 10px;
  margin: 0 0 8px 0;
}

.section-desc {
  color: rgba(255, 255, 255, 0.5);
  font-size: 14px;
  margin: 0 0 20px 0;
}

.routes-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 16px;
}

.route-card {
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 12px;
  padding: 16px;
  cursor: pointer;
  transition: all 0.3s;
}

.route-card:hover {
  background: rgba(255, 255, 255, 0.06);
  border-color: rgba(102, 126, 234, 0.3);
}

.route-card.active {
  background: rgba(102, 126, 234, 0.1);
  border-color: #667eea;
  box-shadow: 0 0 0 2px rgba(102, 126, 234, 0.2);
}

.route-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 10px;
}

.route-location {
  display: flex;
  align-items: center;
  gap: 6px;
  color: rgba(255, 255, 255, 0.8);
  font-size: 14px;
}

.route-ping {
  display: flex;
  align-items: center;
  gap: 4px;
  font-weight: 600;
  font-size: 14px;
}

.route-ping.excellent { color: #67c23a; }
.route-ping.good { color: #409eff; }
.route-ping.normal { color: #e6a23c; }
.route-ping.bad { color: #f56c6c; }

.route-name {
  color: #fff;
  font-weight: 500;
  margin-bottom: 8px;
}

.action-section {
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1), rgba(118, 75, 162, 0.1));
}

.action-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 40px;
}

.selected-info {
  flex: 1;
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
}

.info-item {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.info-item .label {
  color: rgba(255, 255, 255, 0.5);
  font-size: 13px;
}

.info-item .value {
  color: #fff;
  font-size: 15px;
  font-weight: 500;
}

.accelerate-btn {
  min-width: 180px;
  height: 52px;
  font-size: 16px;
  font-weight: 600;
  background: linear-gradient(90deg, #667eea, #764ba2);
  border: none;
  box-shadow: 0 4px 20px rgba(102, 126, 234, 0.4);
}

.accelerate-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 25px rgba(102, 126, 234, 0.5);
}

.rotating {
  animation: rotate 1s linear infinite;
}

@keyframes rotate {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.stats-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 20px;
}

.stat-card {
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 12px;
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon {
  width: 50px;
  height: 50px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 24px;
}

.stat-icon.delay {
  background: rgba(103, 194, 58, 0.15);
  color: #67c23a;
}

.stat-icon.loss {
  background: rgba(64, 158, 255, 0.15);
  color: #409eff;
}

.stat-icon.speed {
  background: rgba(230, 162, 60, 0.15);
  color: #e6a23c;
}

.stat-icon.duration {
  background: rgba(118, 75, 162, 0.15);
  color: #764ba2;
}

.stat-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.stat-value {
  color: #fff;
  font-size: 24px;
  font-weight: 600;
}

.stat-label {
  color: rgba(255, 255, 255, 0.5);
  font-size: 13px;
}
</style>
