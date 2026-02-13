<template>
  <div class="card my-3 shadow-sm">
    <div class="card-body">
      <h5 class="card-title">Share & Export 📤</h5>
      <button class="btn btn-outline-primary me-2" @click="exportCsv">
        Export as CSV
      </button>
      <button class="btn btn-outline-secondary" disabled>
        Share Link (coming soon)
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { api } from '../api'

const exportCsv = async () => {
  const response = await api.get('/destinations/export/csv', { responseType: 'blob' })
  const url = window.URL.createObjectURL(new Blob([response.data]))
  const link = document.createElement('a')
  link.href = url
  link.setAttribute('download', 'destinations.csv')
  document.body.appendChild(link)
  link.click()
}
</script>
