<template>
  <div class="d-flex align-items-center justify-content-center vh-100 bg-light">
    <div class="card shadow-sm p-4" style="max-width: 400px; width: 100%;">
      <h1 class="text-center mb-4">✈️ Travel Wishlist</h1>
      <h3 class="text-center mb-3">Register</h3>

      <input
        v-model="email"
        type="email"
        placeholder="Email"
        class="form-control mb-3"
      />
      <input
        v-model="password"
        type="password"
        placeholder="Password"
        class="form-control mb-3"
      />
      <input
        v-model="confirmPassword"
        type="password"
        placeholder="Confirm Password"
        class="form-control mb-3"
      />

      <div v-if="passwordError" class="text-danger mb-3">{{ passwordError }}</div>

      <button
        @click="register"
        class="btn btn-success w-100 mb-3"
      >
        Register
      </button>

      <div class="text-center">
        <small>
          Already have an account?
          <a href="#" @click.prevent="$emit('switch')">Login here</a>
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
const confirmPassword = ref('')
const passwordError = ref('')

const validatePassword = (pwd: string): boolean => {
  // Minimum 8 chars, 1 uppercase, 1 special character
  const regex = /^(?=.*[A-Z])(?=.*[!@#$%^&*(),.?":{}|<>]).{8,}$/
  return regex.test(pwd)
}

const register = async () => {
  if (password.value !== confirmPassword.value) {
    passwordError.value = 'Passwords do not match.'
    return
  }

  if (!validatePassword(password.value)) {
    passwordError.value = 'Password must be at least 8 characters, contain one uppercase letter and one special character.'
    return
  }

  passwordError.value = ''

  try {
    // Create user
    await api.post('/auth/register', { email: email.value, password: password.value })

    // Then login to get tokens
    const res = await api.post('/auth/login', { email: email.value, password: password.value })
    const { accessToken, refreshToken } = res.data

    // store tokens and update global state
    auth.login({ accessToken, refreshToken })
  } catch (err) {
    console.error('Registration error:', err.response?.data || err)
    passwordError.value = 'Registration failed'
  }
}
</script>
