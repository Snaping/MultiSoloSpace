<template>
  <div class="home-container">
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
    
    <div class="topic-types-section" v-if="Object.keys(groupedTopics).length > 0">
      <div class="section-header">
        <h2 class="section-title">🔥 话题广场</h2>
        <span class="topic-summary">
          已有 <strong :style="{ color: themeStyle.primary }">{{ totalTopics }}</strong> 个活跃话题
        </span>
      </div>
      
      <div class="topic-types-grid">
        <div 
          v-for="(topics, type) in groupedTopics" 
          :key="type"
          class="topic-type-card"
          :style="cardStyle"
        >
          <div class="card-header" :style="cardHeaderStyle">
            <div class="type-info">
              <span class="type-icon">{{ getTypeIcon(type) }}</span>
              <h3 class="type-title">{{ type }}</h3>
            </div>
            <div class="card-badge" v-if="topics.length > 0">
              <span class="badge-count">{{ topics.length }}</span>
              <span class="badge-label">话题</span>
            </div>
          </div>
          
          <div class="topics-list">
            <div 
              v-for="topic in topics.slice(0, 5)" 
              :key="topic.id"
              class="topic-item"
              @click="selectTopic(topic)"
              :style="topicItemStyle"
            >
              <div class="topic-avatar">
                <span class="avatar-text">{{ getAvatarText(topic.creatorName) }}</span>
              </div>
              <div class="topic-info">
                <h4 class="topic-title">{{ topic.title }}</h4>
                <div class="topic-details">
                  <span class="topic-creator">{{ topic.creatorName }}</span>
                  <span class="dot">•</span>
                  <span class="time">{{ formatTime(topic.createdAt) }}</span>
                </div>
              </div>
              <div class="topic-arrow">
                <span>→</span>
              </div>
            </div>
            
            <div v-if="topics.length > 5" class="more-topics" :style="moreTopicsStyle">
              <span>还有 {{ topics.length - 5 }} 个话题</span>
              <span class="more-arrow">↓</span>
            </div>
            
            <div v-if="topics.length === 0" class="empty-topics">
              <span class="empty-icon">🌟</span>
              <span>暂无话题</span>
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
        <p>这里还没有任何话题，来开启第一个吧！</p>
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
import { ref, computed, inject, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { getTopicsGrouped, getTopicTypes, createTopic, createSession } from '../api'
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
  cardShadow: '0 4px 12px rgba(0,0,0,0.1)',
  cardBackground: '#fff',
  cardBorder: '1px solid #eee',
  buttonGradient: 'linear-gradient(135deg, #1E3A8A 0%, #3B82F6 100%)',
  buttonShadow: '0 4px 12px rgba(0,0,0,0.15)',
  accent: '#60A5FA',
  accentLight: '#E0E7FF'
}))

const groupedTopics = ref({})
const topicTypes = ref([])
const showCreateTopicModal = ref(false)
const selectedTopic = ref(null)
const showSessionSelector = ref(false)

let refreshInterval = null

const totalTopics = computed(() => {
  let count = 0
  for (const topics of Object.values(groupedTopics.value)) {
    count += topics.length
  }
  return count
})

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
  borderRadius: themeStyle.value.borderRadius || '16px'
}))

const cardHeaderStyle = computed(() => ({
  background: `linear-gradient(135deg, ${themeStyle.value.primary}15 0%, ${themeStyle.value.secondary}10 100%)`
}))

const topicItemStyle = computed(() => ({
  borderLeft: `3px solid ${themeStyle.value.accentLight}`
}))

const moreTopicsStyle = computed(() => ({
  color: themeStyle.value.primary,
  background: themeStyle.value.accentLight + '80'
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

async function loadTopics() {
  try {
    const [topicsRes, typesRes] = await Promise.all([
      getTopicsGrouped(),
      getTopicTypes()
    ])
    
    if (topicsRes.success) {
      groupedTopics.value = topicsRes.data.groupedTopics
    }
    
    if (typesRes.success) {
      topicTypes.value = typesRes.data
    }
  } catch (error) {
    console.error('加载话题失败:', error)
  }
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
  const topicsOfType = groupedTopics.value[type] || []
  if (topicsOfType.length > 0) {
    selectTopic(topicsOfType[0])
  } else {
    showCreateTopicModal.value = true
  }
}

onMounted(() => {
  loadTopics()
  
  refreshInterval = setInterval(() => {
    loadTopics()
  }, 30000)
})

onUnmounted(() => {
  if (refreshInterval) {
    clearInterval(refreshInterval)
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

.topic-types-section {
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

.topic-summary {
  font-size: 15px;
  color: #888;
}

.topic-types-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(360px, 1fr));
  gap: 28px;
}

.topic-type-card {
  background: #fff;
  border-radius: 20px;
  overflow: hidden;
  transition: all 0.3s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
}

.topic-type-card::before {
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

.topic-type-card:hover::before {
  opacity: 1;
}

.topic-type-card:hover {
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
  gap: 12px;
}

.type-icon {
  font-size: 32px;
}

.type-title {
  font-size: 18px;
  font-weight: 700;
  margin: 0;
}

.card-badge {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
  padding: 6px 12px;
  background: linear-gradient(135deg, #f0f9ff 0%, #e0f2fe 100%);
  border-radius: 12px;
}

.badge-count {
  font-size: 18px;
  font-weight: 800;
}

.badge-label {
  font-size: 11px;
  color: #666;
  font-weight: 500;
}

.topics-list {
  padding: 8px 0;
  max-height: 320px;
  overflow-y: auto;
}

.topic-item {
  padding: 14px 24px;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  gap: 14px;
  border-left: 3px solid transparent;
}

.topic-item:hover {
  background: linear-gradient(90deg, rgba(59, 130, 246, 0.05) 0%, transparent 100%);
  transform: translateX(4px);
}

.topic-avatar {
  width: 44px;
  height: 44px;
  border-radius: 12px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.avatar-text {
  color: #fff;
  font-weight: 700;
  font-size: 16px;
}

.topic-info {
  flex: 1;
  min-width: 0;
}

.topic-title {
  font-size: 15px;
  font-weight: 600;
  margin-bottom: 4px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.topic-details {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 13px;
  color: #888;
}

.topic-creator {
  font-weight: 500;
}

.dot {
  color: #ccc;
}

.topic-arrow {
  flex-shrink: 0;
  opacity: 0.3;
  font-size: 18px;
  transition: all 0.2s ease;
}

.topic-item:hover .topic-arrow {
  opacity: 1;
  transform: translateX(4px);
}

.more-topics,
.empty-topics {
  padding: 16px 24px;
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

.more-topics:hover {
  transform: translateY(2px);
}

.more-arrow {
  animation: bounceArrow 1s ease-in-out infinite;
}

@keyframes bounceArrow {
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(3px); }
}

.empty-topics {
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
  .topic-types-grid {
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
  
  .topic-item {
    padding: 12px 20px;
  }
  
  .card-footer {
    padding: 12px 20px 20px;
  }
}
</style>
