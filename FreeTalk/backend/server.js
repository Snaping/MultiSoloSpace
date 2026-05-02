const express = require('express');
const http = require('http');
const { Server } = require('socket.io');
const cors = require('cors');
const path = require('path');

const topicsRouter = require('./routes/topics');
const sessionsRouter = require('./routes/sessions');
const messagesRouter = require('./routes/messages');
const { getMessages, addMessage, addReaction, getUserReaction } = require('./data/store');
const { generateNameFromIP, getClientIP } = require('./utils/nameGenerator');
const { getDailyTheme } = require('./config/themes');

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
  const theme = getDailyTheme();
  res.json({
    success: true,
    data: {
      id: theme.id,
      name: theme.name,
      style: theme.style
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
  console.log(`FreeTalk Server running on port ${PORT}`);
  console.log(`WebSocket server ready`);
});
