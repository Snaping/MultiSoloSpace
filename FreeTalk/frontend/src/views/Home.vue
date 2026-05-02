<template>
  <div class="home-container" :style="containerStyle">
    <div class="create-section">
      <button 
        class="create-topic-btn"
        @click="showCreateTopicModal = true"
        :style="createBtnStyle"
      >
        <span class="icon">💬</span>
        <span>开启新话题</span>
        <span class="sparkle">✨</span>
      </button>
    </div>
    
    <div class="sessions-section" v-if="Object.keys(groupedSessions).length > 0">
      <div class="section-header">
        <h2 class="section-title">🔥 热门会话</h2>
        <span class="session-summary" :style="{ color: themeStyle.primary }">
          活跃 <strong>{{ totalSessions }}</strong> 个会话
        </span>
      </div>
      
      <div class="sessions-grid">
        <div 
          v-for="(group, type) in groupedSessions" 
          :key="type"
          class="session-type-card"
          :style="cardStyle"
        >
          <div class="card-header" :style="cardHeaderStyle">
            <div class="type-info">
              <span class="type-icon">{{ getTypeIcon(type) }}</span>
              <div class="type-text">
                <h3 class="type-title">{{ type }}</h3>
                <span class="type-count" :style="{ color: themeStyle.primary }">
                  {{ group.sessionCount }} 个会话 · {{ group.topicCount }} 个话题
                </span>
              </div>
            </div>
          </div>
          
          <div class="sessions-list">
            <div 
              v-for="session in group.sessions.slice(0, 3)" 
              :key="session.id"
              class="session-item"
              @click="enterSession(session)"
              :style="sessionItemStyle"
            >
              <div class="session-avatar" :style="avatarStyle">
                <span class="avatar-text">{{ getAvatarText(session.starterName) }}</span>
              </div>
              
              <div class="session-info">
                <h4 class="session-title">{{ session.topicTitle }}</h4>
                <div class="session-details">
                  <span class="session-starter">{{ session.starterName }} 发起</span>
                  <span class="dot">·</span>
                  <span class="message-count">
                    <span class="msg-icon">💬</span>
                    {{ session.messageCount }} 条消息
                  </span>
                </div>
              </div>
              
              <div class="session-right">
                <div class="online-badge" :class="{ active: session.onlineCount > 0 }">
                  <span class="online-dot"></span>
                  <span class="online-count">{{ session.onlineCount || 0 }}</span>
                </div>
                <div class="time-info">
                  <span class="time">{{ formatTime(session.createdAt) }}</span>
                </div>
              </div>
            </div>
            
            <div v-if="group.sessions.length > 3" class="more-sessions" :style="moreSessionsStyle">
              <span>还有 {{ group.sessions.length - 3 }} 个会话</span>
              <span class="more-arrow">↓</span>
            </div>
            
            <div v-if="group.sessions.length === 0" class="empty-sessions">
              <span class="empty-icon">🌟</span>
              <span>暂无活跃会话</span>
            </div>
          </div>
          
          <div class="card-footer">
            <button 
              class="create-session-btn"
              @click="startNewSession(type)"
              :style="sessionBtnStyle"
            >
              <span class="btn-icon">+</span>
              <span>在「{{ type }}」下开聊</span>
            </button>
          </div>
        </div>
      </div>
    </div>
    
    <div v-else class="empty-state">
      <div class="empty-state-content">
        <div class="empty-icon-big">🎉</div>
        <h2>欢迎来到 FreeTalk</h2>
        <p>这里还没有任何会话，来开启第一个吧！</p>
        <button 
          class="first-topic-btn"
          @click="showCreateTopicModal = true"
          :style="createBtnStyle"
        >
          <span>🎁</span>
          <span>开启第一个话题</span>
        </button>
      </div>
    </div>
    
    <CreateTopicModal 
      v-if="showCreateTopicModal"
      :topic-types="topicTypes"
      @close="showCreateTopicModal = false"
      @create="handleCreateTopic"
    />
    
    <SessionSelector 
      v-if="selectedTopic && showSessionSelector"
      :topic="selectedTopic"
      @close="showSessionSelector = false"
      @select="handleSelectSession"
      @new="handleNewSession"
    />
  </div>
</template>

