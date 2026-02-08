import { createContext, useContext, useMemo, useState, useEffect } from 'react'
import { registerGlobalLoadingSetter } from '../api/client'

type LoadingContextValue = {
  activeCount: number
  startLoading: () => void
  stopLoading: () => void
}

const LoadingContext = createContext<LoadingContextValue | undefined>(undefined)

export function LoadingProvider({ children }: { children: React.ReactNode }) {
  const [activeCount, setActiveCount] = useState(0)

  const startLoading = () => setActiveCount((prev) => prev + 1)
  const stopLoading = () => setActiveCount((prev) => Math.max(0, prev - 1))

  useEffect(() => {
    registerGlobalLoadingSetter((isLoading) => {
      setActiveCount(isLoading ? 1 : 0)
      if (isLoading) {
        document.body.classList.add('loading')
      } else {
        document.body.classList.remove('loading')
      }
    })
  }, [])

  const value = useMemo(() => ({ activeCount, startLoading, stopLoading }), [activeCount])

  return <LoadingContext.Provider value={value}>{children}</LoadingContext.Provider>
}

export function useLoading() {
  const context = useContext(LoadingContext)
  if (!context) {
    throw new Error('useLoading must be used within LoadingProvider')
  }
  return context
}
