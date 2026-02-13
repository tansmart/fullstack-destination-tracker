<template>
  <!-- Auth -->
  <LoginForm v-if="!isLoggedIn && authMode === 'login'" 
    @switch="authMode = 'register'" />
  <RegisterForm v-else-if="!isLoggedIn" 
    @switch="authMode = 'login'" />

  <div class="container my-4" v-else>
    <div class="position-relative mb-3">
      <h1 class="text-center mb-4">✈️ Travel Wishlist</h1>
      <button
        class="btn btn-outline-danger btn-sm position-absolute top-0 end-0"
        style="margin: 0.5rem;"
        @click="logout">
        Logout
      </button>
    </div>

    <!-- Tabs -->
    <ul class="nav nav-tabs mb-3">
      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === 'main' }"
          @click="activeTab = 'main'"
        >
          Main
        </button>
      </li>
      <li class="nav-item">
        <button
          class="nav-link"
          :class="{ active: activeTab === 'stats' }"
          @click="activeTab = 'stats'"
        >
          Stats Dashboard
        </button>
      </li>
    </ul>

    <!-- MAIN TAB -->
    <div v-show="activeTab === 'main'" class="row">
      <div class="col-md-7">
        <DestinationForm @created="reload" />

        <div class="btn-group my-3">
          <button class="btn btn-outline-primary" @click="filter = 'all'">All</button>
          <button class="btn btn-outline-success" @click="filter = 'wishlist'">Wishlist</button>
          <button class="btn btn-outline-secondary" @click="filter = 'visited'">Visited</button>
        </div>

        <DestinationList :filter="filter" ref="listRef" @updated="reload" />
      </div>

      <div class="col-md-5">
        <!-- Progress Tracking -->
        <div class="card mb-3">
          <div class="card-body">
            <h5 class="card-title">Progress</h5>
            <p class="mb-2">
              🌍 {{ stats.visited }} of {{ stats.total }} visited
              ({{ completionRate }}%)
            </p>
            <div class="progress">
              <div
                class="progress-bar bg-success"
                role="progressbar"
                :style="{ width: completionRate + '%' }"
              ></div>
            </div>
          </div>
        </div>

        <!-- Map -->
        <h3 class="text-center">Map</h3>
        <div id="map" class="border rounded mb-3" style="height:400px;"></div>

        <!-- Export & Share -->
        <div class="d-flex justify-content-between">
          <div class="dropdown">
            <button
              class="btn btn-outline-dark dropdown-toggle"
              type="button"
              data-bs-toggle="dropdown"
            >
              Export
            </button>
            <ul class="dropdown-menu">
              <li><a class="dropdown-item" @click="exportData('csv')">CSV</a></li>
              <li><a class="dropdown-item" @click="exportData('json')">JSON</a></li>
              <li><a class="dropdown-item" @click="exportData('pdf')">PDF</a></li>
            </ul>
          </div>
          <button class="btn btn-outline-primary" @click="generateShareLink">
            Share Link
          </button>
        </div>
      </div>
    </div>

    <!-- STATS DASHBOARD TAB -->
    <div v-show="activeTab === 'stats'" class="row">
      <div class="col-md-6 mb-4">
        <div class="card">
          <div class="card-body">
            <h5 class="card-title text-center">Destinations by Region</h5>
            <canvas id="regionChart" height="250"></canvas>
          </div>
        </div>
      </div>

      <div class="col-md-6 mb-4">
        <div class="card">
          <div class="card-body">
            <h5 class="card-title text-center">Visited vs Wishlist</h5>
            <canvas id="continentChart" height="250"></canvas>
          </div>
        </div>
      </div>

      <div class="col-md-12 mb-4">
        <div class="card">
          <div class="card-body">
            <h5 class="card-title text-center">Progress Over Time</h5>
            <canvas id="progressChart" height="100"></canvas>
          </div>
        </div>
      </div>

      <div class="text-center">
        <button class="btn btn-outline-secondary mt-3" @click="activeTab = 'main'">
          ← Back to Main
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { Chart, registerables } from 'chart.js'
import DestinationForm from './components/DestinationForm.vue'
import DestinationList from './components/DestinationList.vue'
import LoginForm from './components/LoginForm.vue'
import RegisterForm from './components/RegisterForm.vue'
import { api, Destination } from './api'
import auth, { isAuthenticated } from './auth'

const authState = { isAuthenticated } // no readonly

// Auth state
const authMode = ref<'login' | 'register'>('login')

const isLoggedIn = computed(() => authState.isAuthenticated.value)

// Logout
const logout = () => {
  auth.logout()
  authMode.value = 'login'
}

Chart.register(...registerables)

// Tabs
const activeTab = ref<'main' | 'stats'>('main')

// Filters
const filter = ref<'all' | 'wishlist' | 'visited'>('all')

// List reference
const listRef = ref<InstanceType<typeof DestinationList> | null>(null)

// Stats
const stats = ref({ total: 0, visited: 0, wishlist: 0 })
const completionRate = computed(() =>
  stats.value.total > 0 ? Math.round((stats.value.visited / stats.value.total) * 100) : 0
)

