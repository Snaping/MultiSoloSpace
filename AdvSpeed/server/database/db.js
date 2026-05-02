const path = require('path')
const fs = require('fs')

const dbDir = path.join(__dirname, '..', 'data')
if (!fs.existsSync(dbDir)) {
  fs.mkdirSync(dbDir, { recursive: true })
}

const dbPath = path.join(dbDir, 'db.json')

const defaultData = {
  games: [
    { id: 1, name: '英雄联盟', icon: '🎮', category: 'MOBA', description: '全球最受欢迎的MOBA游戏', status: 'online', players: 1250000 },
    { id: 2, name: '王者荣耀', icon: '👑', category: 'MOBA', description: '移动端MOBA游戏王者', status: 'online', players: 980000 },
    { id: 3, name: 'CS:GO', icon: '🔫', category: 'FPS', description: '经典第一人称射击游戏', status: 'online', players: 560000 },
    { id: 4, name: '绝地求生', icon: '🏃', category: 'FPS', description: '大逃杀游戏鼻祖', status: 'online', players: 420000 },
    { id: 5, name: '原神', icon: '🌟', category: 'RPG', description: '开放世界冒险游戏', status: 'online', players: 890000 },
    { id: 6, name: '守望先锋', icon: '🛡️', category: 'FPS', description: '团队竞技射击游戏', status: 'online', players: 320000 },
    { id: 7, name: 'DOTA 2', icon: '⚔️', category: 'MOBA', description: '经典MOBA游戏', status: 'online', players: 450000 },
    { id: 8, name: 'Apex英雄', icon: '🚀', category: 'FPS', description: '英雄大逃杀游戏', status: 'online', players: 380000 },
  ],
  routes: [
    { id: 1, name: '华东线路', location: '上海', ping: 15, status: 'online', game_id: 1 },
    { id: 2, name: '华北线路', location: '北京', ping: 22, status: 'online', game_id: 1 },
    { id: 3, name: '华南线路', location: '广州', ping: 18, status: 'online', game_id: 1 },
    { id: 4, name: '西南线路', location: '成都', ping: 25, status: 'online', game_id: 1 },
    { id: 5, name: '国际线路', location: '香港', ping: 45, status: 'online', game_id: 1 },
    { id: 6, name: '上海线路', location: '上海', ping: 12, status: 'online', game_id: 2 },
    { id: 7, name: '深圳线路', location: '深圳', ping: 14, status: 'online', game_id: 2 },
    { id: 8, name: '北京线路', location: '北京', ping: 18, status: 'online', game_id: 2 },
    { id: 9, name: '上海节点', location: '上海', ping: 16, status: 'online', game_id: 3 },
    { id: 10, name: '广州节点', location: '广州', ping: 20, status: 'online', game_id: 3 },
    { id: 11, name: '香港节点', location: '香港', ping: 35, status: 'online', game_id: 3 },
    { id: 12, name: '亚洲线路', location: '新加坡', ping: 55, status: 'online', game_id: 4 },
    { id: 13, name: '欧洲线路', location: '法兰克福', ping: 180, status: 'online', game_id: 4 },
    { id: 14, name: '美洲线路', location: '洛杉矶', ping: 220, status: 'online', game_id: 4 },
    { id: 15, name: '国内线路', location: '上海', ping: 10, status: 'online', game_id: 5 },
    { id: 16, name: '亚服线路', location: '东京', ping: 60, status: 'online', game_id: 5 },
    { id: 17, name: '欧服线路', location: '阿姆斯特丹', ping: 200, status: 'online', game_id: 5 },
    { id: 18, name: '亚洲节点', location: '首尔', ping: 45, status: 'online', game_id: 6 },
    { id: 19, name: '美洲节点', location: '旧金山', ping: 195, status: 'online', game_id: 6 },
    { id: 20, name: '完美世界', location: '上海', ping: 15, status: 'online', game_id: 7 },
    { id: 21, name: '东南亚服', location: '新加坡', ping: 65, status: 'online', game_id: 7 },
    { id: 22, name: 'EA亚洲', location: '东京', ping: 50, status: 'online', game_id: 8 },
    { id: 23, name: 'EA美洲', location: '达拉斯', ping: 185, status: 'online', game_id: 8 },
  ],
  cards: [
    { id: 1, code: 'TEST-DAY-001', duration: 1, used: false, used_at: null, created_at: new Date().toISOString() },
    { id: 2, code: 'TEST-DAY-002', duration: 1, used: false, used_at: null, created_at: new Date().toISOString() },
    { id: 3, code: 'TEST-WEEK-001', duration: 7, used: false, used_at: null, created_at: new Date().toISOString() },
    { id: 4, code: 'TEST-MONTH-001', duration: 30, used: false, used_at: null, created_at: new Date().toISOString() },
    { id: 5, code: 'ADVSPEED-2024-001', duration: 30, used: false, used_at: null, created_at: new Date().toISOString() },
    { id: 6, code: 'ADVSPEED-2024-002', duration: 30, used: false, used_at: null, created_at: new Date().toISOString() },
  ],
  users: [],
  nextIds: {
    users: 1,
    cards: 7
  }
}

let dbData = null

function readDB() {
  try {
    if (fs.existsSync(dbPath)) {
      const content = fs.readFileSync(dbPath, 'utf-8')
      dbData = JSON.parse(content)
    } else {
      dbData = JSON.parse(JSON.stringify(defaultData))
      saveDB()
      console.log('数据库已初始化，默认数据已插入')
    }
    
    if (!dbData.games || dbData.games.length === 0) {
      dbData.games = JSON.parse(JSON.stringify(defaultData.games))
    }
    if (!dbData.routes || dbData.routes.length === 0) {
      dbData.routes = JSON.parse(JSON.stringify(defaultData.routes))
    }
    if (!dbData.cards || dbData.cards.length === 0) {
      dbData.cards = JSON.parse(JSON.stringify(defaultData.cards))
    }
    if (!dbData.users) dbData.users = []
    if (!dbData.nextIds) dbData.nextIds = { users: 1, cards: 7 }
    
    return dbData
  } catch (error) {
    console.error('读取数据库错误:', error)
    dbData = JSON.parse(JSON.stringify(defaultData))
    return dbData
  }
}

function saveDB() {
  try {
    fs.writeFileSync(dbPath, JSON.stringify(dbData, null, 2), 'utf-8')
    return true
  } catch (error) {
    console.error('保存数据库错误:', error)
    return false
  }
}

function getDB() {
  if (!dbData) {
    readDB()
  }
  return {
    data: dbData,
    save: saveDB
  }
}

function initDB() {
  readDB()
  console.log('已连接到 JSON 数据库')
}

module.exports = {
  getDB,
  initDB,
  readDB,
  saveDB
}
