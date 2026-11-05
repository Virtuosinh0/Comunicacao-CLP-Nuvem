import { watch } from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// O projeto fica dentro de uma pasta chamada ".git" (C:\Automatec\Software\Extra\.git\...).
// O Vite trata "**/.git/**" como pasta interna do git, o que causa dois problemas:
//   1. fs.deny padrão bloqueia TODOS os arquivos (403 Restricted)  → fsDeny abaixo
//   2. o vigia de arquivos (fixo no Vite, não configurável) ignora todas as mudanças:
//      o Vite continua servindo a versão antiga do cache, nem F5 resolve → plugin watchInsideDotGit
// Se o repositório for movido para fora da pasta ".git", os dois contornos deixam de ser necessários.
const root = fileURLToPath(new URL('.', import.meta.url)).replace(/\\/g, '/').replace(/\/$/, '')
const fsDeny = ['.env', '.env.*', '*.{crt,pem}', `${root}/**/.git/**`]

/** Vigia o FrontEnd com fs.watch e repassa as mudanças ao Vite (invalida o cache + recarrega a tela). */
function watchInsideDotGit() {
  return {
    name: 'watch-inside-dot-git',
    apply: 'serve',
    configureServer(server) {
      const ignored = /(^|[\\/])(node_modules|dist|\.vite)([\\/]|$)/
      const pending = new Map()

      const watcher = watch(root, { recursive: true }, (_event, file) => {
        if (!file || ignored.test(file)) return
        const fullPath = path.join(root, file).replace(/\\/g, '/')
        // fs.watch dispara várias vezes por gravação: agrupa em 50 ms.
        clearTimeout(pending.get(fullPath))
        pending.set(
          fullPath,
          setTimeout(() => {
            pending.delete(fullPath)
            server.watcher.emit('change', fullPath)
          }, 50),
        )
      })

      server.httpServer?.once('close', () => watcher.close())
    },
  }
}

// Em desenvolvimento o front chama "/api/..." no próprio Vite, que repassa para a API Spring Boot.
// Assim não precisa de CORS na API. Para apontar para outra API: VITE_API_PROXY=http://host:porta npm run dev
const apiTarget = process.env.VITE_API_PROXY ?? 'http://localhost:8080'
const proxy = { '/api': { target: apiTarget, changeOrigin: true } }

export default defineConfig({
  plugins: [react(), watchInsideDotGit()],
  server: {
    port: 5173,
    proxy,
    fs: { deny: fsDeny },
  },
  preview: {
    proxy,
  },
})
