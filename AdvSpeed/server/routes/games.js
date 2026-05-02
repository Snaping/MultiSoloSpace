const express = require('express');
const router = express.Router();
const { getDB } = require('../database/db');

router.get('/', async (req, res) => {
  try {
    const { category, search } = req.query;
    const db = getDB();
    
    let result = [...db.data.games];

    if (category) {
      result = result.filter(g => g.category === category);
    }

    if (search) {
      const keyword = search.toLowerCase();
      result = result.filter(g => 
        g.name.toLowerCase().includes(keyword) ||
        (g.description && g.description.toLowerCase().includes(keyword))
      );
    }

    result.sort((a, b) => b.players - a.players);

    res.json({
      success: true,
      data: result
    });
  } catch (error) {
    console.error('获取游戏列表错误:', error);
    res.status(500).json({ 
      success: false, 
      message: '获取游戏列表失败' 
    });
  }
});

router.get('/categories', async (req, res) => {
  try {
    const db = getDB();
    const categories = [...new Set(db.data.games.map(g => g.category))];
    
    res.json({
      success: true,
      data: categories
    });
  } catch (error) {
    console.error('获取分类错误:', error);
    res.status(500).json({ 
      success: false, 
      message: '获取分类失败' 
    });
  }
});

router.get('/:id', async (req, res) => {
  try {
    const { id } = req.params;
    const db = getDB();

    const game = db.data.games.find(g => g.id === parseInt(id));

    if (!game) {
      return res.status(404).json({ 
        success: false, 
        message: '游戏不存在' 
      });
    }

    res.json({
      success: true,
      data: game
    });
  } catch (error) {
    console.error('获取游戏详情错误:', error);
    res.status(500).json({ 
      success: false, 
      message: '获取游戏详情失败' 
    });
  }
});

router.get('/:id/routes', async (req, res) => {
  try {
    const { id } = req.params;
    const db = getDB();

    const routes = db.data.routes
      .filter(r => r.game_id === parseInt(id) && r.status === 'online')
      .sort((a, b) => a.ping - b.ping);

    res.json({
      success: true,
      data: routes
    });
  } catch (error) {
    console.error('获取线路错误:', error);
    res.status(500).json({ 
      success: false, 
      message: '获取线路失败' 
    });
  }
});

module.exports = router;
