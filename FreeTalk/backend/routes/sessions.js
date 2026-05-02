const express = require('express');
const router = express.Router();
const { createSession, getSessionsByTopic, getTopics } = require('../data/store');
const { generateNameFromIP, getClientIP } = require('../utils/nameGenerator');

router.get('/topic/:topicId', (req, res) => {
  const { topicId } = req.params;
  
  const sessions = getSessionsByTopic(topicId);
  
  res.json({
    success: true,
    data: sessions
  });
});

router.post('/', (req, res) => {
  const { topicId } = req.body;
  
  if (!topicId) {
    return res.status(400).json({
      success: false,
      error: '话题ID不能为空'
    });
  }
  
  const topics = getTopics();
  const topic = topics.find(t => t.id === topicId);
  
  if (!topic) {
    return res.status(404).json({
      success: false,
      error: '话题不存在'
    });
  }
  
  const clientIP = getClientIP(req);
  const { name: starterName } = generateNameFromIP(clientIP);
  
  const session = createSession(topicId, clientIP, starterName);
  
  res.json({
    success: true,
    data: {
      session: session,
      starterName: starterName
    }
  });
});

module.exports = router;
