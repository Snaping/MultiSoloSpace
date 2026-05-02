require('dotenv').config();
const express = require('express');
const cors = require('cors');
const path = require('path');
const { initDB, getDB } = require('./database/db');

const authRoutes = require('./routes/auth');
const gameRoutes = require('./routes/games');

const app = express();
const PORT = process.env.PORT || 3001;

app.use(cors());
app.use(express.json());

app.use('/api/auth', authRoutes);
app.use('/api/games', gameRoutes);

app.get('/api/health', (req, res) => {
  res.json({ 
    success: true, 
    message: 'AdvSpeed API 服务正常运行',
    timestamp: new Date().toISOString()
  });
});

app.get('/api/test-cards', async (req, res) => {
  try {
    const db = getDB();
    const unusedCards = db.data.cards.filter(c => !c.used);
    res.json({
      success: true,
      data: unusedCards
    });
  } catch (error) {
    res.status(500).json({ success: false, message: '查询失败' });
  }
});

app.use(express.static(path.join(__dirname, '../client/dist')));

app.get('*', (req, res) => {
  res.sendFile(path.join(__dirname, '../client/dist/index.html'));
});

function startServer() {
  initDB();
  
  app.listen(PORT, () => {
    console.log(`AdvSpeed 后端服务运行在端口 ${PORT}`);
    console.log(`API 地址: http://localhost:${PORT}/api`);
  });
}

startServer();
