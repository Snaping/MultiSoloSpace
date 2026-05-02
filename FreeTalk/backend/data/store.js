const { v4: uuidv4 } = require('uuid');
const dayjs = require('dayjs');

const topics = new Map();
const sessions = new Map();
const messages = new Map();
const reactions = new Map();
const historyMessages = new Map();

const TOPIC_TYPES = [
  '闲聊', '科技', '游戏', '动漫', '音乐', '电影', '美食', '旅行',
  '学习', '工作', '情感', '搞笑', '新闻', '体育', '宠物', '其他'
];

const MAX_MESSAGES_PER_SESSION = 50;
const MESSAGE_EXPIRY_DAYS = 1;
const HISTORY_EXPIRY_DAYS = 30;

function createTopic(type, title, creatorIP, creatorName) {
  const topicId = uuidv4();
  const now = dayjs();
  
  const topic = {
    id: topicId,
    type: type,
    title: title,
    creatorIP: creatorIP,
    creatorName: creatorName,
    createdAt: now.toISOString(),
    lastBubbleTime: now.toISOString(),
    bubblePriority: Math.random()
  };
  
  topics.set(topicId, topic);
  
  return topic;
}

function getTopics() {
  const now = dayjs();
  const activeTopics = [];
  
  for (const [_, topic] of topics) {
    const createdAt = dayjs(topic.createdAt);
    if (now.diff(createdAt, 'day') < MESSAGE_EXPIRY_DAYS + 1) {
      activeTopics.push(topic);
    }
  }
  
  activeTopics.sort((a, b) => {
    const bubbleDiff = b.bubblePriority - a.bubblePriority;
    if (Math.abs(bubbleDiff) > 0.01) {
      return bubbleDiff;
    }
    
    const lastBubbleA = dayjs(a.lastBubbleTime);
    const lastBubbleB = dayjs(b.lastBubbleTime);
    return lastBubbleB - lastBubbleA;
  });
  
  return activeTopics;
}

function getTopicsByType() {
  const allTopics = getTopics();
  const grouped = {};
  
  for (const type of TOPIC_TYPES) {
    grouped[type] = [];
  }
  
  for (const topic of allTopics) {
    if (grouped[topic.type]) {
      grouped[topic.type].push(topic);
    } else {
      if (!grouped['其他']) grouped['其他'] = [];
      grouped['其他'].push(topic);
    }
  }
  
  return grouped;
}

function bubbleTopic(topicId) {
  const topic = topics.get(topicId);
  if (topic) {
    topic.lastBubbleTime = dayjs().toISOString();
    topic.bubblePriority = Math.random();
    return topic;
  }
  return null;
}

function createSession(topicId, starterIP, starterName) {
  const sessionId = uuidv4();
  const now = dayjs();
  
  const session = {
    id: sessionId,
    topicId: topicId,
    starterIP: starterIP,
    starterName: starterName,
    createdAt: now.toISOString(),
    lastBubbleTime: now.toISOString(),
    bubblePriority: Math.random(),
    messageCount: 0
  };
  
  sessions.set(sessionId, session);
  
  const sessionMessages = [];
  messages.set(sessionId, sessionMessages);
  
  return session;
}

function getSessionsByTopic(topicId) {
  const now = dayjs();
  const activeSessions = [];
  
  for (const [_, session] of sessions) {
    if (session.topicId === topicId) {
      const createdAt = dayjs(session.createdAt);
      if (now.diff(createdAt, 'day') < MESSAGE_EXPIRY_DAYS + 1) {
        activeSessions.push(session);
      }
    }
  }
  
  activeSessions.sort((a, b) => {
    const bubbleDiff = b.bubblePriority - a.bubblePriority;
    if (Math.abs(bubbleDiff) > 0.01) {
      return bubbleDiff;
    }
    
    const lastBubbleA = dayjs(a.lastBubbleTime);
    const lastBubbleB = dayjs(b.lastBubbleTime);
    return lastBubbleB - lastBubbleA;
  });
  
  return activeSessions;
}

function getSessionsGroupedByType() {
  const allTopics = getTopics();
  const grouped = {};
  
  for (const type of TOPIC_TYPES) {
    grouped[type] = {
      type: type,
      topics: [],
      sessions: []
    };
  }
  
  const topicMap = new Map();
  for (const topic of allTopics) {
    topicMap.set(topic.id, topic);
    if (grouped[topic.type]) {
      grouped[topic.type].topics.push(topic);
    } else {
      if (!grouped['其他']) {
        grouped['其他'] = { type: '其他', topics: [], sessions: [] };
      }
      grouped['其他'].topics.push(topic);
    }
  }
  
  for (const [sessionId, session] of sessions) {
    const topic = topicMap.get(session.topicId);
    if (!topic) continue;
    
    const typeGroup = grouped[topic.type] || grouped['其他'];
    if (typeGroup) {
      typeGroup.sessions.push({
        ...session,
        topicTitle: topic.title,
        topicCreator: topic.creatorName
      });
    }
  }
  
  for (const type of Object.keys(grouped)) {
    grouped[type].sessions.sort((a, b) => {
      const bubbleDiff = b.bubblePriority - a.bubblePriority;
      if (Math.abs(bubbleDiff) > 0.01) {
        return bubbleDiff;
      }
      return dayjs(b.lastBubbleTime) - dayjs(a.lastBubbleTime);
    });
  }
  
  return grouped;
}

