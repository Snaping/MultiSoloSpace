import axios from 'axios'

const API_BASE_URL = '/api'

const api = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000
})

export async function getTopics() {
  const response = await api.get('/topics')
  return response.data
}

export async function getTopicsGrouped() {
  const response = await api.get('/topics/grouped')
  return response.data
}

export async function getTopicTypes() {
  const response = await api.get('/topics/types')
  return response.data
}

export async function createTopic(type, title) {
  const response = await api.post('/topics', { type, title })
  return response.data
}

export async function bubbleTopic(topicId) {
  const response = await api.post(`/topics/${topicId}/bubble`)
  return response.data
}

export async function getSessionsByTopic(topicId) {
  const response = await api.get(`/sessions/topic/${topicId}`)
  return response.data
}

export async function getSessionsGrouped() {
  const response = await api.get('/sessions/grouped')
  return response.data
}

export async function getSessionOnlineCount(sessionId) {
  const response = await api.get(`/sessions/${sessionId}/online`)
  return response.data
}

export async function createSession(topicId) {
  const response = await api.post('/sessions', { topicId })
  return response.data
}

export async function getMessages(sessionId, isVIP = false) {
  const response = await api.get(`/messages/session/${sessionId}`, {
    params: { vip: isVIP }
  })
  return response.data
}

export async function sendMessage(sessionId, content) {
  const response = await api.post('/messages', { sessionId, content })
  return response.data
}

export async function reactToMessage(messageId, reactionType) {
  const response = await api.post(`/messages/${messageId}/react`, { reactionType })
  return response.data
}

export async function getUserReaction(messageId) {
  const response = await api.get(`/messages/${messageId}/reaction`)
  return response.data
}

export async function getTheme() {
  const response = await api.get('/theme')
  return response.data
}

export async function getAllThemes() {
  const response = await api.get('/themes')
  return response.data
}

export async function switchTheme(themeId) {
  const response = await api.post(`/theme/switch/${themeId}`)
  return response.data
}

export async function getUserIdentity() {
  const response = await api.get('/user/identity')
  return response.data
}

const VIP_KEY = 'freetalk_vip_status'
const VIP_EXPIRY_KEY = 'freetalk_vip_expiry'

export function isVIP() {
  const status = localStorage.getItem(VIP_KEY)
  const expiry = localStorage.getItem(VIP_EXPIRY_KEY)
  
  if (status === 'true' && expiry) {
    const expiryDate = new Date(expiry)
    const now = new Date()
    
    if (now < expiryDate) {
      return true
    } else {
      localStorage.removeItem(VIP_KEY)
      localStorage.removeItem(VIP_EXPIRY_KEY)
      return false
    }
  }
  
  return false
}

export function setVIP(isVIP, durationDays = 30) {
  if (isVIP) {
    const expiryDate = new Date()
    expiryDate.setDate(expiryDate.getDate() + durationDays)
    
    localStorage.setItem(VIP_KEY, 'true')
    localStorage.setItem(VIP_EXPIRY_KEY, expiryDate.toISOString())
  } else {
    localStorage.removeItem(VIP_KEY)
    localStorage.removeItem(VIP_EXPIRY_KEY)
  }
}

export function getVIPExpiry() {
  const expiry = localStorage.getItem(VIP_EXPIRY_KEY)
  return expiry ? new Date(expiry) : null
}

export default api
