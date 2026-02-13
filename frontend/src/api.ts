import axios from 'axios'
import auth from './auth'

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE || 'http://localhost:5208/api'
})

api.interceptors.request.use((config) => {
  const token = auth.getAccessToken()
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true
      const newToken = await auth.refreshTokens()
      if (newToken) {
        originalRequest.headers.Authorization = `Bearer ${newToken}`
        return api(originalRequest)
      }
    }
    return Promise.reject(error)
  }
)

export interface Destination {
  id?: number
  city: string
  country: string
  notes?: string
  visited: boolean
  latitude?: number
  longitude?: number
}