function addMessage(sessionId, content, senderIP, senderName) {
  const sessionMessages = messages.get(sessionId);
  if (!sessionMessages) {
    return null;
  }
  
  const now = dayjs();
  const messageId = uuidv4();
  
  const message = {
    id: messageId,
    sessionId: sessionId,
    content: content,
    senderIP: senderIP,
    senderName: senderName,
    createdAt: now.toISOString(),
    likes: 0,
    dislikes: 0
  };
  
  sessionMessages.push(message);
  
  while (sessionMessages.length > MAX_MESSAGES_PER_SESSION) {
    const removedMessage = sessionMessages.shift();
    saveToHistory(removedMessage);
  }
  
  const session = sessions.get(sessionId);
  if (session) {
    session.messageCount = sessionMessages.length;
    session.lastBubbleTime = now.toISOString();
    session.bubblePriority = Math.random();
  }
  
  return message;
}

function getMessages(sessionId, isVIP = false) {
  const sessionMessages = messages.get(sessionId) || [];
  
  if (!isVIP) {
    return [...sessionMessages];
  }
  
  const historyMessagesForSession = historyMessages.get(sessionId) || [];
  const allMessages = [...historyMessagesForSession, ...sessionMessages];
  
  allMessages.sort((a, b) => {
    return dayjs(a.createdAt) - dayjs(b.createdAt);
  });
  
  return allMessages;
}

function saveToHistory(message) {
  const sessionId = message.sessionId;
  if (!historyMessages.has(sessionId)) {
    historyMessages.set(sessionId, []);
  }
  
  const sessionHistory = historyMessages.get(sessionId);
  sessionHistory.push({...message});
  
  const thirtyDaysAgo = dayjs().subtract(HISTORY_EXPIRY_DAYS, 'day');
  while (sessionHistory.length > 0) {
    const oldest = sessionHistory[0];
    if (dayjs(oldest.createdAt).isBefore(thirtyDaysAgo)) {
      sessionHistory.shift();
    } else {
      break;
    }
  }
}

function addReaction(messageId, reactionType, userIP) {
  const reactionKey = `${messageId}-${userIP}`;
  
  if (reactions.has(reactionKey)) {
    const existingReaction = reactions.get(reactionKey);
    if (existingReaction === reactionType) {
      return null;
    }
    
    removeReactionFromMessage(messageId, existingReaction);
  }
  
  reactions.set(reactionKey, reactionType);
  addReactionToMessage(messageId, reactionType);
  
  return reactionType;
}

function addReactionToMessage(messageId, reactionType) {
  for (const [_, sessionMessages] of messages) {
    for (const msg of sessionMessages) {
      if (msg.id === messageId) {
        if (reactionType === 'like') {
          msg.likes++;
        } else if (reactionType === 'dislike') {
          msg.dislikes++;
        }
        return;
      }
    }
  }
  
  for (const [_, sessionHistory] of historyMessages) {
    for (const msg of sessionHistory) {
      if (msg.id === messageId) {
        if (reactionType === 'like') {
          msg.likes++;
        } else if (reactionType === 'dislike') {
          msg.dislikes++;
        }
        return;
      }
    }
  }
}

function removeReactionFromMessage(messageId, reactionType) {
  for (const [_, sessionMessages] of messages) {
    for (const msg of sessionMessages) {
      if (msg.id === messageId) {
        if (reactionType === 'like') {
          msg.likes = Math.max(0, msg.likes - 1);
        } else if (reactionType === 'dislike') {
          msg.dislikes = Math.max(0, msg.dislikes - 1);
        }
        return;
      }
    }
  }
  
  for (const [_, sessionHistory] of historyMessages) {
    for (const msg of sessionHistory) {
      if (msg.id === messageId) {
        if (reactionType === 'like') {
          msg.likes = Math.max(0, msg.likes - 1);
        } else if (reactionType === 'dislike') {
          msg.dislikes = Math.max(0, msg.dislikes - 1);
        }
        return;
      }
    }
  }
}

function getUserReaction(messageId, userIP) {
  const reactionKey = `${messageId}-${userIP}`;
  return reactions.get(reactionKey) || null;
}

function getTopicTypes() {
  return [...TOPIC_TYPES];
}

function cleanupOldData() {
  const now = dayjs();
  const expiryDate = now.subtract(MESSAGE_EXPIRY_DAYS, 'day');
  const historyExpiryDate = now.subtract(HISTORY_EXPIRY_DAYS, 'day');
  
  for (const [topicId, topic] of topics) {
    if (dayjs(topic.createdAt).isBefore(expiryDate)) {
      topics.delete(topicId);
    }
  }
  
  for (const [sessionId, session] of sessions) {
    if (dayjs(session.createdAt).isBefore(expiryDate)) {
      sessions.delete(sessionId);
      messages.delete(sessionId);
    }
  }
  
  for (const [sessionId, sessionHistory] of historyMessages) {
    const filteredHistory = sessionHistory.filter(msg => 
      dayjs(msg.createdAt).isAfter(historyExpiryDate)
    );
    
    if (filteredHistory.length === 0) {
      historyMessages.delete(sessionId);
    } else {
      historyMessages.set(sessionId, filteredHistory);
    }
  }
  
  console.log(`Cleanup completed at ${now.format()}`);
}

setInterval(cleanupOldData, 60 * 60 * 1000);

function getSession(sessionId) {
  return sessions.get(sessionId) || null;
}

function getTopic(topicId) {
  return topics.get(topicId) || null;
}

module.exports = {
  TOPIC_TYPES,
  MAX_MESSAGES_PER_SESSION,
  MESSAGE_EXPIRY_DAYS,
  HISTORY_EXPIRY_DAYS,
  createTopic,
  getTopics,
  getTopicsByType,
  bubbleTopic,
  createSession,
  getSessionsByTopic,
  getSessionsGroupedByType,
  getSession,
  getTopic,
  addMessage,
  getMessages,
  addReaction,
  getUserReaction,
  getTopicTypes,
  cleanupOldData
};
