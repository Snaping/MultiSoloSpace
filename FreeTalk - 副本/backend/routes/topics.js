const express = require('express');
const router = express.Router();
const { createTopic, getTopics, getTopicsByType, getTopicTypes, bubbleTopic } = require('../data/store');
const { generateNameFromIP, getClientIP } = require('../utils/nameGenerator');
const { getDailyTheme } = require('../config/themes');

router.get('/types', (req, res) => {
  const types = getTopicTypes();
  res.json({
    success: true,
    data: types
  });
});

router.get('/', (req, res) => {
  const topics = getTopics();
  const theme = getDailyTheme();
  
  res.json({
    success: true,
    data: {
      topics: topics,
      theme: {
        id: theme.id,
        name: theme.name,
        style: theme.style
      }
    }
  });
});

router.get('/grouped', (req, res) => {
  const groupedTopics = getTopicsByType();
  const theme = getDailyTheme();
  
  res.json({
    success: true,
    data: {
      groupedTopics: groupedTopics,
      theme: {
        id: theme.id,
        name: theme.name,
        style: theme.style
      }
    }
  });
});

router.post('/', (req, res) => {
  const { type, title } = req.body;
  
  if (!type || !title) {
    return res.status(400).json({
      success: false,
      error: '话题类型和标题不能为空'
    });
  }
  
  const types = getTopicTypes();
  if (!types.includes(type)) {
    return res.status(400).json({
      success: false,
      error: '无效的话题类型'
    });
  }
  
  const clientIP = getClientIP(req);
  const { name: creatorName } = generateNameFromIP(clientIP);
  
  const topic = createTopic(type, title, clientIP, creatorName);
  
  res.json({
    success: true,
    data: {
      topic: topic,
      creatorName: creatorName
    }
  });
});

router.post('/:topicId/bubble', (req, res) => {
  const { topicId } = req.params;
  
  const topic = bubbleTopic(topicId);
  
  if (!topic) {
    return res.status(404).json({
      success: false,
      error: '话题不存在'
    });
  }
  
  res.json({
    success: true,
    data: topic
  });
});

module.exports = router;
