const express = require('express');
const router = express.Router();
const { addMessage, getMessages, addReaction, getUserReaction } = require('../data/store');
const { generateNameFromIP, getClientIP } = require('../utils/nameGenerator');

router.get('/session/:sessionId', (req, res) => {
  const { sessionId } = req.params;
  const isVIP = req.query.vip === 'true';
  
  const messages = getMessages(sessionId, isVIP);
  
  res.json({
    success: true,
    data: {
      messages: messages,
      isVIP: isVIP,
      maxMessages: 50
    }
  });
});

router.post('/', (req, res) => {
  const { sessionId, content } = req.body;
  
  if (!sessionId || !content || !content.trim()) {
    return res.status(400).json({
      success: false,
      error: '会话ID和内容不能为空'
    });
  }
  
  if (content.trim().length > 500) {
    return res.status(400).json({
      success: false,
      error: '内容不能超过500个字符'
    });
  }
  
  const clientIP = getClientIP(req);
  const { name: senderName } = generateNameFromIP(clientIP);
  
  const message = addMessage(sessionId, content.trim(), clientIP, senderName);
  
  if (!message) {
    return res.status(404).json({
      success: false,
      error: '会话不存在'
    });
  }
  
  res.json({
    success: true,
    data: {
      message: message,
      senderName: senderName
    }
  });
});

router.post('/:messageId/react', (req, res) => {
  const { messageId } = req.params;
  const { reactionType } = req.body;
  
  if (!reactionType || !['like', 'dislike'].includes(reactionType)) {
    return res.status(400).json({
      success: false,
      error: '无效的反应类型，只能是 like 或 dislike'
    });
  }
  
  const clientIP = getClientIP(req);
  const currentReaction = getUserReaction(messageId, clientIP);
  
  if (currentReaction === reactionType) {
    return res.json({
      success: true,
      data: {
        message: '已取消反应',
        reactionType: null,
        wasChanged: false
      }
    });
  }
  
  const result = addReaction(messageId, reactionType, clientIP);
  
  res.json({
    success: true,
    data: {
      message: result ? '反应已更新' : '反应已添加',
      reactionType: result,
      wasChanged: currentReaction !== null
    }
  });
});

router.get('/:messageId/reaction', (req, res) => {
  const { messageId } = req.params;
  
  const clientIP = getClientIP(req);
  const reaction = getUserReaction(messageId, clientIP);
  
  res.json({
    success: true,
    data: {
      reactionType: reaction
    }
  });
});

module.exports = router;
