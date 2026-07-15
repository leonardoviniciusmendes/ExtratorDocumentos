<script setup>
import { computed, onMounted, ref } from 'vue'

const apiBase = ref(localStorage.getItem('apiBase') || '')
const onlyCompatible = ref(true)
const objectiveFilter = ref('todos')
const supportFilter = ref('todos')
const search = ref('')
const loading = ref(false)
const syncing = ref(false)
const error = ref('')
const health = ref(null)
const credits = ref(null)
const models = ref([])
const lastUpdated = ref(null)

const endpoint = (path) => `${apiBase.value}${path}`

async function request(path, options = {}) {
  const response = await fetch(endpoint(path), options)
  const text = await response.text()
  const data = text ? JSON.parse(text) : null
  if (!response.ok) {
    const message = data?.erro || data?.mensagem || data?.title || response.statusText
    throw new Error(message)
  }
  return data
}

async function loadHealth() {
  try {
    const response = await fetch(endpoint('/health'))
    health.value = response.ok ? 'Healthy' : `HTTP ${response.status}`
  } catch {
    health.value = 'Indisponivel'
  }
}

async function loadCredits() {
  credits.value = await request('/api/openrouter/creditos')
}

async function loadModels() {
  models.value = await request(`/api/openrouter/modelos?apenasCompativeis=${onlyCompatible.value}`)
}

async function refreshAll() {
  loading.value = true
  error.value = ''
  localStorage.setItem('apiBase', apiBase.value)
  try {
    await Promise.all([loadHealth(), loadCredits(), loadModels()])
    lastUpdated.value = new Date()
  } catch (err) {
    error.value = err.message || 'Falha ao consultar a API.'
  } finally {
    loading.value = false
  }
}

async function syncModels() {
  syncing.value = true
  error.value = ''
  try {
    await request('/api/openrouter/modelos/sincronizar', { method: 'POST' })
    await refreshAll()
  } catch (err) {
    error.value = err.message || 'Falha ao sincronizar modelos.'
  } finally {
    syncing.value = false
  }
}

const objectives = computed(() => {
  const values = new Set(models.value.map((model) => model.objetivo).filter(Boolean))
  return ['todos', ...Array.from(values).sort((a, b) => a.localeCompare(b))]
})

const filteredModels = computed(() => {
  const term = search.value.trim().toLowerCase()
  return models.value.filter((model) => {
    const matchesTerm = !term ||
      model.id?.toLowerCase().includes(term) ||
      model.nome?.toLowerCase().includes(term) ||
      model.objetivo?.toLowerCase().includes(term)
    const matchesObjective = objectiveFilter.value === 'todos' || model.objetivo === objectiveFilter.value
    const matchesSupport = supportFilter.value === 'todos' ||
      (supportFilter.value === 'arquivo' && model.aceitaArquivo) ||
      (supportFilter.value === 'imagem' && model.aceitaImagem) ||
      (supportFilter.value === 'json' && model.suportaJson) ||
      (supportFilter.value === 'structured' && model.suportaStructuredOutputs)
    return matchesTerm && matchesObjective && matchesSupport
  })
})

const modelStats = computed(() => {
  const source = models.value
  return {
    total: source.length,
    available: source.filter((model) => model.disponivel).length,
    file: source.filter((model) => model.aceitaArquivo).length,
    image: source.filter((model) => model.aceitaImagem).length,
    json: source.filter((model) => model.suportaJson).length
  }
})

const usagePercent = computed(() => {
  const total = Number(credits.value?.creditosTotais)
  const used = Number(credits.value?.usoTotal)
  if (!total || Number.isNaN(total) || Number.isNaN(used)) return null
  return Math.min(100, Math.max(0, (used / total) * 100))
})

function money(value) {
  if (value === null || value === undefined || value === '') return '-'
  return Number(value).toLocaleString('pt-BR', {
    style: 'currency',
    currency: 'USD',
    minimumFractionDigits: 4,
    maximumFractionDigits: 6
  })
}

function number(value) {
  if (value === null || value === undefined) return '-'
  return Number(value).toLocaleString('pt-BR')
}

function dateTime(value) {
  if (!value) return '-'
  return new Date(value).toLocaleString('pt-BR')
}

