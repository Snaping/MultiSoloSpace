<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content">
      <div class="modal-header">
        <div class="header-info">
          <h2>{{ topic?.title }}</h2>
          <span class="topic-type">{{ topic?.type }}</span>
        </div>
        <button class="close-btn" @click="$emit('close')">&times;</button>
      </div>
      
      <div class="modal-body">
        <div class="sessions-section" v-if="sessions.length > 0">
          <h3 class="section-label">活跃会话</h3>
          <div class="sessions-list">
            <div 
              v-for="session in sessions" 
              :key="session.id"
              class="session-item"
              @click="$emit('select', session)"
            >
              <div class="session-info">
                <div class="session-starter">
                  <span class="label">发起者:</span>
                  <span class="name">{{ session.starterName }}</span>
                </div>
                <div class="session-meta">
                  <span class="message-count">
                    {{ session.messageCount }} 条消息
                  </span>
                  <span class="time">{{ formatTime(session.createdAt) }}</span>
                </div>
              </div>
              <div class="session-arrow">&rarr;</div>
            </div>
          </div>
        </div>
        
        <div class="empty-section" v-else>
          <div class="empty-icon">💬</div>
          <p>该话题下暂无活跃会话</p>
        </div>
        
        <div class="divider">
          <span>或者</span>
        </div>
      </div>
      
      <div class="modal-footer">
        <button class="cancel-btn" @click="$emit('close')">取消</button>
        <button class="new-session-btn" @click="$emit('new')">
          开启新会话
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, watch, onMounted } from 'vue'
import { getSessionsByTopic } from '../api'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import 'dayjs/locale/zh-cn'

dayjs.extend(relativeTime)
dayjs.locale('zh-cn')

const props = defineProps({
  topic: {
    type: Object,
    required: true
  }
})

const emit = defineEmits(['close', 'select', 'new'])

const sessions = ref([])

function formatTime(time) {
  return dayjs(time).fromNow()
}

async function loadSessions() {
  if (!props.topic?.id) return
  
  try {
    const res = await getSessionsByTopic(props.topic.id)
    if (res.success) {
      sessions.value = res.data
    }
  } catch (error) {
    console.error('加载会话失败:', error)
  }
}

watch(() => props.topic, () => {
  loadSessions()
}, { immediate: true })

onMounted(() => {
  loadSessions()
})
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background-color: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
  padding: 20px;
}

.modal-content {
  background-color: #fff;
  border-radius: 16px;
  width: 100%;
  max-width: 500px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  padding: 24px;
  border-bottom: 1px solid #eee;
}

.header-info h2 {
  font-size: 20px;
  font-weight: 600;
  margin-bottom: 8px;
  word-break: break-word;
}

.topic-type {
  display: inline-block;
  padding: 4px 12px;
  background: #EFF6FF;
  color: #1D4ED8;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 600;
}

.close-btn {
  background: none;
  border: none;
  font-size: 28px;
  cursor: pointer;
  color: #999;
  padding: 0;
  line-height: 1;
  flex-shrink: 0;
  margin-left: 16px;
}

.close-btn:hover {
  color: #333;
}

.modal-body {
  padding: 24px;
}

.section-label {
  font-size: 14px;
  font-weight: 600;
  color: #666;
  margin-bottom: 12px;
}

.sessions-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.session-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px;
  background: #f9f9f9;
  border-radius: 12px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.session-item:hover {
  background: #f0f0f0;
  transform: translateX(4px);
}

.session-info {
  flex: 1;
}

.session-starter {
  margin-bottom: 8px;
}

.session-starter .label {
  font-size: 13px;
  color: #999;
  margin-right: 4px;
}

.session-starter .name {
  font-size: 15px;
  font-weight: 600;
}

.session-meta {
  display: flex;
  gap: 16px;
  font-size: 13px;
  color: #999;
}

.session-arrow {
  font-size: 20px;
  color: #ccc;
  flex-shrink: 0;
  margin-left: 16px;
}

.empty-section {
  text-align: center;
  padding: 40px 20px;
}

.empty-icon {
  font-size: 48px;
  margin-bottom: 16px;
}

.empty-section p {
  font-size: 15px;
  color: #999;
}

.divider {
  display: flex;
  align-items: center;
  margin: 24px 0;
}

.divider::before,
.divider::after {
  content: '';
  flex: 1;
  height: 1px;
  background: #eee;
}

.divider span {
  padding: 0 16px;
  font-size: 14px;
  color: #999;
}

.modal-footer {
  display: flex;
  gap: 12px;
  padding: 24px;
  border-top: 1px solid #eee;
}

.cancel-btn,
.new-session-btn {
  flex: 1;
  padding: 14px 24px;
  border-radius: 8px;
  font-size: 16px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.cancel-btn {
  background: #f5f5f5;
  border: 1px solid #ddd;
  color: #666;
}

.cancel-btn:hover {
  background: #eee;
}

.new-session-btn {
  background: linear-gradient(135deg, #3B82F6 0%, #1D4ED8 100%);
  border: none;
  color: #fff;
}

.new-session-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
}

@media (max-width: 480px) {
  .modal-footer {
    flex-direction: column;
  }
  
  .modal-header {
    flex-direction: column;
    align-items: stretch;
  }
  
  .close-btn {
    position: absolute;
    top: 24px;
    right: 24px;
  }
}
</style>