<script setup>
import { ref, computed, inject, onMounted, onUnmounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { io } from 'socket.io-client'
import { getTopicsGrouped, getTopicTypes, createTopic, createSession, getSessionsGrouped } from '../api'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import 'dayjs/locale/zh-cn'
import CreateTopicModal from '../components/CreateTopicModal.vue'
import SessionSelector from '../components/SessionSelector.vue'

dayjs.extend(relativeTime)
dayjs.locale('zh-cn')

const router = useRouter()

const themeStyle = inject('themeStyle', ref({
  primary: '#1E3A8A',
  secondary: '#3B82F6',
  background: '#EFF6FF',
  backgroundColor: '#EFF6FF',
  accent: '#60A5FA',
  accentLight: '#E0E7FF',
  cardShadow: '0 4px 12px rgba(0,0,0,0.1)',
  cardBackground: '#fff',
  cardBorder: '1px solid #eee',
  buttonGradient: 'linear-gradient(135deg, #1E3A8A 0%, #3B82F6 100%)',
  buttonShadow: '0 4px 12px rgba(0,0,0,0.15)',
  borderRadius: '16px',
  transition: 'all 0.3s ease',
  font: 'sans-serif'
}))

const groupedSessions = ref({})
const topicTypes = ref([])
const showCreateTopicModal = ref(false)
const selectedTopic = ref(null)
const showSessionSelector = ref(false)

let socket = null
let refreshInterval = null

const totalSessions = computed(() => {
  let count = 0
  for (const group of Object.values(groupedSessions.value)) {
    count += group.sessionCount
  }
  return count
})

const containerStyle = computed(() => ({
  background: themeStyle.value.background,
  backgroundColor: themeStyle.value.backgroundColor,
  fontFamily: themeStyle.value.font,
  minHeight: '100%'
}))

const createBtnStyle = computed(() => ({
  background: themeStyle.value.buttonGradient,
  boxShadow: themeStyle.value.buttonShadow,
  color: '#fff'
}))

const sessionBtnStyle = computed(() => ({
  color: themeStyle.value.primary,
  borderColor: themeStyle.value.primary,
  backgroundColor: themeStyle.value.accentLight
}))

const cardStyle = computed(() => ({
  background: themeStyle.value.cardBackground,
  boxShadow: themeStyle.value.cardShadow,
  border: themeStyle.value.cardBorder,
  borderRadius: themeStyle.value.borderRadius || '16px',
  transition: themeStyle.value.transition || 'all 0.3s ease'
}))

const cardHeaderStyle = computed(() => ({
  background: `linear-gradient(135deg, ${themeStyle.value.primary}10 0%, ${themeStyle.value.secondary}08 100%)`,
  borderBottom: `2px solid ${themeStyle.value.primary}15`
}))

const sessionItemStyle = computed(() => ({
  borderLeft: `3px solid ${themeStyle.value.accentLight}`,
  transition: themeStyle.value.transition || 'all 0.3s ease'
}))

const avatarStyle = computed(() => ({
  background: themeStyle.value.buttonGradient
}))

const moreSessionsStyle = computed(() => ({
  color: themeStyle.value.primary,
  background: themeStyle.value.accentLight + '60'
}))

function getTypeIcon(type) {
  const icons = {
    '闲聊': '💭',
    '科技': '💻',
    '游戏': '🎮',
    '动漫': '🎌',
    '音乐': '🎵',
    '电影': '🎬',
    '美食': '🍜',
    '旅行': '✈️',
    '学习': '📚',
    '工作': '💼',
    '情感': '💕',
    '搞笑': '😂',
    '新闻': '📰',
    '体育': '⚽',
    '宠物': '🐱',
    '其他': '📦'
  }
  return icons[type] || '💬'
}

function getAvatarText(name) {
  if (!name) return '?'
  return name.charAt(0)
}

function formatTime(time) {
  return dayjs(time).fromNow()
}

async function loadSessions() {
  try {
    const [sessionsRes, typesRes] = await Promise.all([
      getSessionsGrouped(),
      getTopicTypes()
    ])
    
    if (sessionsRes.success) {
      groupedSessions.value = sessionsRes.data
    }
    
    if (typesRes.success) {
      topicTypes.value = typesRes.data
    }
  } catch (error) {
    console.error('加载会话失败:', error)
  }
}

function initWebSocket() {
  try {
    socket = io({
      transports: ['websocket', 'polling']
    })
    
    socket.on('onlineCountChanged', (data) => {
      const { sessionId, onlineCount } = data
      
      for (const type of Object.keys(groupedSessions.value)) {
        const sessions = groupedSessions.value[type].sessions
        for (const session of sessions) {
          if (session.id === sessionId) {
            session.onlineCount = onlineCount
            break
          }
        }
      }
    })
  } catch (error) {
    console.error('WebSocket连接失败:', error)
  }
}

function enterSession(session) {
  router.push(`/session/${session.id}`)
}

function selectTopic(topic) {
  selectedTopic.value = topic
  showSessionSelector.value = true
}

async function handleCreateTopic(type, title) {
  try {
    const res = await createTopic(type, title)
    if (res.success) {
      showCreateTopicModal.value = false
      
      const sessionRes = await createSession(res.data.topic.id)
      if (sessionRes.success) {
        router.push(`/session/${sessionRes.data.session.id}`)
      }
    }
  } catch (error) {
    console.error('创建话题失败:', error)
  }
}

function handleSelectSession(session) {
  router.push(`/session/${session.id}`)
}

async function handleNewSession() {
  if (!selectedTopic.value) return
  
  try {
    const res = await createSession(selectedTopic.value.id)
    if (res.success) {
      showSessionSelector.value = false
      router.push(`/session/${res.data.session.id}`)
    }
  } catch (error) {
    console.error('创建会话失败:', error)
  }
}

function startNewSession(type) {
  showCreateTopicModal.value = true
}

onMounted(() => {
  loadSessions()
  initWebSocket()
  
  refreshInterval = setInterval(() => {
    loadSessions()
  }, 15000)
})

onUnmounted(() => {
  if (refreshInterval) {
    clearInterval(refreshInterval)
  }
  if (socket) {
    socket.disconnect()
  }
})
</script>

<style scoped>
.home-container {
  max-width: 1400px;
  margin: 0 auto;
}

.create-section {
  margin-bottom: 40px;
  text-align: center;
}

.create-topic-btn {
  padding: 18px 48px;
  border: none;
  border-radius: 50px;
  color: #fff;
  font-size: 18px;
  font-weight: 700;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 12px;
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
}

.create-topic-btn::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(255,255,255,0.3), transparent);
  transition: left 0.5s ease;
}

