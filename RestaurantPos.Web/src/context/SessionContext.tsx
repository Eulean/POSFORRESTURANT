import { createContext, useContext, useMemo, useState } from 'react'

type SessionContextValue = {
  sessionExpired: boolean
  setSessionExpired: (value: boolean) => void
}

const SessionContext = createContext<SessionContextValue | undefined>(undefined)

export function SessionProvider({ children }: { children: React.ReactNode }) {
  const [sessionExpired, setSessionExpired] = useState(false)
  const value = useMemo(() => ({ sessionExpired, setSessionExpired }), [sessionExpired])

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>
}

export function useSession() {
  const context = useContext(SessionContext)
  if (!context) {
    throw new Error('useSession must be used within SessionProvider')
  }
  return context
}
