# AbilityKit Admin Console

独立的 AbilityKit Orleans Gateway 后台前端工程，使用 Vite + Vue 3 + TypeScript。

## 开发

```bash
npm install
npm run dev
```

开发服务器默认使用 Vite proxy 将 `/api` 与 `/debug` 转发到 `http://localhost:5000`。可通过环境变量 `ABILITYKIT_GATEWAY_URL` 指向实际 Gateway。

## 构建

```bash
npm run build
```

构建产物会输出到 `../Orleans/src/AbilityKit.Orleans.Gateway/wwwroot/admin`，因此 Gateway 仍可通过 `/admin` 托管正式后台，同时前端源码与后端 API 工程保持独立。