.create-topic-btn:hover::before {
  left: 100%;
}

.create-topic-btn:hover {
  transform: translateY(-3px) scale(1.02);
}

.create-topic-btn .icon {
  font-size: 24px;
}

.sparkle {
  font-size: 18px;
  animation: sparkle 1.5s ease-in-out infinite;
}

@keyframes sparkle {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.6; transform: scale(1.2); }
}

.sessions-section {
  animation: fadeIn 0.5s ease;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(20px); }
  to { opacity: 1; transform: translateY(0); }
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  margin-bottom: 24px;
}

.section-title {
  font-size: 28px;
  font-weight: 800;
  margin: 0;
}

.session-summary {
  font-size: 15px;
  color: #888;
}

.sessions-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(380px, 1fr));
  gap: 28px;
}

.session-type-card {
  background: #fff;
  border-radius: 20px;
  overflow: hidden;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
}

.session-type-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 4px;
  background: linear-gradient(90deg, var(--primary-color, #1E3A8A), var(--secondary-color, #3B82F6));
  opacity: 0;
  transition: opacity 0.3s ease;
}

.session-type-card:hover::before {
  opacity: 1;
}

.session-type-card:hover {
  transform: translateY(-8px);
}

.card-header {
  padding: 20px 24px;
  border-bottom: 1px solid #f0f0f0;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.type-info {
  display: flex;
  align-items: center;
  gap: 14px;
}

.type-icon {
  font-size: 36px;
}

.type-text {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.type-title {
  font-size: 18px;
  font-weight: 700;
  margin: 0;
}

.type-count {
  font-size: 13px;
  color: #666;
}

.sessions-list {
  padding: 8px 0;
  max-height: 400px;
  overflow-y: auto;
}

.session-item {
  padding: 16px 24px;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  gap: 14px;
  border-left: 3px solid transparent;
}

.session-item:hover {
  background: linear-gradient(90deg, rgba(59, 130, 246, 0.05) 0%, transparent 100%);
  transform: translateX(4px);
}

.session-avatar {
  width: 48px;
  height: 48px;
  border-radius: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.avatar-text {
  color: #fff;
  font-weight: 700;
  font-size: 18px;
}

.session-info {
  flex: 1;
  min-width: 0;
}

.session-title {
  font-size: 15px;
  font-weight: 600;
  margin-bottom: 6px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.session-details {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: #888;
  flex-wrap: wrap;
}

.session-starter {
  font-weight: 500;
}

.dot {
  color: #ccc;
}

.message-count {
  display: flex;
  align-items: center;
  gap: 4px;
}

.msg-icon {
  font-size: 14px;
}

.session-right {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 6px;
  flex-shrink: 0;
}

.online-badge {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  background: #f5f5f5;
  border-radius: 20px;
  transition: all 0.2s ease;
}

.online-badge.active {
  background: linear-gradient(135deg, #DCFCE7 0%, #D1FAE5 100%);
}

.online-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #D1D5DB;
  transition: all 0.2s ease;
}

.online-badge.active .online-dot {
  background: #22C55E;
  box-shadow: 0 0 8px rgba(34, 197, 94, 0.5);
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% { transform: scale(1); opacity: 1; }
  50% { transform: scale(1.2); opacity: 0.8; }
}

.online-count {
  font-size: 13px;
  font-weight: 600;
  color: #666;
}

.online-badge.active .online-count {
  color: #166534;
}

.time-info {
  font-size: 12px;
  color: #aaa;
}

.more-sessions,
.empty-sessions {
  padding: 14px 24px;
  text-align: center;
  font-size: 14px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  cursor: pointer;
  transition: all 0.2s ease;
  margin: 8px 16px;
  border-radius: 10px;
}

.more-sessions:hover {
  transform: translateY(2px);
}

.more-arrow {
  animation: bounceArrow 1s ease-in-out infinite;
}

@keyframes bounceArrow {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(3px); }
}

.empty-sessions {
  flex-direction: column;
  gap: 8px;
  opacity: 0.6;
}

.empty-icon {
  font-size: 24px;
}

.card-footer {
  padding: 16px 24px 24px;
}

.create-session-btn {
  width: 100%;
  padding: 14px;
  background: transparent;
  border: 2px dashed;
  border-radius: 12px;
  font-size: 15px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.create-session-btn:hover {
  border-style: solid;
  transform: translateY(-2px);
}

.btn-icon {
  font-size: 18px;
  font-weight: 700;
}

.empty-state {
  text-align: center;
  padding: 100px 20px;
  display: flex;
  justify-content: center;
  align-items: center;
}

.empty-state-content {
  max-width: 400px;
  animation: fadeInUp 0.6s ease;
}

@keyframes fadeInUp {
  from { opacity: 0; transform: translateY(30px); }
  to { opacity: 1; transform: translateY(0); }
}

.empty-icon-big {
  font-size: 100px;
  margin-bottom: 32px;
  animation: float 3s ease-in-out infinite;
}

@keyframes float {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-10px); }
}

.empty-state h2 {
  font-size: 32px;
  font-weight: 800;
  margin-bottom: 16px;
}

.empty-state p {
  font-size: 18px;
  color: #888;
  margin-bottom: 40px;
}

.first-topic-btn {
  padding: 18px 48px;
  border: none;
  border-radius: 50px;
  color: #fff;
  font-size: 18px;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.3s ease;
  display: inline-flex;
  align-items: center;
  gap: 12px;
}

.first-topic-btn:hover {
  transform: translateY(-3px) scale(1.05);
}

@media (max-width: 768px) {
  .sessions-grid {
    grid-template-columns: 1fr;
    gap: 20px;
  }
  
  .create-section {
    margin-bottom: 32px;
  }
  
  .create-topic-btn {
    width: 100%;
    justify-content: center;
    padding: 16px 32px;
  }
  
  .section-header {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
  }
  
  .section-title {
    font-size: 24px;
  }
  
  .card-header {
    padding: 16px 20px;
  }
  
  .session-item {
    padding: 14px 20px;
  }
  
  .card-footer {
    padding: 12px 20px 20px;
  }
  
  .session-details {
    flex-direction: column;
    align-items: flex-start;
    gap: 2px;
  }
  
  .session-right {
    align-items: center;
  }
}
</style>
