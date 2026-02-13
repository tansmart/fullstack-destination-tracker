<template>
  <div class="d-flex align-items-center justify-content-center vh-100 bg-light">
    <div class="card shadow-sm p-4" style="max-width: 400px; width: 100%;">
      <h1 class="text-center mb-4">✈️ Travel Wishlist</h1>
      <h3 class="text-center mb-3">Login</h3>

      <input v-model="email" type="email" class="form-control mb-3" placeholder="Email" />
      <input v-model="password" type="password" class="form-control mb-4" placeholder="Password" />

      <button @click="login" class="btn btn-primary w-100 mb-3">Login</button>

      <div class="text-center">
        <small>Don’t have an account?
          <a href="#" @click.prevent="$emit('switch')">Register here</a>
        </small>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { api } from '../api'
import auth from '../auth'

const emit = defineEmits<{
  (e: 'switch'): void
}>()

const email = ref('')
const password = ref('')
const errorMessage = ref('')

const login = async () => {
  try {
    errorMessage.value = ''
    const res = await api.post('/auth/login', { email: email.value, password: password.value })
    const { accessToken, refreshToken } = res.data

    // Store tokens globally and in localStorage
    auth.login({ accessToken, refreshToken })
  } catch (err) {
    console.error('Login error:', err.response?.data || err)
    errorMessage.value = 'Invalid email or password.'
  }
}
</script>
