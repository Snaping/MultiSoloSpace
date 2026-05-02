<template>
  <div class="activate-container">
    <el-container>
      <el-header class="header">
        <div class="header-left">
          <el-button text @click="goBack" class="back-btn">
            <el-icon><ArrowLeft /></el-icon>
            返回
          </el-button>
          <h1 class="page-title">卡密激活</h1>
        </div>
      </el-header>

      <el-main class="main-content">
        <div class="content-wrapper">
          <div class="activate-card">
            <div class="card-header">
              <div class="icon-wrapper">
                <el-icon :size="48">
                  <Key />
                </el-icon>
              </div>
              <div class="header-info">
                <h2>激活VIP会员</h2>
                <p>输入您的卡密，解锁全部加速功能</p>
              </div>
            </div>

            <el-divider />

            <el-form
              ref="activateFormRef"
              :model="activateForm"
              :rules="activateRules"
              class="activate-form"
            >
              <el-form-item prop="code">
                <el-input
                  v-model="activateForm.code"
                  placeholder="请输入卡密，例如：TEST-DAY-001"
                  size="large"
                  prefix-icon="Key"
                  maxlength="30"
                  show-word-limit
                />
              </el-form-item>

              <el-form-item>
                <el-button
                  type="primary"
                  size="large"
                  :loading="activating"
                  class="activate-btn"
                  @click="handleActivate"
                >
                  <el-icon><Lightning /></el-icon>
                  立即激活
                </el-button>
              </el-form-item>
            </el-form>

            <el-divider content-position="left">
              <span class="divider-text">当前会员状态</span>
            </el-divider>

            <div class="vip-status-section">
              <div class="status-card" :class="{ 'vip-active': userStore.isVip }">
                <div class="status-icon">
                  <el-icon :size="36" v-if="userStore.isVip"><Crown /></el-icon>
                  <el-icon :size="36" v-else><User /></el-icon>
                </div>
                <div class="status-info">
                  <h3 class="status-title">
                    {{ userStore.isVip ? 'VIP会员' : '普通用户' }}
                  </h3>
                  <p class="status-desc" v-if="userStore.isVip && remainingTime">
                    剩余时长：<span class="highlight">{{ remainingTime.days }}天 {{ remainingTime.hours }}小时 {{ remainingTime.minutes }}分钟</span>
                  </p>
                  <p class="status-desc" v-else-if="!userStore.isVip">
                    激活卡密后享受全部加速功能
                  </p>
                  <p class="status-desc expired" v-else>
                    VIP已过期，请重新激活
                  </p>
                </div>
                <el-tag
                  :type="userStore.isVip ? 'success' : 'info'"
                  size="large"
                  effect="dark"
                  class="status-tag"
                >
                  {{ userStore.isVip ? '已激活' : '未激活' }}
                </el-tag>
              </div>
            </div>

            <el-divider content-position="left">
              <span class="divider-text">可用测试卡密</span>
            </el-divider>

            <div class="test-cards-section">
              <p class="tips-text">以下为测试用卡密，点击即可复制使用：</p>
              <div class="test-cards">
                <el-tag
                  v-for="card in testCards"
                  :key="card.code"
                  class="test-card"
                  effect="plain"
                  @click="copyCard(card.code)"
                >
                  <span class="card-code">{{ card.code }}</span>
                  <span class="card-duration">{{ card.duration }}天</span>
                  <el-tooltip content="点击复制">
                    <el-icon class="copy-icon"><CopyDocument /></el-icon>
                  </el-tooltip>
                </el-tag>
              </div>
            </div>
          </div>
        </div>
      </el-main>
    </el-container>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { useUserStore } from '@/stores/user'
import api from '@/api'

const router = useRouter()
const userStore = useUserStore()

const activateFormRef = ref(null)
const activating = ref(false)
const testCards = ref([])

const activateForm = ref({
  code: ''
})

const activateRules = {
  code: [
    { required: true, message: '请输入卡密', trigger: 'blur' },
    { min: 6, message: '卡密格式不正确', trigger: 'blur' }
  ]
}

const remainingTime = computed(() => userStore.getRemainingTime())

const goBack = () => {
  router.push('/home')
}

const loadTestCards = async () => {
  try {
    const res = await api.get('/test-cards')
    if (res.data.success) {
      testCards.value = res.data.data
    }
  } catch (e) {
    console.log('无法获取测试卡密')
    testCards.value = [
      { code: 'TEST-DAY-001', duration: 1 },
      { code: 'TEST-WEEK-001', duration: 7 },
      { code: 'TEST-MONTH-001', duration: 30 }
    ]
  }
}

const copyCard = (code) => {
  activateForm.value.code = code
  navigator.clipboard?.writeText(code)
  ElMessage.success(`已复制卡密: ${code}`)
}