onMounted(refreshAll)
</script>

<template>
  <main class="shell">
    <header class="topbar">
      <div>
        <h1>OpenRouter</h1>
        <p>Uso da conta, modelos disponiveis e compatibilidade com extracao.</p>
      </div>
      <div class="actions">
        <input v-model="apiBase" aria-label="Base da API" placeholder="API local" />
        <button type="button" @click="refreshAll" :disabled="loading">
          {{ loading ? 'Atualizando' : 'Atualizar' }}
        </button>
        <button type="button" class="secondary" @click="syncModels" :disabled="syncing || loading">
          {{ syncing ? 'Sincronizando' : 'Sincronizar modelos' }}
        </button>
      </div>
    </header>

    <section v-if="error" class="alert">
      {{ error }}
    </section>

    <section class="metrics">
      <article class="metric">
        <span>API</span>
        <strong>{{ health || '-' }}</strong>
        <small>{{ lastUpdated ? `Atualizado ${dateTime(lastUpdated)}` : 'Aguardando consulta' }}</small>
      </article>
      <article class="metric">
        <span>Saldo disponivel</span>
        <strong>{{ money(credits?.saldoDisponivel) }}</strong>
        <small>Total {{ money(credits?.creditosTotais) }}</small>
      </article>
      <article class="metric">
        <span>Uso total</span>
        <strong>{{ money(credits?.usoTotal) }}</strong>
        <div class="progress" aria-label="Uso de creditos">
          <div :style="{ width: `${usagePercent ?? 0}%` }"></div>
        </div>
      </article>
      <article class="metric">
        <span>Modelos</span>
        <strong>{{ modelStats.available }} / {{ modelStats.total }}</strong>
        <small>{{ modelStats.file }} arquivo, {{ modelStats.image }} imagem, {{ modelStats.json }} JSON</small>
      </article>
    </section>

    <section class="toolbar">
      <label>
        Busca
        <input v-model="search" placeholder="modelo, id ou objetivo" />
      </label>
      <label>
        Objetivo
        <select v-model="objectiveFilter">
          <option v-for="objective in objectives" :key="objective" :value="objective">
            {{ objective }}
          </option>
        </select>
      </label>
      <label>
        Suporte
        <select v-model="supportFilter">
          <option value="todos">todos</option>
          <option value="arquivo">arquivo</option>
          <option value="imagem">imagem</option>
          <option value="json">json</option>
          <option value="structured">structured outputs</option>
        </select>
      </label>
      <label class="check">
        <input v-model="onlyCompatible" type="checkbox" @change="loadModels" />
        Apenas compativeis
      </label>
    </section>

    <section class="table-panel">
      <div class="table-header">
        <h2>Modelos disponiveis</h2>
        <span>{{ filteredModels.length }} resultado(s)</span>
      </div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Modelo</th>
              <th>Objetivo</th>
              <th>Contexto</th>
              <th>Capacidades</th>
              <th>Entrada</th>
              <th>Saida</th>
              <th>Atualizado</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="model in filteredModels" :key="model.id">
              <td>
                <strong>{{ model.nome || model.id }}</strong>
                <small>{{ model.id }}</small>
              </td>
              <td>{{ model.objetivo || '-' }}</td>
              <td>{{ number(model.contextoTokens) }}</td>
              <td>
                <div class="badges">
                  <span v-if="model.aceitaArquivo">Arquivo</span>
                  <span v-if="model.aceitaImagem">Imagem</span>
                  <span v-if="model.suportaJson">JSON</span>
                  <span v-if="model.suportaStructuredOutputs">Schema</span>
                  <span v-if="model.disponivel" class="ok">Online</span>
                </div>
              </td>
              <td>{{ money(model.precoEntradaPorMilhaoTokens) }}</td>
              <td>{{ money(model.precoSaidaPorMilhaoTokens) }}</td>
              <td>{{ dateTime(model.atualizadoEm || model.ultimoVistoEm) }}</td>
            </tr>
            <tr v-if="filteredModels.length === 0">
              <td colspan="7" class="empty">Nenhum modelo encontrado.</td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>
  </main>
</template>
