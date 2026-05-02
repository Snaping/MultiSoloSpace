<template>
  <div class="home-container">
    <el-container>
      <el-header class="header">
        <div class="header-left">
          <div class="logo">
            <el-icon :size="32" color="#667eea">
              <Lightning />
            </el-icon>
            <span class="logo-text">AdvSpeed</span>
          </div>
        </div>

        <div class="header-center">
          <div class="search-box">
            <el-input
              v-model="searchText"
              placeholder="搜索游戏..."
              prefix-icon="Search"
              clearable
              @clear="handleSearch"
              @keyup.enter="handleSearch"
            >
              <template #append>
                <el-button @click="handleSearch">
                  <el-icon><Search /></el-icon>
                </el-button>
              </template>
            </el-input>
          </div>
        </div>

        <div class="header-right">
          <el-dropdown @command="handleCommand">
            <div class="user-info">
              <el-avatar :size="36" class="avatar">
                <el-icon><User /></el-icon>
              </el-avatar>
              <div class="user-detail">
                <span class="username">{{ userStore.username }}</span>
                <div class="vip-status">
                  <el-tag v-if="userStore.isVip" type="success" size="small" effect="dark">
                    VIP会员
                  </el-tag>
                  <el-tag v-else type="info" size="small">
                    普通用户
                  </el-tag>
                </div>
              </div>
              <el-icon class="dropdown-icon"><ArrowDown /></el-icon>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="activate">
                  <el-icon><Key /></el-icon>
                  卡密激活
                </el-dropdown-item>
                <el-dropdown-item command="logout" divided>
                  <el-icon><SwitchButton /></el-icon>
                  退出登录
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>

      <el-main class="main-content">
        <div class="content-header">
          <h2 class="page-title">
            <el-icon><VideoCamera /></el-icon>
            热门游戏
          </h2>
          <div class="category-tabs">
            <el-tag
              v-for="cat in categories"
              :key="cat"
              :type="selectedCategory === cat ? 'primary' : 'info'"
              class="category-tag"
              @click="selectCategory(cat)"
            >
              {{ cat }}
            </el-tag>
          </div>
        </div>

        <div class="games-grid" v-loading="loading">
          <div
            v-for="game in filteredGames"
            :key="game.id"
            class="game-card"
            @click="goToGame(game)"
          >
            <div class="game-icon">{{ game.icon }}</div>
            <div class="game-info">
              <h3 class="game-name">{{ game.name }}</h3>
              <div class="game-meta">
                <el-tag :type="getCategoryType(game.category)" size="small">
                  {{ game.category }}
                </el-tag>
                <span class="players">
                  <el-icon><User /></el-icon>
                  {{ formatPlayers(game.players) }} 在线
                </span>
              </div>
            </div>
            <div class="game-actions">
              <el-button type="primary" size="small" class="play-btn">
                加速
              </el-button>
            </div>
          </div>

          <el-empty v-if="filteredGames.length === 0 && !loading" description="没有找到相关游戏" />
        </div>
      </el-main>
    </el-container>

    <div class="floating-status" v-if="accelerationStatus.active">
      <div class="floating-content">
        <div class="floating-icon">
          <el-icon :size="20" class="pulse"><Lightning /></el-icon>
        </div>
        <div class="floating-info">
          <span class="floating-game">{{ accelerationStatus.gameName }}</span>
          <span class="floating-ping">{{ accelerationStatus.ping }}ms</span>
        </div>
        <el-button type="danger" size="small" @click="stopAcceleration">
          断开
        </el-button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useUserStore } from '@/stores/user'
import api from '@/api'

const router = useRouter()
const userStore = useUserStore()

const loading = ref(false)
const games = ref([])
const categories = ref(['全部'])
const selectedCategory = ref('全部')
const searchText = ref('')

const accelerationStatus = ref({
  active: false,
  gameName: '',
  gameId: null,
  routeId: null,
  ping: 0
})

const filteredGames = computed(() => {
  let result = games.value

  if (selectedCategory.value !== '全部') {
    result = result.filter(g => g.category === selectedCategory.value)
  }

  if (searchText.value) {
    const keyword = searchText.value.toLowerCase()
    result = result.filter(g => 
      g.name.toLowerCase().includes(keyword) ||
      g.description?.toLowerCase().includes(keyword)
    )
  }

  return result
})

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

const loadGames = async () => {
  loading.value = true
  try {
    const res = await api.get('/games')
    if (res.data.success) {
      games.value = res.data.data
      
      const cats = [...new Set(res.data.data.map(g => g.category))]
      categories.value = ['全部', ...cats]
    }
  } catch (error) {
    ElMessage.error('加载游戏列表失败')
  } finally {
    loading.value = false
  }
}

const selectCategory = (cat) => {
  selectedCategory.value = cat
}

const handleSearch = () => {
}