const handleActivate = async () => {
  const valid = await activateFormRef.value.validate().catch(() => false)
  if (!valid) return

  activating.value = true
  const result = await userStore.activateCard(activateForm.value.code.trim().toUpperCase())
  activating.value = false

  if (result.success) {
    ElMessage.success(result.message)
    activateForm.value.code = ''
  } else {
    ElMessage.error(result.message)
  }
}

onMounted(() => {
  loadTestCards()
})
</script>

<style scoped>
.activate-container {
  min-height: 100vh;
  background: linear-gradient(135deg, #1a1a2e 0%, #16213e 50%, #0f3460 100%);
}

.header {
  height: 70px;
  background: rgba(255, 255, 255, 0.03);
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  display: flex;
  align-items: center;
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

.page-title {
  font-size: 20px;
  color: #fff;
  font-weight: 600;
  margin: 0;
}

.main-content {
  padding: 40px 30px;
}

.content-wrapper {
  max-width: 600px;
  margin: 0 auto;
}

.activate-card {
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 20px;
  padding: 40px;
}

.card-header {
  display: flex;
  align-items: center;
  gap: 20px;
}

.icon-wrapper {
  width: 80px;
  height: 80px;
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.2), rgba(118, 75, 162, 0.2));
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #667eea;
}

.header-info h2 {
  font-size: 24px;
  color: #fff;
  font-weight: 600;
  margin: 0 0 6px 0;
}

.header-info p {
  color: rgba(255, 255, 255, 0.5);
  font-size: 14px;
  margin: 0;
}

.divider-text {
  color: rgba(255, 255, 255, 0.6);
  font-size: 14px;
}

.activate-form {
  margin: 20px 0;
}

.activate-form :deep(.el-input__wrapper) {
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.1);
  box-shadow: none;
  padding: 4px 16px;
}

.activate-form :deep(.el-input__wrapper:hover) {
  border-color: rgba(102, 126, 234, 0.5);
}

.activate-form :deep(.el-input__wrapper.is-focus) {
  border-color: #667eea;
  box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
}

.activate-form :deep(.el-input__inner) {
  color: #fff;
  font-size: 16px;
}

.activate-form :deep(.el-input__prefix-inner) {
  color: rgba(255, 255, 255, 0.4);
}

.activate-btn {
  width: 100%;
  height: 52px;
  font-size: 17px;
  font-weight: 600;
  background: linear-gradient(90deg, #667eea, #764ba2);
  border: none;
  box-shadow: 0 4px 20px rgba(102, 126, 234, 0.4);
}

.activate-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 6px 25px rgba(102, 126, 234, 0.5);
}

.vip-status-section {
  margin-top: 10px;
}

.status-card {
  display: flex;
  align-items: center;
  gap: 20px;
  padding: 24px;
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 16px;
  transition: all 0.3s;
}

.status-card.vip-active {
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.1), rgba(118, 75, 162, 0.1));
  border-color: rgba(102, 126, 234, 0.3);
}

.status-icon {
  width: 70px;
  height: 70px;
  background: rgba(255, 255, 255, 0.05);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: rgba(255, 255, 255, 0.5);
}

.status-card.vip-active .status-icon {
  background: linear-gradient(135deg, rgba(102, 126, 234, 0.2), rgba(118, 75, 162, 0.2));
  color: #667eea;
}

.status-info {
  flex: 1;
}

.status-title {
  font-size: 18px;
  font-weight: 600;
  margin: 0 0 8px 0;
  color: #fff;
}

.status-desc {
  color: rgba(255, 255, 255, 0.5);
  font-size: 14px;
  margin: 0;
}

.status-desc .highlight {
  color: #67c23a;
  font-weight: 600;
}

.status-desc.expired {
  color: #f56c6c;
}

.status-tag {
  font-size: 14px;
  padding: 8px 16px;
}

.test-cards-section {
  margin-top: 10px;
}

.tips-text {
  color: rgba(255, 255, 255, 0.5);
  font-size: 13px;
  margin: 0 0 16px 0;
}

.test-cards {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
}

.test-card {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 16px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.3s;
  border-color: rgba(102, 126, 234, 0.3);
}

.test-card:hover {
  background: rgba(102, 126, 234, 0.1);
  border-color: #667eea;
  transform: translateY(-2px);
}

.card-code {
  font-family: 'Consolas', 'Monaco', monospace;
  color: #667eea;
  font-weight: 600;
}

.card-duration {
  color: rgba(255, 255, 255, 0.6);
  font-size: 12px;
}

.copy-icon {
  color: rgba(255, 255, 255, 0.4);
  font-size: 14px;
}
</style>
