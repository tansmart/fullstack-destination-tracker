<template>
  <div class="card my-3 shadow-sm">
    <div class="card-body">
      <h5 class="card-title">Your Travel Progress 🌍</h5>
      <p class="mb-2">
        {{ stats.visited }} of {{ stats.total }} destinations visited
        ({{ percent }}%)
      </p>
      <div class="progress">
        <div
          class="progress-bar bg-success"
          role="progressbar"
          :style="{ width: percent + '%' }"
          :aria-valuenow="percent"
          aria-valuemin="0"
          aria-valuemax="100"
        >
          {{ percent }}%
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { api } from '../api'

const stats = ref({ total: 0, visited: 0, wishlist: 0 })

const percent = computed(() =>
  stats.value.total > 0 ? Math.round((stats.value.visited / stats.value.total) * 100) : 0
)

const loadStats = async () => {
  stats.value = (await api.get('/destinations/stats')).data
}

onMounted(loadStats)
</script>