// Map
const map = ref<L.Map>()
const markers: L.Marker[] = []

// Chart refs
let regionChart: Chart
let continentChart: Chart
let progressChart: Chart

// Load stats
const loadStats = async () => {
  try {
    const res = await api.get('/destinations/stats')
    stats.value = res.data
  } catch {
    const res = await api.get('/destinations')
    const data: Destination[] = res.data
    const visited = data.filter(d => d.visited).length
    stats.value = { total: data.length, visited, wishlist: data.length - visited }
  }
}

// Load map + markers
const loadMap = async () => {
  const destinations: Destination[] = (await api.get('/destinations')).data

  if (!map.value) {
    map.value = L.map('map', {
      center: [20, 0],
      zoom: 2,
      worldCopyJump: true
    })
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap contributors'
    }).addTo(map.value)
  }

  markers.forEach(m => m.remove())
  markers.length = 0

  destinations.forEach(d => {
    if (d.latitude && d.longitude) {
      const m = L.marker([d.latitude, d.longitude]).addTo(map.value!)
      m.bindPopup(`<b>${d.city}, ${d.country}</b><br>${d.notes ?? ''}`)
      markers.push(m)
    }
  })

  if (markers.length > 1) {
    const group = L.featureGroup(markers)
    map.value!.fitBounds(group.getBounds(), { padding: [50, 50], maxZoom: 8 })
  } else if (markers.length === 1) {
    map.value!.setView(markers[0].getLatLng(), 6)
  } else {
    map.value!.setView([20, 0], 2)
  }
}

// --- Charts ---
const initCharts = async () => {
  const res = await api.get('/destinations')
  const data: Destination[] = res.data
  if (data.length === 0) return

  const regionMap: Record<string, string> = {
    'France': 'Europe', 'Germany': 'Europe', 'Spain': 'Europe',
    'Japan': 'Asia', 'China': 'Asia', 'India': 'Asia',
    'USA': 'Americas', 'Canada': 'Americas', 'Brazil': 'Americas',
    'South Africa': 'Africa', 'Egypt': 'Africa', 'Kenya': 'Africa',
    'Australia': 'Oceania', 'New Zealand': 'Oceania'
  }

  const regionCounts: Record<string, number> = {}
  const continentCounts = { Visited: 0, Wishlist: 0 }

  data.forEach(d => {
    const region = regionMap[d.country] || 'Other'
    regionCounts[region] = (regionCounts[region] || 0) + 1
    d.visited ? continentCounts.Visited++ : continentCounts.Wishlist++
  })

  regionChart?.destroy()
  continentChart?.destroy()
  progressChart?.destroy()

  const ctx1 = document.getElementById('regionChart') as HTMLCanvasElement
  regionChart = new Chart(ctx1, {
    type: 'bar',
    data: { labels: Object.keys(regionCounts), datasets: [{ label: 'Destinations by Region', data: Object.values(regionCounts), backgroundColor: '#4e73df' }] },
    options: { responsive: true, plugins: { legend: { display: false } } }
  })

  const ctx2 = document.getElementById('continentChart') as HTMLCanvasElement
  continentChart = new Chart(ctx2, {
    type: 'pie',
    data: { labels: Object.keys(continentCounts), datasets: [{ data: Object.values(continentCounts), backgroundColor: ['#1cc88a', '#f6c23e'] }] },
    options: { plugins: { legend: { position: 'bottom' } } }
  })

  const ctx3 = document.getElementById('progressChart') as HTMLCanvasElement
  progressChart = new Chart(ctx3, {
    type: 'line',
    data: { labels: ['Start', 'Now'], datasets: [{ label: 'Completion Rate (%)', data: [0, completionRate.value], borderColor: '#36b9cc', fill: false }] },
    options: { scales: { y: { beginAtZero: true, max: 100 } }, plugins: { legend: { display: false } } }
  })
}

// Reload everything
const reload = () => {
  listRef.value?.load()
  loadMap()
  loadStats()
  if (activeTab.value === 'stats') initCharts()
}

// Export
const exportData = async (format: string) => {
  try {
    const res = await api.get(`/destinations/export?format=${format}`, { responseType: 'blob' })
    const url = URL.createObjectURL(res.data)
    const a = document.createElement('a')
    a.href = url
    a.download = `travel-wishlist.${format}`
    a.click()
    URL.revokeObjectURL(url)
  } catch (err) {
    console.error('Export failed', err)
    alert('Export failed')
  }
}

// Share
const generateShareLink = () => {
  const fakeId = Math.random().toString(36).substring(2, 8)
  const shareUrl = `${window.location.origin}/share/${fakeId}`
  navigator.clipboard.writeText(shareUrl)
  alert(`Share link copied to clipboard:\n${shareUrl}`)
}

// Watch tab to update charts and map
watch(activeTab, (tab) => {
  if (tab === 'stats') initCharts()
  else if (tab === 'main') setTimeout(() => map.value?.invalidateSize(), 0)
})

onMounted(() => {
  loadMap()
  loadStats()
})
</script>
