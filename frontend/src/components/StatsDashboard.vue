<template>
  <div class="card card-body mt-3">
    <h5 class="mb-3">Travel Stats</h5>

    <div class="row g-4">
      <div class="col-md-6">
        <canvas ref="visitedChart"></canvas>
      </div>
      <div class="col-md-6">
        <canvas ref="continentChart"></canvas>
      </div>
    </div>
  </div>
</template>

<script lang="ts">
import { defineComponent, onMounted, ref, watch } from 'vue'
import { Chart, ArcElement, Tooltip, Legend, CategoryScale, LinearScale, BarElement } from 'chart.js'
import { api, Destination } from '../api'

Chart.register(ArcElement, Tooltip, Legend, CategoryScale, LinearScale, BarElement)

export default defineComponent({
  name: 'StatsDashboard',
  props: {
    refreshTrigger: Number
  },
  setup(props) {
    const visitedChart = ref<HTMLCanvasElement | null>(null)
    const continentChart = ref<HTMLCanvasElement | null>(null)
    let visitedInstance: Chart | null = null
    let continentInstance: Chart | null = null

    const loadStats = async () => {
      const res = await api.get<Destination[]>('/destinations')
      const destinations = res.data

      // 1. Visited breakdown
      const visited = destinations.filter(d => d.visited).length
      const notVisited = destinations.length - visited

      if (visitedInstance) visitedInstance.destroy()
      visitedInstance = new Chart(visitedChart.value!, {
        type: 'doughnut',
        data: {
          labels: ['Visited', 'Wishlist'],
          datasets: [{
            data: [visited, notVisited],
            backgroundColor: ['#36A2EB', '#FFCE56']
          }]
        },
        options: { plugins: { legend: { position: 'bottom' } } }
      })

      // 2. Continent breakdown (basic heuristic)
      const continentMap: Record<string, number> = {}
      destinations.forEach(d => {
        const country = d.country.toLowerCase()
        if (country.includes('usa') || country.includes('canada')) continentMap['North America'] = (continentMap['North America'] ?? 0) + 1
        else if (country.includes('brazil') || country.includes('argentina')) continentMap['South America'] = (continentMap['South America'] ?? 0) + 1
        else if (country.includes('france') || country.includes('germany') || country.includes('uk')) continentMap['Europe'] = (continentMap['Europe'] ?? 0) + 1
        else if (country.includes('japan') || country.includes('china') || country.includes('india')) continentMap['Asia'] = (continentMap['Asia'] ?? 0) + 1
        else if (country.includes('australia')) continentMap['Oceania'] = (continentMap['Oceania'] ?? 0) + 1
        else continentMap['Other'] = (continentMap['Other'] ?? 0) + 1
      })

      if (continentInstance) continentInstance.destroy()
      continentInstance = new Chart(continentChart.value!, {
        type: 'bar',
        data: {
          labels: Object.keys(continentMap),
          datasets: [{
            label: 'Destinations per Continent',
            data: Object.values(continentMap),
            backgroundColor: '#4BC0C0'
          }]
        },
        options: { scales: { y: { beginAtZero: true } } }
      })
    }

    onMounted(loadStats)
    watch(() => props.refreshTrigger, loadStats)

    return { visitedChart, continentChart }
  }
})
</script>
