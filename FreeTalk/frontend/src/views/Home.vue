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
      </button>
    </div>
    
    <div class="topic-types-section" v-if="Object.keys(groupedTopics).length > 0">
      <h2 class="section-title">话题广场</h2>
      
      <div class="topic-types-grid">
        <div 
          v-for="(topics, type) in groupedTopics" 
          :key="type"
          class="topic-type-card"
          :style="cardStyle"
        >
          <div class="card-header">
            <h3 class="type-title">{{ type }}</h3>
            <span class="topic-count">{{ topics.length }} 个话题</span>
          </div>
          
          <div class="topics-list">
            <div 
              v-for="topic in topics.slice(0, 5)" 
              :key="topic.id"
              class="topic-item"
              @click="selectTopic(topic)"
            >
              <div class="topic-info">
                <h4 class="topic-title">{{ topic.title }}</h4>
                <span class="topic-creator">by {{ topic.creatorName }}</span>
              </div>
              <div class="topic-meta">
                <span class="time">{{ formatTime(topic.createdAt) }}</span>
              </div>
            </div>
            
            <div v-if="topics.length > 5" class="more-topics">
              还有 {{ topics.length - 5 }} 个话题...
            </div>
            
            <div v-if="topics.length === 0" class="empty-topics">
              暂无话题，来开启第一个吧！
            </div>
          </div>
          
          <div class="card-footer">
            <button 
              class="create-session-btn"
              @click="startNewSession(type)"
              :style="sessionBtnStyle"
            >
              在「{{ type }}」下开聊
            </button>
          </div>
        </div>
      </div>
    </div>
    
    <div v-else class="empty-state">
      <div class="empty-icon">🎉</div>
      <h2>欢迎来到 FreeTalk</h2>
      <p>这里还没有任何话题，来开启第一个吧！</p>
      <button 
        class="first-topic-btn"
        @click="showCreateTopicModal = true"
        :style="createBtnStyle"
      >
        开启第一个话题
      </button>
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
  cardShadow: '0 4px 12px rgba(0,0,0,0.1)'
}))

const groupedTopics = ref({})
const topicTypes = ref([])
const showCreateTopicModal = ref(false)
const selectedTopic = ref(null)
const showSessionSelector = ref(false)

let refreshInterval = null

const createBtnStyle = computed(() => ({
  background: `linear-gradient(135deg, ${themeStyle.value.primary} 0%, ${themeStyle.value.secondary} 100%)`
}))

const sessionBtnStyle = computed(() => ({
  color: themeStyle.value.primary
}))

const cardStyle = computed(() => ({
  boxShadow: themeStyle.value.cardShadow
}))

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
  margin-bottom: 32px;
  text-align: center;
}

.create-topic-btn {
  padding: 16px 40px;
  border: none;
  border-radius: 30px;
  color: #fff;
  font-size: 18px;
  font-weight: 600;
  cursor: pointer;
  display: inline-flex;
  align-items: center;
  gap: 12px;
  transition: all 0.2s ease;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.create-topic-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.2);
}

.create-topic-btn .icon {
  font-size: 24px;
}

.section-title {
  font-size: 24px;
  font-weight: 700;
  margin-bottom: 24px;
}

.topic-types-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
  gap: 24px;
}

.topic-type-card {
  background: #fff;
  border-radius: 16px;
  overflow: hidden;
  transition: all 0.2s ease;
}

.topic-type-card:hover {
  transform: translateY(-4px);
}

.card-header {
  padding: 20px;
  border-bottom: 1px solid #f5f5f5;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.type-title {
  font-size: 18px;
  font-weight: 600;
}

.topic-count {
  font-size: 14px;
  color: #999;
}

.topics-list {
  padding: 8px 0;
  max-height: 300px;
  overflow-y: auto;
}

.topic-item {
  padding: 12px 20px;
  cursor: pointer;
  transition: background 0.2s ease;
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 12px;
}

.topic-item:hover {
  background: #f9f9f9;
}

.topic-info {
  flex: 1;
  min-width: 0;
}

.topic-title {
  font-size: 15px;
  font-weight: 500;
  margin-bottom: 4px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.topic-creator {
  font-size: 13px;
  color: #999;
}

.topic-meta {
  flex-shrink: 0;
}

.time {
  font-size: 12px;
  color: #bbb;
}

.more-topics,
.empty-topics {
  padding: 16px 20px;
  text-align: center;
  color: #999;
  font-size: 14px;
}

.card-footer {
  padding: 16px 20px;
  border-top: 1px solid #f5f5f5;
}

.create-session-btn {
  width: 100%;
  padding: 12px;
  background: none;
  border: 1px dashed;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.create-session-btn:hover {
  background: rgba(0, 0, 0, 0.02);
  border-style: solid;
}

.empty-state {
  text-align: center;
  padding: 80px 20px;
}

.empty-icon {
  font-size: 80px;
  margin-bottom: 24px;
}

.empty-state h2 {
  font-size: 28px;
  font-weight: 700;
  margin-bottom: 12px;
}

.empty-state p {
  font-size: 16px;
  color: #666;
  margin-bottom: 32px;
}

.first-topic-btn {
  padding: 16px 40px;
  border: none;
  border-radius: 30px;
  color: #fff;
  font-size: 18px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.first-topic-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 20px rgba(0, 0, 0, 0.2);
}

@media (max-width: 768px) {
  .topic-types-grid {
    grid-template-columns: 1fr;
    gap: 16px;
  }
  
  .create-topic-btn {
    width: 100%;
    justify-content: center;
  }
  
  .section-title {
    font-size: 20px;
  }
}
</style>
