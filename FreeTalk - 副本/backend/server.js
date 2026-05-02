const express = require('express');
const http = require('http');
const { Server } = require('socket.io');
const cors = require('cors');
const path = require('path');

const topicsRouter = require('./routes/topics');
const sessionsRouter = require('./routes/sessions');
const messagesRouter = require('./routes/messages');
const { getMessages, addMessage, addReaction, getUserReaction, createTopic, createSession, addMessage: storeAddMessage } = require('./data/store');
const { generateNameFromIP, getClientIP } = require('./utils/nameGenerator');
const { getDailyTheme, getAllThemes, getThemeById } = require('./config/themes');

let currentOverrideTheme = null;

function initializeSampleData() {
  const sampleTopics = [
    { type: '闲聊', title: '今天天气怎么样？' },
    { type: '游戏', title: '最近在玩什么游戏？' },
    { type: '动漫', title: '推荐几部好看的动漫' },
    { type: '科技', title: 'AI技术的未来发展' },
    { type: '美食', title: '周末去吃什么？' },
    { type: '音乐', title: '最近单曲循环的歌' },
    { type: '电影', title: '最近上映的电影推荐' },
    { type: '旅行', title: '下一个想去的城市' }
  ];
  
  const sampleMessages = [
    '今天天气真不错，适合出去走走！',
    '我正在玩《原神》，新版本体验很棒！',
    '《进击的巨人》结局虽然有争议，但整体是神作',
    'AI越来越强大了，以后可能很多工作都会被替代',
    '周末想去吃火锅，有人一起吗？',
    '最近一直在听周杰伦的老歌',
    '《流浪地球2》真的太震撼了！',
    '想去云南大理，听说风景很美'
  ];
  
  const sampleUserNames = [
    '张三', '李四', '王五', '赵六', '钱七',
    '孙八', '周九', '吴十', '郑十一', '王十二'
  ];
  
  console.log('🔄 初始化示例数据...');
  
  sampleTopics.forEach((topicData, index) => {
    const topic = createTopic(
      topicData.type,
      topicData.title,
      `192.168.1.${index + 1}`,
      sampleUserNames[index % sampleUserNames.length]
    );
    
    if (index < 5) {
      const session = createSession(
        topic.id,
        `192.168.1.${index + 100}`,
        sampleUserNames[(index + 3) % sampleUserNames.length]
      );
      
      const numMessages = 3 + Math.floor(Math.random() * 5);
      for (let i = 0; i < numMessages; i++) {
        const msgIndex = (index + i) % sampleMessages.length;
        const userIndex = (index + i + 5) % sampleUserNames.length;
        
        storeAddMessage(
          session.id,
          sampleMessages[msgIndex],
          `192.168.1.${200 + userIndex}`,
          sampleUserNames[userIndex]
        );
      }
      
      console.log(`  ✅ 创建会话: ${topic.title} (${session.messageCount || numMessages}条消息)`);
    }
  });
  
  console.log('✅ 示例数据初始化完成！');
}

const app = express();
const server = http.createServer(app);

const io = new Server(server, {
  cors: {
    origin: '*',
    methods: ['GET', 'POST']
  }
});

app.use(cors());
app.use(express.json());

app.use('/api/topics', topicsRouter);
app.use('/api/sessions', sessionsRouter);
app.use('/api/messages', messagesRouter);

app.get('/api/theme', (req, res) => {
  const theme = currentOverrideTheme || getDailyTheme();
  res.json({
    success: true,
    data: {
      id: theme.id,
      name: theme.name,
      icon: theme.icon,
      description: theme.description,
      style: theme.style,
      isOverride: currentOverrideTheme !== null
    }
  });
});

app.get('/api/themes', (req, res) => {
  const allThemes = getAllThemes();
  const currentTheme = currentOverrideTheme || getDailyTheme();
  
  const themesList = allThemes.map(theme => ({
    id: theme.id,
    name: theme.name,
    icon: theme.icon,
    description: theme.description,
    isActive: theme.id === currentTheme.id,
    isDaily: theme.id === getDailyTheme().id,
    style: {
      primary: theme.style.primary,
      secondary: theme.style.secondary,
      cardBorder: theme.style.cardBorder
    }
  }));
  
  res.json({
    success: true,
    data: themesList
  });
});

