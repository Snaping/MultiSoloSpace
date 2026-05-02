<template>
  <div class="session-container">
    <div class="session-header">
      <button class="back-btn" @click="goBack">
        <span>&larr;</span>
        <span>返回</span>
      </button>
      
      <div class="session-info">
        <h1 class="session-title">{{ sessionInfo?.title || '会话中' }}</h1>
        <div class="session-meta">
          <span class="topic-type" v-if="sessionInfo?.type">{{ sessionInfo.type }}</span>
          <span class="message-limit" v-if="!isVIP">仅显示最近50条消息</span>
          <span class="vip-hint" v-if="isVIP">VIP: 可查看30天历史</span>
        </div>
      </div>
    </div>
    
    <div class="messages-container" ref="messagesContainer">
      <div class="empty-messages" v-if="messages.length === 0">
        <div class="empty-icon">💬</div>
        <h3>会话已开启</h3>
        <p>来发布第一条消息吧！</p>
      </div>
      
      <div 
        v-for="message in messages" 
        :key="message.id"
        class="message-item"
        :class="{ 'own-message': isOwnMessage(message) }"
      >
        <div class="message-avatar">
          <span class="avatar-initial">{{ getInitial(message.senderName) }}</span>
        </div>
        
        <div class="message-content-wrapper">
          <div class="message-header">
            <span class="sender-name">{{ message.senderName }}</span>
            <span class="message-time">{{ formatTime(message.createdAt) }}</span>
          </div>
          
          <div class="message-bubble">
            <p class="message-text">{{ message.content }}</p>
          </div>
          
          <div class="message-actions">
            <button 
              class="action-btn like-btn"
              :class="{ active: getUserReaction(message.id) === 'like' }"
              @click="handleReaction(message, 'like')"
            >
              <span class="icon">👍</span>
              <span class="count">{{ message.likes }}</span>
            </button>
            
            <button 
              class="action-btn dislike-btn"
              :class="{ active: getUserReaction(message.id) === 'dislike' }"
              @click="handleReaction(message, 'dislike')"
            >
              <span class="icon">👎</span>
              <span class="count">{{ message.dislikes }}</span>
            </button>
          </div>
        </div>
      </div>
      
      <div v-if="!isVIP && hasMoreHistory" class="history-hint">
        <p>还有更多历史消息</p>
        <button class="upgrade-btn" @click="showVIPHint = true">
          升级VIP查看完整历史
        </button>
      </div>
    </div>
    
    <div class="input-section">
      <div class="input-wrapper">
        <textarea
          v-model="inputText"
          class="message-input"
          placeholder="输入消息..."
          maxlength="500"
          @keydown.enter.exact="handleSend"
          @keydown.enter.shift="allowNewline"
          ref="inputRef"
        ></textarea>
        
        <div class="input-actions">
          <span class="char-count">{{ inputText.length }}/500</span>
          <button 
            class="send-btn"
            :disabled="!canSend"
            @click="handleSend"
          >
            发送
          </button>
        </div>
      </div>
      
      <div class="input-tip">
        <span class="tip-text">💡 按 Enter 发送，Shift + Enter 换行</span>
        <span class="user-id">我的身份: {{ userName }}</span>
      </div>
    </div>
    
    <div v-if="showVIPHint" class="vip-hint-overlay" @click="showVIPHint = false">
      <div class="vip-hint-content" @click.stop>
        <h3>升级VIP会员</h3>
        <p>成为VIP会员后，您可以查看会话的完整历史记录（最多30天）。</p>
        <div class="vip-features">
          <div class="feature-item">
            <span class="feature-icon">📜</span>
            <span>查看30天历史消息</span>
          </div>
          <div class="feature-item">
            <span class="feature-icon">🎨</span>
            <span>专属主题配色</span>
          </div>
          <div class="feature-item">
            <span class="feature-icon">⚡</span>
            <span>优先冒泡置顶</span>
          </div>
        </div>
        <div class="vip-actions">
          <button class="cancel-btn" @click="showVIPHint = false">暂不升级</button>
          <button class="upgrade-btn" @click="handleUpgradeVIP">立即升级 ¥29/月</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, inject, onMounted, onUnmounted, nextTick, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { io } from 'socket.io-client'
import { getMessages, getUserReaction as fetchUserReaction, setVIP, isVIP as checkVIP } from '../api'
import dayjs from 'dayjs'
import relativeTime from 'dayjs/plugin/relativeTime'
import 'dayjs/locale/zh-cn'

dayjs.extend(relativeTime)
dayjs.locale('zh-cn')

const route = useRoute()
const router = useRouter()

const themeStyle = inject('themeStyle', ref({}))
const userName = inject('userName', ref('访客'))

const sessionId = computed(() => route.params.sessionId)
const messages = ref([])
const inputText = ref('')
const messagesContainer = ref(null)
const inputRef = ref(null)
const socket = ref(null)
const isVIP = ref(false)
const hasMoreHistory = ref(false)
const showVIPHint = ref(false)
const sessionInfo = ref(null)
const userReactions = ref(new Map())

const canSend = computed(() => {
  return inputText.value.trim().length > 0
})

