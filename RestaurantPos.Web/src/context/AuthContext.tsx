import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import { login as loginRequest } from '../api/auth'
import type { AuthResponse } from '../types/api'
import { isTokenExpired } from './token'
import { useToast } from './ToastContext'
import { useSession } from './SessionContext'

export type AuthState = {
  token: string | null
  userName: string | null
  roles: string[]
}

type AuthContextValue = AuthState & {
  login: (userName: string, password: string) => Promise<AuthResponse>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined)

const tokenKey = 'pos_token'
const userKey = 'pos_user'
const rolesKey = 'pos_roles'

const getStoredRoles = () => {
  const stored = localStorage.getItem(rolesKey)
  if (!stored) return []
  try {
    return JSON.parse(stored) as string[]
  } catch {
    return []
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const { addToast } = useToast()
  const { setSessionExpired } = useSession()
  const [token, setToken] = useState<string | null>(() => localStorage.getItem(tokenKey))
  const [userName, setUserName] = useState<string | null>(() => localStorage.getItem(userKey))
  const [roles, setRoles] = useState<string[]>(() => getStoredRoles())

  useEffect(() => {
    if (token && isTokenExpired(token)) {
      localStorage.removeItem(tokenKey)
      localStorage.removeItem(userKey)
      localStorage.removeItem(rolesKey)
      setToken(null)
      setUserName(null)
      setRoles([])
      addToast('Session expired. Please log in again.')
      setSessionExpired(true)
    }
  }, [token, addToast, setSessionExpired])

  const login = async (name: string, password: string) => {
    const response: AuthResponse = await loginRequest({ userName: name, password })
    localStorage.setItem(tokenKey, response.token)
    localStorage.setItem(userKey, response.userName)
    localStorage.setItem(rolesKey, JSON.stringify(response.roles))
    setToken(response.token)
    setUserName(response.userName)
    setRoles(response.roles)
    return response
  }

  const logout = () => {
    localStorage.removeItem(tokenKey)
    localStorage.removeItem(userKey)
    localStorage.removeItem(rolesKey)
    setToken(null)
    setUserName(null)
    setRoles([])
  }

  const value = useMemo(
    () => ({ token, userName, roles, login, logout }),
    [token, userName, roles]
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider')
  }
  return context
}
