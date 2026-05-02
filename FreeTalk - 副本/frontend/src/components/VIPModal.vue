<template>
  <div class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content">
      <div class="modal-header">
        <h2>成为VIP会员</h2>
        <button class="close-btn" @click="$emit('close')">&times;</button>
      </div>
      
      <div class="modal-body">
        <div class="vip-features">
          <h3>VIP会员权益</h3>
          <ul>
            <li>
              <span class="icon">📜</span>
              <span>查看30天内的历史会话记录</span>
            </li>
            <li>
              <span class="icon">🎨</span>
              <span>专属主题配色方案</span>
            </li>
            <li>
              <span class="icon">⚡</span>
              <span>优先冒泡置顶权限</span>
            </li>
            <li>
              <span class="icon">🔔</span>
              <span>实时消息提醒</span>
            </li>
          </ul>
        </div>
        
        <div class="pricing">
          <div class="price-card selected">
            <div class="price-header">
              <span class="recommended">推荐</span>
            </div>
            <div class="price-content">
              <div class="duration">月度会员</div>
              <div class="price">
                <span class="currency">¥</span>
                <span class="amount">29</span>
                <span class="period">/月</span>
              </div>
              <div class="original-price">原价 ¥59</div>
            </div>
          </div>
          
          <div class="price-card">
            <div class="price-content">
              <div class="duration">年度会员</div>
              <div class="price">
                <span class="currency">¥</span>
                <span class="amount">199</span>
                <span class="period">/年</span>
              </div>
              <div class="original-price">原价 ¥599</div>
              <div class="saving">立省 ¥400</div>
            </div>
          </div>
        </div>
        
        <div class="payment-info">
          <p>💡 演示版本：点击下方按钮即可体验VIP功能</p>
        </div>
      </div>
      
      <div class="modal-footer">
        <button class="cancel-btn" @click="$emit('close')">取消</button>
        <button class="confirm-btn" @click="handleUpgrade">立即开通</button>
      </div>
    </div>
  </div>
</template>

<script setup>
const emit = defineEmits(['close', 'upgrade'])

function handleUpgrade() {
  emit('upgrade')
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
  max-width: 500px;
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

.vip-features h3 {
  font-size: 18px;
  font-weight: 600;
  margin-bottom: 16px;
}

.vip-features ul {
  list-style: none;
}

.vip-features li {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 0;
  border-bottom: 1px solid #f5f5f5;
}

.vip-features li:last-child {
  border-bottom: none;
}

.icon {
  font-size: 20px;
}

.pricing {
  display: flex;
  gap: 16px;
  margin: 24px 0;
}

.price-card {
  flex: 1;
  border: 2px solid #eee;
  border-radius: 12px;
  padding: 16px;
  text-align: center;
  position: relative;
  cursor: pointer;
  transition: all 0.2s ease;
}

.price-card:hover {
  border-color: #3B82F6;
}

.price-card.selected {
  border-color: #3B82F6;
  background: linear-gradient(135deg, #EFF6FF 0%, #DBEAFE 100%);
}

.recommended {
  position: absolute;
  top: -12px;
  left: 50%;
  transform: translateX(-50%);
  background: linear-gradient(135deg, #3B82F6 0%, #1D4ED8 100%);
  color: #fff;
  padding: 4px 12px;
  border-radius: 12px;
  font-size: 12px;
  font-weight: 600;
}

.duration {
  font-size: 14px;
  color: #666;
  margin-bottom: 8px;
}

.price {
  display: flex;
  align-items: baseline;
  justify-content: center;
  gap: 2px;
}

.currency {
  font-size: 16px;
  font-weight: 600;
  color: #333;
}

.amount {
  font-size: 32px;
  font-weight: 700;
  color: #1D4ED8;
}

.period {
  font-size: 14px;
  color: #666;
}

.original-price {
  font-size: 12px;
  color: #999;
  text-decoration: line-through;
  margin-top: 4px;
}

.saving {
  font-size: 12px;
  color: #10B981;
  font-weight: 600;
  margin-top: 4px;
}

.payment-info {
  background: #FFFBEB;
  border: 1px solid #FCD34D;
  border-radius: 8px;
  padding: 12px;
  text-align: center;
}

.payment-info p {
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

.confirm-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.4);
}

@media (max-width: 480px) {
  .pricing {
    flex-direction: column;
  }
  
  .modal-footer {
    flex-direction: column;
  }
}
</style>
