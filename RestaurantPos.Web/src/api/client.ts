import axios from 'axios'
import { API_BASE_URL } from './config'

export const api = axios.create({
  baseURL: API_BASE_URL
})

let setGlobalLoading: ((isLoading: boolean) => void) | null = null
let pendingCount = 0

export function registerGlobalLoadingSetter(setter: (isLoading: boolean) => void) {
  setGlobalLoading = setter
}

function startLoading() {
  pendingCount += 1
  setGlobalLoading?.(true)
}

function stopLoading() {
  pendingCount = Math.max(0, pendingCount - 1)
  if (pendingCount === 0) {
    setGlobalLoading?.(false)
  }
}

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('pos_token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  startLoading()
  return config
})

api.interceptors.response.use(
  (response) => {
    stopLoading()
    return response
  },
  (error) => {
    stopLoading()
    return Promise.reject(error)
  }
)