const goToGame = (game) => {
  if (!userStore.isVip) {
    ElMessageBox.confirm(
      '加速功能需要VIP会员，是否前往激活卡密？',
      '提示',
      {
        confirmButtonText: '前往激活',
        cancelButtonText: '取消',
        type: 'warning'
      }
    ).then(() => {
      router.push('/activate')
    }).catch(() => {})
    return
  }
  router.push(`/game/${game.id}`)
}

const handleCommand = (command) => {
  if (command === 'activate') {
    router.push('/activate')
  } else if (command === 'logout') {
    ElMessageBox.confirm('确定要退出登录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    }).then(() => {
      userStore.logout()
      router.push('/login')
      ElMessage.success('已退出登录')
    }).catch(() => {})
  }
}

const stopAcceleration = () => {
  accelerationStatus.value = {
    active: false,
    gameName: '',
    gameId: null,
    routeId: null,
    ping: 0
  }
  ElMessage.info('已断开加速连接')
}

onMounted(() => {
  loadGames()
  userStore.verifyToken()
})
</script>

<style scoped>
.home-container {
  min-height: 100vh;
  background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%);
}

.header {
  height: 70px;
  background: rgba(255, 255, 255, 0.03);
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 30px;
  backdrop-filter: blur(20px);
}

.header-left .logo {
  display: flex;
  align-items: center;
  gap: 10px;
}

.logo-text {
  font-size: 22px;
  font-weight: 700;
  background: linear-gradient(90deg, #667eea, #764ba2);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.header-center {
  flex: 1;
  max-width: 500px;
  margin: 0 40px;
}

.search-box :deep(.el-input__wrapper) {
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: none;
}

.search-box :deep(.el-input__wrapper:hover) {
  border-color: rgba(102, 126, 234, 0.5);
}

.search-box :deep(.el-input__wrapper.is-focus) {
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.search-box :deep(.el-input__inner) {
  color: #fff;
}

.search-box :deep(.el-input-group__append) {
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-left: none;
}

.header-right {
  display: flex;
  align-items: center;
}

.user-info {
  display: flex;
  align-items: center;
  gap: 12px;
  cursor: pointer;
  padding: 8px 12px;
  border-radius: 10px;
  transition: background 0.3s;
}

.user-info:hover {
  background: rgba(255, 255, 255, 0.05);
}

.avatar {
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.3), rgba(118, 75, 162, 0.3));
}

.user-detail {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.username {
  color: #fff;
  font-size: 14px;
  font-weight: 500;
}

.dropdown-icon {
  color: rgba(255, 255, 255, 0.5);
}

.main-content {
  padding: 30px;
}

.content-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 30px;
}

.page-title {
  font-size: 24px;
  color: #fff;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 10px;
  margin: 0;
}

.category-tabs {
  display: flex;
  gap: 10px;
}

.category-tag {
  cursor: pointer;
  transition: all 0.3s;
}

.games-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 20px;
}

.game-card {
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 16px;
  padding: 20px;
  display: flex;
  align-items: center;
  gap: 16px;
  cursor: pointer;
  transition: all 0.3s;
}

.game-card:hover {
  background: rgba(255, 255, 255, 0.06);
  border-color: rgba(102, 126, 234, 0.3);
  transform: translateY(-4px);
  box-shadow: 0 10px 40px rgba(102, 126, 234, 0.1);
}

.game-icon {
  width: 64px;
  height: 64px;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.2), rgba(118, 75, 162, 0.2));
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 32px;
}

.game-info {
  flex: 1;
}

.game-name {
  font-size: 18px;
  color: #fff;
  font-weight: 600;
  margin: 0 0 8px 0;
}

.game-meta {
  display: flex;
  align-items: center;
  gap: 12px;
}

.players {
  display: flex;
  align-items: center;
  gap: 4px;
  color: rgba(255, 255, 255, 0.5);
  font-size: 13px;
}

.play-btn {
  background: linear-gradient(90deg, #667eea, #764ba2);
  border: none;
  padding: 8px 20px;
}

.floating-status {
  position: fixed;
  bottom: 30px;
  right: 30px;
  background: rgba(26, 26, 46, 0.95);
  backdrop-filter: blur(20px);
  border: 1px solid rgba(102, 126, 234, 0.3);
  border-radius: 16px;
  padding: 16px 20px;
  box-shadow: 0 10px 40px rgba(102, 126, 234, 0.2);
  z-index: 1000;
}

.floating-content {
  display: flex;
  align-items: center;
  gap: 16px;
}

.floating-icon {
  width: 44px;
  height: 44px;
  background: linear-gradient(135deg, #667eea, #764ba2);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
}

.pulse {
  animation: pulse 1.5s infinite;
}

@keyframes pulse {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.5; }
}

.floating-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.floating-game {
  color: #fff;
  font-weight: 600;
  font-size: 14px;
}

.floating-ping {
  color: #67c23a;
  font-size: 12px;
}
</style>
