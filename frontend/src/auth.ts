import { ref } from 'vue'
import axios from 'axios'

export const isAuthenticated = ref(!!localStorage.getItem('accessToken'))

const accessToken = ref(localStorage.getItem('accessToken') || '')
const refreshToken = ref(localStorage.getItem('refreshToken') || '')

export function login(tokens: { accessToken: string; refreshToken: string }) {
  accessToken.value = tokens.accessToken
  refreshToken.value = tokens.refreshToken

  localStorage.setItem('accessToken', tokens.accessToken)
  localStorage.setItem('refreshToken', tokens.refreshToken)

  isAuthenticated.value = true
}

export function logout() {
  accessToken.value = ''
  refreshToken.value = ''
  localStorage.removeItem('accessToken')
  localStorage.removeItem('refreshToken')
  isAuthenticated.value = false
}

export function getAccessToken() {
  return accessToken.value
}

export async function refreshTokens() {
  try {
    const res = await axios.post('/api/auth/refresh', {
      refreshToken: refreshToken.value,
    })

    const { accessToken: newAccess, refreshToken: newRefresh } = res.data
    accessToken.value = newAccess
    refreshToken.value = newRefresh

    localStorage.setItem('accessToken', newAccess)
    localStorage.setItem('refreshToken', newRefresh)

    return newAccess
  } catch (err) {
    console.warn('Token refresh failed:', err)
    logout()
    return null
  }
}

export default { isAuthenticated, login, logout, getAccessToken, refreshTokens }
