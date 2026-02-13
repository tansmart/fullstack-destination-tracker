<template>
  <div class="card card-body mb-3">
    <h5>Add Destination</h5>
    <form @submit.prevent="submit">
      <div class="mb-2">
        <input v-model="city" class="form-control" placeholder="City" required />
      </div>
      <div class="mb-2">
        <input v-model="country" class="form-control" placeholder="Country" required />
      </div>
      <div class="mb-2">
        <input v-model="notes" class="form-control" placeholder="Notes (optional)" />
      </div>
      <div class="d-flex gap-2">
        <button type="submit" class="btn btn-primary">Add</button>
      </div>
    </form>
  </div>
</template>

<script lang="ts">
import { defineComponent, ref } from 'vue'
import { api, Destination } from '../api'

export default defineComponent({
  emits: ['created'],
  setup(_, { emit }) {
    const city = ref('')
    const country = ref('')
    const notes = ref('')
    const latitude = ref<number | null>(null)
    const longitude = ref<number | null>(null)

    const tryGeocode = async () => {
      const q = encodeURIComponent(`${city.value}, ${country.value}`)
      try {
        const r = await fetch(`https://nominatim.openstreetmap.org/search?q=${q}&format=json&limit=1`)
        const data = await r.json()
        if (data.length) {
          latitude.value = parseFloat(data[0].lat)
          longitude.value = parseFloat(data[0].lon)
          return true
        }
      } catch (e) {
        console.error(e)
      }
      return false
    }

    const submit = async () => {
      // Auto geocode if missing
      if (!latitude.value || !longitude.value) {
        const ok = await tryGeocode()
        if (!ok) return alert('Cannot locate this city')
      }

      const payload: Destination = {
        city: city.value,
        country: country.value,
        notes: notes.value,
        visited: false,
        latitude: latitude.value!,
        longitude: longitude.value!
      }

      try {
        await api.post('/destinations', payload)
        city.value = ''
        country.value = ''
        notes.value = ''
        latitude.value = null
        longitude.value = null
        emit('created')
      } catch (e) {
        console.error(e)
        alert('Failed to add destination')
      }
    }

    return { city, country, notes, submit }
  }
})
</script>
