import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { appVersionMark } from '@aurouscia/vite-app-version'
import viteAppVersionConfig from './appVersionOptions.json'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [vue(), appVersionMark(viteAppVersionConfig)],
  envDir: 'env',
  build:{
    outDir:"../../RailChess/wwwroot",
    emptyOutDir:false,
    rollupOptions: {
      output:{
        manualChunks: (id) => {
          if (id.includes('node_modules'))
            return 'libs'
        }
      },
      onwarn(msg, defaultHandler) {
        if (msg.code !== 'INVALID_ANNOTATION')
          defaultHandler(msg)
      },
    },
  }
})
