const themes = [
  {
    id: 'historical',
    name: '历史人物',
    icon: '�️',
    description: '穿越时空，与历史名人对话',
    names: [
      '秦始皇', '汉武帝', '唐太宗', '宋太祖', '成吉思汗',
      '诸葛亮', '曹操', '刘备', '孙权', '关羽',
      '李白', '杜甫', '白居易', '苏轼', '辛弃疾',
      '武则天', '杨贵妃', '西施', '王昭君', '貂蝉',
      '韩信', '萧何', '张良', '刘伯温', '王阳明',
      '康熙', '乾隆', '雍正', '李世民', '刘邦',
      '项羽', '虞姬', '卓文君', '司马相如', '蔡文姬',
      '陶渊明', '谢灵运', '王维', '孟浩然', '王昌龄',
      '王安石', '欧阳修', '曾巩', '苏洵', '苏辙',
      '玄奘', '郑和', '戚继光', '郑成功', '林则徐'
    ],
    style: {
      primary: '#8B4513',
      secondary: '#D2691E',
      background: 'linear-gradient(135deg, #F5F5DC 0%, #DEB887 50%, #D2691E 100%)',
      backgroundColor: '#F5F5DC',
      accent: '#8B4513',
      accentLight: '#DEB887',
      success: '#228B22',
      error: '#B22222',
      warning: '#DAA520',
      info: '#4682B4',
      font: '"Georgia", "Times New Roman", serif',
      cardBackground: 'linear-gradient(145deg, #FFFFFF 0%, #FFF8DC 100%)',
      cardShadow: '0 8px 32px rgba(139, 69, 19, 0.3), inset 0 1px 0 rgba(255, 255, 255, 0.8)',
      cardBorder: '2px solid #D2691E',
      buttonGradient: 'linear-gradient(135deg, #8B4513 0%, #A0522D 50%, #CD853F 100%)',
      buttonShadow: '0 4px 15px rgba(139, 69, 19, 0.4)',
      inputBorder: '2px solid #D2691E',
      inputFocusBorder: '3px solid #8B4513',
      messageUserBg: 'linear-gradient(135deg, #8B4513 0%, #A0522D 100%)',
      messageOtherBg: 'linear-gradient(135deg, #FFF8DC 0%, #FAEBD7 100%)',
      headerBg: 'linear-gradient(180deg, #8B4513 0%, #A0522D 100%)',
      headerText: '#FFFFFF',
      borderRadius: '12px',
      transition: 'all 0.4s cubic-bezier(0.4, 0, 0.2, 1)',
      animationDuration: '0.6s',
      hoverScale: '1.02'
    }
  },
  {
    id: 'technology',
    name: '科技名人',
    icon: '🚀',
    description: '探索科技前沿，与创新者同行',
    names: [
      '爱因斯坦', '牛顿', '伽利略', '爱迪生', '特斯拉',
      '居里夫人', '达尔文', '霍金', '图灵', '冯诺依曼',
      '比尔盖茨', '乔布斯', '马斯克', '扎克伯格', '贝佐斯',
      '杨振宁', '李政道', '钱学森', '邓稼先', '华罗庚',
      '袁隆平', '屠呦呦', '钟南山', '林巧稚', '吴孟超',
      '玻尔', '薛定谔', '海森堡', '狄拉克',
      '香农', '维纳', '巴贝奇',
      '莱特兄弟', '贝尔', '富兰克林', '法拉第',
      '麦克斯韦', '赫兹', '欧姆', '安培', '伏特',
      '沃森', '克里克', '孟德尔', '摩尔根'
    ],
    style: {
      primary: '#0066FF',
      secondary: '#00D4FF',
      background: 'linear-gradient(135deg, #0F172A 0%, #1E3A8A 50%, #0066FF 100%)',
      backgroundColor: '#0F172A',
      accent: '#00D4FF',
      accentLight: '#7DD3FC',
      success: '#22C55E',
      error: '#EF4444',
      warning: '#F59E0B',
      info: '#3B82F6',
      font: '"SF Pro Display", "Segoe UI", "Roboto", sans-serif',
      cardBackground: 'linear-gradient(145deg, rgba(30, 58, 138, 0.9) 0%, rgba(15, 23, 42, 0.95) 100%)',
      cardShadow: '0 8px 32px rgba(0, 102, 255, 0.4), 0 0 60px rgba(0, 212, 255, 0.1), inset 0 1px 0 rgba(255, 255, 255, 0.1)',
      cardBorder: '1px solid rgba(0, 212, 255, 0.3)',
      buttonGradient: 'linear-gradient(135deg, #0066FF 0%, #00D4FF 50%, #0066FF 100%)',
      buttonShadow: '0 4px 20px rgba(0, 102, 255, 0.5), 0 0 30px rgba(0, 212, 255, 0.3)',
      inputBorder: '2px solid rgba(0, 212, 255, 0.5)',
      inputFocusBorder: '2px solid #00D4FF',
      messageUserBg: 'linear-gradient(135deg, #0066FF 0%, #00D4FF 100%)',
      messageOtherBg: 'linear-gradient(145deg, rgba(30, 58, 138, 0.9) 0%, rgba(15, 23, 42, 0.95) 100%)',
      headerBg: 'linear-gradient(180deg, rgba(0, 102, 255, 0.95) 0%, rgba(0, 212, 255, 0.8) 100%)',
      headerText: '#FFFFFF',
      borderRadius: '16px',
      transition: 'all 0.3s cubic-bezier(0.4, 0, 0.2, 1)',
      animationDuration: '0.5s',
      hoverScale: '1.03'
    }
  },
  {
    id: 'mythology',
    name: '神话人物',
    icon: '⚡',
    description: '踏入神话世界，与诸神共舞',
    names: [
      '宙斯', '赫拉', '波塞冬', '哈迪斯', '雅典娜',
      '阿波罗', '阿耳忒弥斯', '阿瑞斯', '阿佛洛狄忒', '赫菲斯托斯',
      '奥丁', '托尔', '洛基', '弗丽嘉', '索尔',
      '女娲', '伏羲', '神农', '黄帝', '炎帝',
      '玉帝', '王母娘娘', '太上老君', '二郎神', '哪吒',
      '孙悟空', '猪八戒', '沙僧', '唐僧', '白龙马',
      '盘古', '共工', '祝融', '刑天', '夸父',
      '后羿', '嫦娥', '吴刚', '玉兔', '月老',
      '八仙', '铁拐李', '汉钟离', '张果老', '吕洞宾',
      '何仙姑', '蓝采和', '韩湘子', '曹国舅', '妈祖'
    ],
    style: {
      primary: '#10B981',
      secondary: '#34D399',
      background: 'linear-gradient(135deg, #ECFDF5 0%, #D1FAE5 50%, #6EE7B7 100%)',
      backgroundColor: '#ECFDF5',
      accent: '#065F46',
      accentLight: '#A7F3D0',
      success: '#22C55E',
      error: '#EF4444',
      warning: '#F59E0B',
      info: '#06B6D4',
      font: '"KaiTi", "STKaiti", "SimSun", serif',
      cardBackground: 'linear-gradient(145deg, #FFFFFF 0%, #ECFDF5 50%, #D1FAE5 100%)',
      cardShadow: '0 8px 32px rgba(16, 185, 129, 0.3), 0 0 40px rgba(52, 211, 153, 0.2)',
      cardBorder: '2px solid #34D399',
      buttonGradient: 'linear-gradient(135deg, #065F46 0%, #10B981 50%, #34D399 100%)',
      buttonShadow: '0 4px 20px rgba(16, 185, 129, 0.4)',
      inputBorder: '2px solid #34D399',
      inputFocusBorder: '3px solid #065F46',
      messageUserBg: 'linear-gradient(135deg, #065F46 0%, #10B981 100%)',
      messageOtherBg: 'linear-gradient(145deg, #FFFFFF 0%, #ECFDF5 100%)',
      headerBg: 'linear-gradient(180deg, #065F46 0%, #10B981 100%)',
      headerText: '#FFFFFF',
      borderRadius: '14px',
      transition: 'all 0.4s ease',
      animationDuration: '0.5s',
      hoverScale: '1.03'
    }
  },
  {
    id: 'literature',
    name: '文学角色',
    icon: '📚',
    description: '沉浸文学海洋，与经典角色对话',
    names: [
      '贾宝玉', '林黛玉', '薛宝钗', '王熙凤', '贾母',
      '宋江', '卢俊义', '吴用', '林冲', '鲁智深',
      '刘备', '关羽', '张飞', '诸葛亮', '曹操',
      '史湘云', '妙玉', '贾元春', '贾迎春', '贾探春',
      '晁盖', '公孙胜', '关胜', '秦明', '呼延灼',
      '白骨精', '红孩儿', '铁扇公主', '玉皇大帝', '如来佛祖',
      '孙权', '周瑜', '鲁肃', '吕蒙', '陆逊',
      '堂吉诃德', '桑丘', '哈姆雷特', '罗密欧', '朱丽叶',
      '福尔摩斯', '华生', '波洛', '马普尔小姐', '邦德',
      '鲁迅', '老舍', '巴金', '茅盾', '郭沫若'
    ],
    style: {
      primary: '#92400E',
      secondary: '#B45309',
      background: 'linear-gradient(135deg, #FFFBEB 0%, #FEF3C7 50%, #FDE68A 100%)',
      backgroundColor: '#FFFBEB',
      accent: '#78350F',
      accentLight: '#FDE68A',
      success: '#16A34A',
      error: '#DC2626',
      warning: '#D97706',
      info: '#0284C7',
      font: '"SimSun", "NSimSun", "FangSong", serif',
      cardBackground: 'linear-gradient(145deg, #FFFFFF 0%, #FFFBEB 50%, #FEF3C7 100%)',
      cardShadow: '0 8px 32px rgba(146, 64, 14, 0.3), inset 0 1px 0 rgba(255, 255, 255, 0.9)',
      cardBorder: '2px solid #B45309',
      buttonGradient: 'linear-gradient(135deg, #78350F 0%, #92400E 50%, #B45309 100%)',
      buttonShadow: '0 4px 20px rgba(146, 64, 14, 0.4)',
      inputBorder: '2px solid #B45309',
      inputFocusBorder: '3px solid #78350F',
      messageUserBg: 'linear-gradient(135deg, #78350F 0%, #92400E 100%)',
      messageOtherBg: 'linear-gradient(145deg, #FFFFFF 0%, #FFFBEB 100%)',
      headerBg: 'linear-gradient(180deg, #78350F 0%, #92400E 100%)',
      headerText: '#FFFFFF',
      borderRadius: '10px',
      transition: 'all 0.45s ease',
      animationDuration: '0.55s',
      hoverScale: '1.02'
    }
  },
  {
    id: 'sports',
    name: '体育明星',
    icon: '⚽',
    description: '运动激情，与体育巨星同台竞技',
    names: [
      '乔丹', '科比', '詹姆斯', '库里', '杜兰特',
      '梅西', 'C罗', '内马尔', '姆巴佩', '哈兰德',
      '姚明', '刘翔', '孙杨', '林丹', '李宗伟',
      '费德勒', '纳达尔', '德约科维奇', '穆雷', '瓦林卡',
      '舒马赫', '汉密尔顿', '塞纳', '维特尔', '巴顿',
      '泰森', '阿里', '帕奎奥', '梅威瑟', '霍利菲尔德',
      '菲尔普斯', '波波夫', '索普', '霍根班德', '罗切特',
      '陶菲克', '盖德', '谌龙', '安赛龙',
      '王治郅', '易建联', '巴特尔', '林书豪', '周琦',
      '苏炳添', '谢震业', '史冬鹏', '张国伟'
    ],
    style: {
      primary: '#DC2626',
      secondary: '#EF4444',
      background: 'linear-gradient(135deg, #FEF2F2 0%, #FEE2E2 50%, #FECACA 100%)',
      backgroundColor: '#FEF2F2',
      accent: '#991B1B',
      accentLight: '#FECACA',
      success: '#16A34A',
      error: '#DC2626',
      warning: '#D97706',
      info: '#0284C7',
      font: '"Arial", "Helvetica Neue", "Impact", sans-serif',
      cardBackground: 'linear-gradient(145deg, #FFFFFF 0%, #FEF2F2 50%, #FEE2E2 100%)',
      cardShadow: '0 8px 32px rgba(220, 38, 38, 0.3), 0 0 40px rgba(239, 68, 68, 0.2)',
      cardBorder: '2px solid #EF4444',
      buttonGradient: 'linear-gradient(135deg, #991B1B 0%, #DC2626 50%, #EF4444 100%)',
      buttonShadow: '0 4px 20px rgba(220, 38, 38, 0.4)',
      inputBorder: '2px solid #EF4444',
      inputFocusBorder: '3px solid #991B1B',
      messageUserBg: 'linear-gradient(135deg, #991B1B 0%, #DC2626 100%)',
      messageOtherBg: 'linear-gradient(145deg, #FFFFFF 0%, #FEF2F2 100%)',
      headerBg: 'linear-gradient(180deg, #991B1B 0%, #DC2626 100%)',
      headerText: '#FFFFFF',
      borderRadius: '12px',
      transition: 'all 0.3s ease',
      animationDuration: '0.35s',
      hoverScale: '1.04'
    }
  },
  {
    id: 'food',
    name: '中华美食',
    icon: '�',
    description: '舌尖上的享受，与美食相伴',
    names: [
      '小笼包', '饺子', '馄饨', '包子', '馒头',
      '红烧肉', '糖醋排骨', '宫保鸡丁', '鱼香肉丝', '麻婆豆腐',
      '火锅', '烧烤', '麻辣烫', '串串香', '冒菜',
      '奶茶', '咖啡', '果汁', '可乐', '雪碧',
      '披萨', '汉堡', '薯条', '炸鸡', '热狗',
      '牛排', '意面', '沙拉', '三明治',
      '蛋糕', '面包', '饼干', '巧克力', '冰淇淋',
      '粽子', '月饼', '汤圆', '年糕', '腊八粥',
      '烤鸭', '烧鸡', '烧鹅', '卤味', '酱板鸭',
      '北京烤鸭', '佛跳墙', '东坡肉', '狮子头', '四喜丸子'
    ],
    style: {
      primary: '#EA580C',
      secondary: '#F97316',
      background: 'linear-gradient(135deg, #FFF7ED 0%, #FFEDD5 50%, #FED7AA 100%)',
      backgroundColor: '#FFF7ED',
      accent: '#9A3412',
      accentLight: '#FDBA74',
      success: '#16A34A',
      error: '#DC2626',
      warning: '#D97706',
      info: '#0284C7',
      font: '"Comic Sans MS", "Marker Felt", "Chalkboard", cursive',
      cardBackground: 'linear-gradient(145deg, #FFFFFF 0%, #FFF7ED 50%, #FFEDD5 100%)',
      cardShadow: '0 8px 32px rgba(234, 88, 12, 0.3), 0 0 50px rgba(249, 115, 22, 0.2)',
      cardBorder: '2px solid #F97316',
      buttonGradient: 'linear-gradient(135deg, #9A3412 0%, #EA580C 50%, #F97316 100%)',
      buttonShadow: '0 4px 20px rgba(234, 88, 12, 0.4)',
      inputBorder: '2px solid #F97316',
      inputFocusBorder: '3px solid #9A3412',
      messageUserBg: 'linear-gradient(135deg, #9A3412 0%, #EA580C 100%)',
      messageOtherBg: 'linear-gradient(145deg, #FFFFFF 0%, #FFF7ED 100%)',
      headerBg: 'linear-gradient(180deg, #9A3412 0%, #EA580C 100%)',
      headerText: '#FFFFFF',
      borderRadius: '18px',
      transition: 'all 0.35s ease',
      animationDuration: '0.4s',
      hoverScale: '1.05'
    }
  }
];

function getDailyTheme(date = new Date()) {
  const dayOfYear = Math.floor((date - new Date(date.getFullYear(), 0, 0)) / (1000 * 60 * 60 * 24));
  const themeIndex = dayOfYear % themes.length;
  return themes[themeIndex];
}

function getThemeById(themeId) {
  if (themeId === 'daily') {
    return null;
  }
  return themes.find(t => t.id === themeId);
}

function getAllThemes() {
  return themes;
}

module.exports = {
  themes,
  getDailyTheme,
  getThemeById,
  getAllThemes
};
