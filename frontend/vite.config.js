import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    host: true, // cho phép truy cập từ bên ngoài
    // Keep a fixed port so tunnels don't accidentally point at the wrong instance.
    // If the port is busy, fail fast instead of silently switching ports.
    port: 5173,
    strictPort: true,
    // Vite expects `string[] | true`.
    // A leading dot allows the domain + all subdomains (e.g. *.loca.lt).
    allowedHosts: ['.loca.lt'],

    // Proxy API to the local backend so the browser always calls same-origin
    // (works even when the frontend is accessed via localtunnel).
    proxy: {
      '/api': {
        target: 'http://127.0.0.1:5166',
        changeOrigin: true,
      },
    },
  },
})
