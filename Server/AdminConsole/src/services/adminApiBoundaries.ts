export const adminApiBoundaries = {
  admin: {
    prefix: '/api/admin',
    responsibility: '后台专用聚合、诊断、运维、审计与房间管理门面，后台页面默认只依赖这里。'
  },
  rooms: {
    prefix: '/api/rooms',
    responsibility: '真实房间/玩家业务接口，面向客户端和自动化流程；后台不直接调用，避免把运营语义混入玩家协议。'
  },
  sandbox: {
    prefix: '/api/shooter-sandbox',
    responsibility: '演示用 Shooter 沙盒自动化入口。'
  },
  debug: {
    prefix: '/debug',
    responsibility: '开发者调试控制台，不承载正式后台模块。'
  }
} as const;