function getInitial(name) {
  if (!name) return '?'
  return name.charAt(0)
}

function formatTime(time) {
  return dayjs(time).format('HH:mm')
}

function isOwnMessage(message) {
  return message.senderName === userName.value
}

function getUserReaction(messageId) {
  return userReactions.value.get(messageId) || null
}

function allowNewline(event) {
}

async function handleSend() {
  if (!canSend.value || !socket.value) return
  
  const content = inputText.value.trim()
  if (!content) return
  
  socket.value.emit('sendMessage', {
    sessionId: sessionId.value,
    content: content
  })
  
  inputText.value = ''
  nextTick(() => {
    if (inputRef.value) {
      inputRef.value.focus()
    }
  })
}

function handleReaction(message, reactionType) {
  if (!socket.value) return
  
  const currentReaction = getUserReaction(message.id)
  
  socket.value.emit('reactToMessage', {
    messageId: message.id,
    sessionId: sessionId.value,
    reactionType: reactionType
  })
}

function handleUpgradeVIP() {
  setVIP(true, 30)
  isVIP.value = true
  showVIPHint.value = false
  loadMessages()
}

function goBack() {
  router.push('/')
}

async function loadMessages() {
  try {
    const res = await getMessages(sessionId.value, isVIP.value)
    if (res.success) {
      messages.value = res.data.messages
      
      if (!isVIP.value && res.data.messages.length >= 50) {
        hasMoreHistory.value = true
      } else {
        hasMoreHistory.value = false
      }
      
      nextTick(() => {
        scrollToBottom()
      })
    }
  } catch (error) {
    console.error('加载消息失败:', error)
  }
}

function scrollToBottom() {
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
  }
}

function initSocket() {
  socket.value = io({
    transports: ['websocket', 'polling']
  })
  
  socket.value.on('connect', () => {
    console.log('WebSocket connected')
    
    socket.value.emit('joinSession', {
      sessionId: sessionId.value,
      isVIP: isVIP.value
    })
  })
  
  socket.value.on('initialMessages', (msgs) => {
    messages.value = msgs
    nextTick(() => {
      scrollToBottom()
    })
  })
  
  socket.value.on('newMessage', (message) => {
    messages.value.push(message)
    
    if (messages.value.length > 50 && !isVIP.value) {
      messages.value.shift()
    }
    
    nextTick(() => {
      scrollToBottom()
    })
  })
  
  socket.value.on('reactionUpdated', (data) => {
    const { messageId, reactionType } = data
    
    const message = messages.value.find(m => m.id === messageId)
    if (message) {
      const oldReaction = userReactions.value.get(messageId)
      
      if (oldReaction === 'like') {
        message.likes = Math.max(0, message.likes - 1)
      } else if (oldReaction === 'dislike') {
        message.dislikes = Math.max(0, message.dislikes - 1)
      }
      
      if (reactionType === 'like') {
        message.likes++
      } else if (reactionType === 'dislike') {
        message.dislikes++
      }
      
      if (reactionType) {
        userReactions.value.set(messageId, reactionType)
      } else {
        userReactions.value.delete(messageId)
      }
    }
  })
  
  socket.value.on('error', (error) => {
    console.error('WebSocket error:', error)
  })
  
  socket.value.on('disconnect', () => {
    console.log('WebSocket disconnected')
  })
}

onMounted(() => {
  isVIP.value = checkVIP()
  
  loadMessages()
  initSocket()
  
  nextTick(() => {
    if (inputRef.value) {
      inputRef.value.focus()
    }
  })
})

onUnmounted(() => {
  if (socket.value) {
    socket.value.emit('leaveSession', { sessionId: sessionId.value })
    socket.value.disconnect()
  }
})
</script>

<style scoped>
.session-container {
  display: flex;
  flex-direction: column;
  height: calc(100vh - 120px);
  max-width: 900px;
  margin: 0 auto;
  background: #fff;
  border-radius: 16px;
  overflow: hidden;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.1);
}

.session-header {
  display: flex;
  align-items: center;
  padding: 16px 24px;
  border-bottom: 1px solid #f5f5f5;
  background: #fafafa;
}

.back-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  background: #fff;
  border: 1px solid #ddd;
  border-radius: 8px;
  cursor: pointer;
  font-size: 14px;
  transition: all 0.2s ease;
  margin-right: 16px;
}

.back-btn:hover {
  background: #f5f5f5;
}

.session-info {
  flex: 1;
}

.session-title {
  font-size: 18px;
  font-weight: 600;
  margin-bottom: 4px;
}

.session-meta {
  display: flex;
  align-items: center;
  gap: 12px;
}

.topic-type {
  padding: 2px 10px;
  background: #EFF6FF;
  color: #1D4ED8;
  border-radius: 20px;
  font-size: 12px;
  font-weight: 600;
}

.message-limit {
  font-size: 12px;
  color: #999;
}

.vip-hint {
  font-size: 12px;
  color: #10B981;
  font-weight: 600;
}

.messages-container {
  flex: 1;
  overflow-y: auto;
  padding: 24px;
  background: #fafafa;
}

