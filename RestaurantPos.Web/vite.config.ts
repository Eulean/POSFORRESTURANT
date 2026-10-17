import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5268',
      '/health': 'http://localhost:5268',
      '/logo': 'http://localhost:5268'
    }
  }
})
