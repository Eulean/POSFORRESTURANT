// The packaged app serves its API and UI from the same laptop.
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

export function mediaUrl(path: string | null | undefined): string {
  if (!path) return ''
  if (/^https?:\/\//i.test(path)) return path
  return `${API_BASE_URL.replace(/\/+$/, '')}/${path.replace(/^\/+/, '')}`
}
