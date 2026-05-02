<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content">
      <div class="modal-header">
        <h2>开启新话题</h2>
        <button class="close-btn" @click="$emit('close')">&times;</button>
      </div>
      
      <div class="modal-body">
        <div class="form-group">
          <label class="form-label">选择话题类型</label>
          <div class="type-grid">
            <button
              v-for="type in topicTypes"
              :key="type"
              class="type-btn"
              :class="{ selected: selectedType === type }"
              @click="selectedType = type"
            >
              {{ type }}
            </button>
          </div>
        </div>
        
        <div class="form-group">
          <label class="form-label">话题标题</label>
          <input
            v-model="title"
            type="text"
            class="form-input"
            placeholder="输入话题标题，比如：今天天气怎么样？"
            maxlength="50"
          />
          <div class="char-count">{{ title.length }}/50</div>
        </div>
        
        <div class="form-tip">
          💡 提示：话题创建后，其他用户可以在这个话题下开启会话聊天
        </div>
      </div>
      
      <div class="modal-footer">
        <button class="cancel-btn" @click="$emit('close')">取消</button>
        <button 
          class="confirm-btn" 
          @click="handleCreate"
          :disabled="!canCreate"
        >
          创建话题
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'

const props = defineProps({
  topicTypes: {
    type: Array,
    required: true
  }
})

const emit = defineEmits(['close', 'create'])

const selectedType = ref('')
const title = ref('')

const canCreate = computed(() => {
  return selectedType.value && title.value.trim().length > 0
})

function handleCreate() {
  if (!canCreate.value) return
  emit('create', selectedType.value, title.value.trim())
}
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
  max-width: 600px;
  max-height: 90vh;
  overflow-y: auto;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 24px;
  border-bottom: 1px solid #eee;
}

.modal-header h2 {
  font-size: 24px;
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
}

.close-btn:hover {
  color: #333;
}

.modal-body {
  padding: 24px;
}

.form-group {
  margin-bottom: 24px;
}

.form-label {
  display: block;
  font-size: 16px;
  font-weight: 600;
  margin-bottom: 12px;
}

.type-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(100px, 1fr));
  gap: 8px;
}

.type-btn {
  padding: 10px 16px;
  border: 1px solid #ddd;
  background: #fff;
  border-radius: 8px;
  font-size: 14px;
  cursor: pointer;
  transition: all 0.2s ease;
}

.type-btn:hover {
  border-color: #3B82F6;
  background: #EFF6FF;
}

.type-btn.selected {
  border-color: #3B82F6;
  background: #3B82F6;
  color: #fff;
}

.form-input {
  width: 100%;
  padding: 14px 16px;
  border: 1px solid #ddd;
  border-radius: 8px;
  font-size: 16px;
  outline: none;
  transition: border-color 0.2s ease;
}

.form-input:focus {
  border-color: #3B82F6;
}

.form-input::placeholder {
  color: #999;
}

.char-count {
  text-align: right;
  font-size: 12px;
  color: #999;
  margin-top: 4px;
}

.form-tip {
  background: #FFFBEB;
  border: 1px solid #FCD34D;
  border-radius: 8px;
  padding: 12px;
  font-size: 14px;
  color: #92400E;
}

.modal-footer {
  display: flex;
  gap: 12px;
  padding: 24px;
  border-top: 1px solid #eee;
}

.cancel-btn,
.confirm-btn {
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

.confirm-btn {
  background: linear-gradient(135deg, #3B82F6 0%, #1D4ED8 100%);
  border: none;
  color: #fff;
}

.confirm-btn:hover:not(:disabled) {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
}

.confirm-btn:disabled {
  background: #ccc;
  cursor: not-allowed;
}

@media (max-width: 480px) {
  .modal-footer {
    flex-direction: column;
  }
  
  .type-grid {
    grid-template-columns: repeat(3, 1fr);
  }
}
</style>
