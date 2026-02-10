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
          if (id.includes('lodash-es'))
            return 'lodash'
          if (id.includes('/node_modules/vue/') || id.includes('/node_modules/@vue/'))
            return 'vue'
          if (id.includes('signalr'))
            return 'signalr'
          if (id.includes('axios'))
            return 'axios'
          if (id.includes('chart.js'))
            return 'chart'
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
