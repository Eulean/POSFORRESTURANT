import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import App from './App'
import './index.css'
import { AuthProvider } from './context/AuthContext'
import { ToastProvider } from './context/ToastContext'
import Toasts from './components/Toasts'
import { SessionProvider } from './context/SessionContext'
import SessionExpiredModal from './components/SessionExpiredModal'
import { LoadingProvider } from './context/LoadingContext'

const storedTheme = localStorage.getItem('pos_theme')
if (storedTheme === 'dark') {
  document.documentElement.classList.add('dark')
} else {
  document.documentElement.classList.remove('dark')
}

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ToastProvider>
      <SessionProvider>
        <AuthProvider>
          <LoadingProvider>
            <BrowserRouter>
              <App />
              <Toasts />
              <SessionExpiredModal />
            </BrowserRouter>
          </LoadingProvider>
        </AuthProvider>
      </SessionProvider>
    </ToastProvider>
  </React.StrictMode>
)
