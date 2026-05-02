const express = require('express');
const router = express.Router();
const { getDB } = require('../database/db');
const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');

const JWT_SECRET = process.env.JWT_SECRET || 'advspeed_jwt_secret_key_2024';

router.post('/register', async (req, res) => {
  try {
    const { username, password } = req.body;
    const db = await getDB();

    if (!username || !password) {
      return res.status(400).json({ 
        success: false, 
        message: '用户名和密码不能为空' 
      });
    }

    const existingUser = db.data.users.find(u => u.username === username);
    if (existingUser) {
      return res.status(400).json({ 
        success: false, 
        message: '用户名已存在' 
      });
    }

    const hashedPassword = bcrypt.hashSync(password, 10);
    const newUser = {
      id: db.data.nextIds.users++,
      username,
      password: hashedPassword,
      card_code: null,
      expire_at: null,
      created_at: new Date().toISOString()
    };
    
    db.data.users.push(newUser);
    await db.write();

    const token = jwt.sign(
      { id: newUser.id, username: newUser.username },
      JWT_SECRET,
      { expiresIn: '7d' }
    );

    res.json({
      success: true,
      message: '注册成功',
      data: {
        token,
        user: {
          id: newUser.id,
          username: newUser.username,
          is_vip: false,
          expire_at: null
        }
      }
    });
  } catch (error) {
    console.error('注册错误:', error);
    res.status(500).json({ success: false, message: '服务器错误' });
  }
});

router.post('/login', async (req, res) => {
  try {
    const { username, password } = req.body;
    const db = await getDB();

    if (!username || !password) {
      return res.status(400).json({ 
        success: false, 
        message: '用户名和密码不能为空' 
      });
    }

    const user = db.data.users.find(u => u.username === username);

    if (!user) {
      return res.status(401).json({ 
        success: false, 
        message: '用户名或密码错误' 
      });
    }

    const isPasswordValid = bcrypt.compareSync(password, user.password);
    if (!isPasswordValid) {
      return res.status(401).json({ 
        success: false, 
        message: '用户名或密码错误' 
      });
    }

    const isVip = user.expire_at && new Date(user.expire_at) > new Date();
    
    const token = jwt.sign(
      { id: user.id, username: user.username },
      JWT_SECRET,
      { expiresIn: '7d' }
    );

    res.json({
      success: true,
      message: '登录成功',
      data: {
        token,
        user: {
          id: user.id,
          username: user.username,
          is_vip: isVip,
          expire_at: user.expire_at
        }
      }
    });
  } catch (error) {
    console.error('登录错误:', error);
    res.status(500).json({ success: false, message: '服务器错误' });
  }
});

router.post('/activate', async (req, res) => {
  try {
    const { code } = req.body;
    const authHeader = req.headers.authorization;
    const db = await getDB();
    
    if (!authHeader) {
      return res.status(401).json({ 
        success: false, 
        message: '请先登录' 
      });
    }

    const token = authHeader.replace('Bearer ', '');
    
    jwt.verify(token, JWT_SECRET, async (err, decoded) => {
      if (err) {
        return res.status(401).json({ 
          success: false, 
          message: 'Token无效或已过期' 
        });
      }

      if (!code) {
        return res.status(400).json({ 
          success: false, 
          message: '请输入卡密' 
        });
      }

      const card = db.data.cards.find(c => c.code.toUpperCase() === code.toUpperCase());

      if (!card) {
        return res.status(400).json({ 
          success: false, 
          message: '无效的卡密' 
        });
      }

      if (card.used) {
        return res.status(400).json({ 
          success: false, 
          message: '该卡密已被使用' 
        });
      }

      const userIndex = db.data.users.findIndex(u => u.id === decoded.id);
      if (userIndex === -1) {
        return res.status(404).json({ 
          success: false, 
          message: '用户不存在' 
        });
      }

      const user = db.data.users[userIndex];
      const now = new Date();
      let currentExpire = user.expire_at ? new Date(user.expire_at) : now;
      if (currentExpire < now) currentExpire = now;
      
      const newExpire = new Date(currentExpire.getTime() + card.duration * 24 * 60 * 60 * 1000);

      card.used = true;
      card.used_at = now.toISOString();
      user.card_code = card.code;
      user.expire_at = newExpire.toISOString();

      await db.write();

      res.json({
        success: true,
        message: `激活成功！已添加 ${card.duration} 天VIP时长`,
        data: {
          duration: card.duration,
          expire_at: newExpire.toISOString()
        }
      });
    });
  } catch (error) {
    console.error('激活错误:', error);
    res.status(500).json({ success: false, message: '服务器错误' });
  }
});

router.get('/verify', async (req, res) => {
  try {
    const authHeader = req.headers.authorization;
    const db = await getDB();
    
    if (!authHeader) {
      return res.status(401).json({ 
        success: false, 
        message: '请先登录' 
      });
    }

    const token = authHeader.replace('Bearer ', '');
    
    jwt.verify(token, JWT_SECRET, (err, decoded) => {
      if (err) {
        return res.status(401).json({ 
          success: false, 
          message: 'Token无效或已过期' 
        });
      }

      const user = db.data.users.find(u => u.id === decoded.id);
      if (!user) {
        return res.status(404).json({ 
          success: false, 
          message: '用户不存在' 
        });
      }

      const isVip = user.expire_at && new Date(user.expire_at) > new Date();

      res.json({
        success: true,
        data: {
          user: {
            id: user.id,
            username: user.username,
            is_vip: isVip,
            expire_at: user.expire_at
          }
        }
      });
    });
  } catch (error) {
    console.error('验证错误:', error);
    res.status(500).json({ success: false, message: '服务器错误' });
  }
});

module.exports = router;
