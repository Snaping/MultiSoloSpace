const crypto = require('crypto');
const { getDailyTheme } = require('../config/themes');

const ipNameMap = new Map();
const lastCleanup = new Map();

function generateNameFromIP(ip) {
  const today = new Date().toDateString();
  const theme = getDailyTheme();
  
  const mapKey = `${ip}-${today}`;
  
  if (ipNameMap.has(mapKey)) {
    return {
      name: ipNameMap.get(mapKey),
      theme: theme
    };
  }
  
  const hash = crypto.createHash('md5');
  hash.update(ip + today + theme.id);
  const hashHex = hash.digest('hex');
  
  const hashNum = parseInt(hashHex.substr(0, 8), 16);
  const nameIndex = hashNum % theme.names.length;
  
  const name = theme.names[nameIndex];
  
  ipNameMap.set(mapKey, name);
  
  cleanupOldEntries();
  
  return {
    name: name,
    theme: theme
  };
}

function cleanupOldEntries() {
  const today = new Date().toDateString();
  
  for (const [key, _] of ipNameMap) {
    const [_, date] = key.split('-');
    if (date !== today) {
      ipNameMap.delete(key);
    }
  }
}

function getClientIP(req) {
  const forwardedFor = req.headers['x-forwarded-for'];
  if (forwardedFor) {
    const ips = forwardedFor.split(',');
    return ips[0].trim();
  }
  
  const realIP = req.headers['x-real-ip'];
  if (realIP) {
    return realIP.trim();
  }
  
  const remoteAddress = req.connection?.remoteAddress || 
                        req.socket?.remoteAddress || 
                        req.ip;
  
  if (remoteAddress === '::1' || remoteAddress === '::ffff:127.0.0.1') {
    return '127.0.0.1';
  }
  
  return remoteAddress || 'unknown';
}

module.exports = {
  generateNameFromIP,
  getClientIP,
  ipNameMap
};
