<template>
  <div class="card card-body">
    <h5>Destinations</h5>
    <div v-if="loading" class="text-muted">Loading...</div>
    <div v-else>
      <table class="table table-striped table-hover">
        <thead>
          <tr>
            <th>City</th>
            <th>Country</th>
            <th>Notes</th>
            <th>Visited</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="d in filtered" :key="d.id">
            <td>{{ d.city }}</td>
            <td>{{ d.country }}</td>
            <td>{{ d.notes }}</td>
            <td>
              <input
                type="checkbox"
                class="form-check-input"
                v-model="d.visited"
                @change="toggleVisited(d)"
              />
            </td>
            <td>
              <button class="btn btn-sm btn-danger" @click="remove(d)">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
      <div v-if="filtered.length === 0" class="text-muted">No destinations</div>
    </div>
  </div>
</template>

<script lang="ts" setup>
import { ref, computed, watch } from 'vue'
import { api, Destination } from '../api'

// Props
interface Props {
  filter?: 'all' | 'wishlist' | 'visited'
}
const props = defineProps<Props>()

// Emits
const emit = defineEmits<{
  (e: 'updated'): void
}>()

// State
const list = ref<Destination[]>([])
const loading = ref(false)

// Load destinations
const load = async () => {
  loading.value = true
  try {
    const res = await api.get<Destination[]>('/destinations')
    list.value = res.data
  } catch (e) {
    console.error(e)
    alert('Failed to load destinations')
  } finally {
    loading.value = false
  }
}

// Toggle visited status
const toggleVisited = async (d: Destination) => {
  try {
    await api.put(`/destinations/${d.id}`, d) // send full object with updated visited
    emit('updated') // notify parent to refresh progress/map/stats
  } catch (e) {
    console.error(e)
    d.visited = !d.visited // revert on failure
    alert('Failed to update')
  }
}

// Remove destination
const remove = async (d: Destination) => {
  if (!confirm(`Delete ${d.city}, ${d.country}?`)) return
  try {
    await api.delete(`/destinations/${d.id}`)
    await load()
    emit('updated') // refresh parent after deletion
  } catch {
    alert('Delete failed')
  }
}

// Computed filtered list
const filtered = computed(() => {
  if (props.filter === 'visited') return list.value.filter(x => x.visited)
  if (props.filter === 'wishlist') return list.value.filter(x => !x.visited)
  return list.value
})

// Expose load to parent
defineExpose({ load })

// Initial load
load()
</script>