.empty-messages {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  text-align: center;
  color: #999;
}

.empty-icon {
  font-size: 64px;
  margin-bottom: 16px;
}

.empty-messages h3 {
  font-size: 20px;
  font-weight: 600;
  margin-bottom: 8px;
  color: #333;
}

.message-item {
  display: flex;
  gap: 12px;
  margin-bottom: 24px;
}

.message-item.own-message {
  flex-direction: row-reverse;
}

.message-avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
  background: linear-gradient(135deg, #3B82F6 0%, #1D4ED8 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.avatar-initial {
  color: #fff;
  font-weight: 600;
  font-size: 16px;
}

.message-content-wrapper {
  max-width: 70%;
}

.own-message .message-content-wrapper {
  text-align: right;
}

.message-header {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 6px;
}

.own-message .message-header {
  flex-direction: row-reverse;
}

.sender-name {
  font-size: 13px;
  font-weight: 600;
  color: #555;
}

.message-time {
  font-size: 12px;
  color: #aaa;
}

.message-bubble {
  background: #fff;
  border-radius: 16px;
  padding: 12px 16px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
  display: inline-block;
  max-width: 100%;
}

.own-message .message-bubble {
  background: linear-gradient(135deg, #3B82F6 0%, #1D4ED8 100%);
}

.message-text {
  font-size: 15px;
  line-height: 1.5;
  word-break: break-word;
  color: #333;
  white-space: pre-wrap;
}

.own-message .message-text {
  color: #fff;
}

.message-actions {
  display: flex;
  gap: 8px;
  margin-top: 8px;
}

.own-message .message-actions {
  justify-content: flex-end;
}

.action-btn {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px 8px;
  background: transparent;
  border: 1px solid transparent;
  border-radius: 16px;
  cursor: pointer;
  font-size: 13px;
  transition: all 0.2s ease;
}

.action-btn:hover {
  background: rgba(0, 0, 0, 0.05);
}

.action-btn.active {
  background: #EFF6FF;
  border-color: #3B82F6;
}

.action-btn .icon {
  font-size: 14px;
}

.action-btn .count {
  color: #666;
}

.history-hint {
  text-align: center;
  padding: 16px;
  margin-top: 16px;
  background: #FFFBEB;
  border-radius: 8px;
}

.history-hint p {
  font-size: 14px;
  color: #92400E;
  margin-bottom: 8px;
}

.upgrade-btn {
  padding: 8px 16px;
  background: linear-gradient(135deg, #3B82F6 0%, #1D4ED8 100%);
  border: none;
  border-radius: 20px;
  color: #fff;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
}

.input-section {
  padding: 16px 24px;
  border-top: 1px solid #f5f5f5;
  background: #fff;
}

.input-wrapper {
  background: #f5f5f5;
  border-radius: 12px;
  padding: 12px;
}

.message-input {
  width: 100%;
  min-height: 60px;
  max-height: 120px;
  border: none;
  background: transparent;
  font-size: 15px;
  resize: vertical;
  outline: none;
  font-family: inherit;
}

.message-input::placeholder {
  color: #999;
}

.input-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 8px;
  padding-top: 8px;
  border-top: 1px solid #e5e5e5;
}

.char-count {
  font-size: 12px;
  color: #999;
}

.send-btn {
  padding: 8px 24px;
  background: linear-gradient(135deg, #3B82F6 0%, #1D4ED8 100%);
  border: none;
  border-radius: 20px;
  color: #fff;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
}

.send-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
}

.send-btn:disabled {
  background: #ccc;
  cursor: not-allowed;
}

.input-tip {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 12px;
  font-size: 12px;
  color: #999;
}

.user-id {
  font-weight: 500;
  color: #666;
}

.vip-hint-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 1000;
  padding: 20px;
}

.vip-hint-content {
  background: #fff;
  border-radius: 16px;
  padding: 32px;
  max-width: 400px;
  width: 100%;
}

.vip-hint-content h3 {
  font-size: 24px;
  font-weight: 700;
  margin-bottom: 12px;
}

.vip-hint-content p {
  font-size: 15px;
  color: #666;
  margin-bottom: 24px;
}

.vip-features {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 24px;
}

.feature-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 0;
}

.feature-icon {
  font-size: 20px;
}

.vip-actions {
  display: flex;
  gap: 12px;
}

.vip-actions .cancel-btn {
  flex: 1;
  padding: 12px;
  background: #f5f5f5;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
}

.vip-actions .upgrade-btn {
  flex: 1;
  padding: 12px;
  background: linear-gradient(135deg, #3B82F6 0%, #1D4ED8 100%);
  border: none;
  border-radius: 8px;
  color: #fff;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
}

@media (max-width: 768px) {
  .session-container {
    height: calc(100vh - 160px);
    border-radius: 0;
    margin: 0 -16px;
  }
  
  .session-header {
    padding: 12px 16px;
  }
  
  .session-title {
    font-size: 16px;
  }
  
  .messages-container {
    padding: 16px;
  }
  
  .message-content-wrapper {
    max-width: 85%;
  }
  
  .input-section {
    padding: 12px 16px;
  }
}
</style>