app.post('/api/theme/switch/:themeId', (req, res) => {
  const { themeId } = req.params;
  
  if (themeId === 'daily') {
    currentOverrideTheme = null;
    const theme = getDailyTheme();
    res.json({
      success: true,
      data: {
        id: theme.id,
        name: theme.name,
        icon: theme.icon,
        description: theme.description,
        style: theme.style,
        isOverride: false
      }
    });
    return;
  }
  
  const theme = getThemeById(themeId);
  
  if (!theme) {
    return res.status(404).json({
      success: false,
      error: '主题不存在'
    });
  }
  
  currentOverrideTheme = theme;
  
  io.emit('themeChanged', {
    id: theme.id,
    name: theme.name,
    icon: theme.icon,
    style: theme.style
  });
  
  res.json({
    success: true,
    data: {
      id: theme.id,
      name: theme.name,
      icon: theme.icon,
      description: theme.description,
      style: theme.style,
      isOverride: true
    }
  });
});

app.get('/api/user/identity', (req, res) => {
  const clientIP = getClientIP(req);
  const { name, theme } = generateNameFromIP(clientIP);
  
  res.json({
    success: true,
    data: {
      name: name,
      theme: {
        id: theme.id,
        name: theme.name
      }
    }
  });
});

if (process.env.NODE_ENV === 'production') {
  app.use(express.static(path.join(__dirname, '../frontend/dist')));
  
  app.get('*', (req, res) => {
    res.sendFile(path.join(__dirname, '../frontend/dist/index.html'));
  });
}

const activeSessions = new Map();

io.on('connection', (socket) => {
  console.log('New client connected:', socket.id);
  
  const clientIP = socket.handshake.address === '::1' ? '127.0.0.1' : 
                    socket.handshake.address || 'unknown';
  
  socket.on('joinSession', ({ sessionId, isVIP = false }) => {
    socket.join(sessionId);
    
    if (!activeSessions.has(sessionId)) {
      activeSessions.set(sessionId, new Set());
    }
    activeSessions.get(sessionId).add(socket.id);
    
    const messages = getMessages(sessionId, isVIP);
    socket.emit('initialMessages', messages);
    
    console.log(`Client ${socket.id} joined session ${sessionId}`);
  });
  
  socket.on('leaveSession', ({ sessionId }) => {
    socket.leave(sessionId);
    
    if (activeSessions.has(sessionId)) {
      activeSessions.get(sessionId).delete(socket.id);
      if (activeSessions.get(sessionId).size === 0) {
        activeSessions.delete(sessionId);
      }
    }
    
    console.log(`Client ${socket.id} left session ${sessionId}`);
  });
  
  socket.on('sendMessage', ({ sessionId, content }) => {
    if (!sessionId || !content || !content.trim()) {
      return;
    }
    
    if (content.trim().length > 500) {
      socket.emit('error', { message: '内容不能超过500个字符' });
      return;
    }
    
    const { name: senderName } = generateNameFromIP(clientIP);
    const message = addMessage(sessionId, content.trim(), clientIP, senderName);
    
    if (message) {
      io.to(sessionId).emit('newMessage', message);
      io.emit('sessionUpdated', { sessionId });
    }
  });
  
  socket.on('reactToMessage', ({ messageId, sessionId, reactionType }) => {
    if (!reactionType || !['like', 'dislike'].includes(reactionType)) {
      return;
    }
    
    const currentReaction = getUserReaction(messageId, clientIP);
    
    if (currentReaction === reactionType) {
      return;
    }
    
    const result = addReaction(messageId, reactionType, clientIP);
    
    io.to(sessionId).emit('reactionUpdated', {
      messageId: messageId,
      reactionType: result,
      wasChanged: currentReaction !== null
    });
  });
  
  socket.on('disconnect', () => {
    for (const [sessionId, sockets] of activeSessions) {
      if (sockets.has(socket.id)) {
        sockets.delete(socket.id);
        if (sockets.size === 0) {
          activeSessions.delete(sessionId);
        }
      }
    }
    
    console.log('Client disconnected:', socket.id);
  });
});

const PORT = process.env.PORT || 3000;

server.listen(PORT, () => {
  console.log(`\n========================================`);
  console.log(`🚀 FreeTalk Server 启动成功!`);
  console.log(`========================================`);
  console.log(`📡 HTTP 服务: http://localhost:${PORT}`);
  console.log(`🔌 WebSocket: 已就绪`);
  console.log(`========================================\n`);
  
  initializeSampleData();
  
  console.log(`\n✨ 服务已准备就绪，可以开始使用了!`);
  console.log(`💡 提示: 前端访问 http://localhost:5173 即可体验`);
  console.log(`========================================\n`);
});
