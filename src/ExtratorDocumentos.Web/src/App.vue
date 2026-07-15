<script setup>
import { computed, onMounted, ref } from 'vue'

const apiBase = ref(localStorage.getItem('apiBase') || '')
const activeView = ref('dashboard')
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
const documents = ref([])
const documentsTotal = ref(0)
const lastUpdated = ref(null)
const uploadType = ref('0')
const uploadFile = ref(null)
const uploading = ref(false)
const uploadHistory = ref(loadUploadHistory())
const selectedUploadId = ref(uploadHistory.value[0]?.id || null)

const menuItems = [
  { value: 'dashboard', label: 'Dashboard' },
  { value: 'documentos', label: 'Documentos' },
  { value: 'openrouter', label: 'OpenRouter' }
]

const documentTypes = [
  { value: '0', label: 'Identificacao' },
  { value: '1', label: 'Endereco' },
  { value: '2', label: 'Contrato' }
]

const persistedDocumentTypeNames = {
  0: 'RG',
  1: 'CPF',
  2: 'CNH',
  3: 'ComprovanteResidencia',
  4: 'ContaLuz',
  5: 'CertidaoNascimento',
  6: 'CertidaoCasamento',
  7: 'ContratoSocial',
  8: 'CartaoCNPJ',
  9: 'ContaAgua',
  10: 'ContaTelefone',
  11: 'ContaInternet',
  12: 'ContaGas',
  13: 'FaturaCartaoCredito',
  14: 'ExtratoBancario',
  15: 'ContratoLocacao',
  16: 'IPTU',
  17: 'Elegibilidade',
  18: 'FichaAssociativa',
  19: 'DocumentoOficialComSelfie',
  99: 'Outros'
}

const statusDocumentoNames = {
  0: 'Pendente',
  1: 'Disponivel',
  2: 'EmAnalise',
  3: 'Aprovado',
  4: 'Rejeitado',
  5: 'Excluido',
  6: 'Erro',
  7: 'Concluido',
  8: 'Processando'
}

const statusExtracaoNames = {
  0: 'Pendente',
  1: 'Processando',
  2: 'Processado',
  3: 'RevisaoManual',
  4: 'Erro',
  5: 'Concluido',
  6: 'ConcluidoComAlertas',
  7: 'RequerRevisao'
}

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

async function loadDocuments() {
  const result = await request('/api/Documentos?page=1&pageSize=100')
  documents.value = result?.items || []
  documentsTotal.value = result?.total ?? documents.value.length
}

async function refreshAll() {
  loading.value = true
  error.value = ''
  localStorage.setItem('apiBase', apiBase.value)
  try {
    await Promise.all([loadHealth(), loadCredits(), loadModels(), loadDocuments()])
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

async function uploadDocument() {
  if (!uploadFile.value) {
    error.value = 'Selecione um arquivo para processar.'
    return
  }

  uploading.value = true
  error.value = ''
  try {
    const form = new FormData()
    form.append('TipoDocumento', uploadType.value)
    form.append('Arquivo', uploadFile.value)

    const startedAt = new Date()
    const result = await request('/api/Documentos', {
      method: 'POST',
      body: form
    })
    const item = {
      id: crypto.randomUUID(),
      tipoDocumento: uploadType.value,
      tipoNome: typeName(uploadType.value),
      arquivo: uploadFile.value.name,
      tamanhoBytes: uploadFile.value.size,
      criadoEm: startedAt.toISOString(),
      httpStatus: result?.status || 'Processado',
      retorno: result
    }
    uploadHistory.value = [item, ...uploadHistory.value].slice(0, 50)
    selectedUploadId.value = item.id
    saveUploadHistory()
    await loadDocuments()
  } catch (err) {
    error.value = err.message || 'Falha ao processar o documento.'
  } finally {
    uploading.value = false
  }
}

function onFileChange(event) {
  uploadFile.value = event.target.files?.[0] || null
}

function clearUploadHistory() {
  uploadHistory.value = []
  selectedUploadId.value = null
  saveUploadHistory()
}

function loadUploadHistory() {
  try {
    return JSON.parse(localStorage.getItem('uploadHistory') || '[]')
  } catch {
    return []
  }
}

function saveUploadHistory() {
  localStorage.setItem('uploadHistory', JSON.stringify(uploadHistory.value))
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

const uploadsByType = computed(() => {
  return documentTypes.map((type) => ({
    ...type,
    count: uploadHistory.value.filter((item) => item.tipoDocumento === type.value).length
  }))
})

const documentsByType = computed(() => {
  const counts = documents.value.reduce((acc, item) => {
    const key = String(item.tipo)
    acc[key] = (acc[key] || 0) + 1
    return acc
  }, {})

  return Object.entries(counts)
    .map(([value, count]) => ({
      value,
      label: persistedDocumentTypeName(value),
      count
    }))
    .sort((a, b) => b.count - a.count || a.label.localeCompare(b.label))
})

const documentDashboard = computed(() => ({
  total: documentsTotal.value,
  processed: documents.value.filter((item) => [2, 5, 6, 7].includes(Number(item.statusExtracao))).length,
  pending: documents.value.filter((item) => [0, 1].includes(Number(item.statusExtracao))).length,
  errors: documents.value.filter((item) => Number(item.statusExtracao) === 4 || Number(item.status) === 6).length,
  uploads: uploadHistory.value.length
}))

const selectedUpload = computed(() => {
  return uploadHistory.value.find((item) => item.id === selectedUploadId.value) || uploadHistory.value[0] || null
})

const selectedReturn = computed(() => {
  if (!selectedUpload.value) return ''
  return JSON.stringify(selectedUpload.value.retorno, null, 2)
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

function typeName(value) {
  return documentTypes.find((type) => type.value === String(value))?.label || `Tipo ${value}`
}

function persistedDocumentTypeName(value) {
  return persistedDocumentTypeNames[Number(value)] || `Tipo ${value}`
}

function statusDocumentoName(value) {
  return statusDocumentoNames[Number(value)] || String(value ?? '-')
}

function statusExtracaoName(value) {
  return statusExtracaoNames[Number(value)] || String(value ?? '-')
}

function fileSize(value) {
  if (!value) return '-'
  if (value < 1024) return `${value} B`
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`
  return `${(value / 1024 / 1024).toFixed(1)} MB`
}

onMounted(refreshAll)
</script>

<template>
  <main class="shell">
    <header class="topbar">
      <div>
        <h1>Extrator Documentos</h1>
        <p>Monitoramento de modelos, uso OpenRouter e verificacao dos retornos de documentos.</p>
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

    <nav class="main-menu" aria-label="Menu principal">
      <button
        v-for="item in menuItems"
        :key="item.value"
        type="button"
        :class="{ active: activeView === item.value }"
        @click="activeView = item.value"
      >
        {{ item.label }}
      </button>
    </nav>

    <section v-if="error" class="alert">
      {{ error }}
    </section>

    <template v-if="activeView === 'dashboard'">
      <section class="metrics">
        <article class="metric">
          <span>Documentos</span>
          <strong>{{ documentDashboard.total }}</strong>
          <small>{{ documentsByType.length }} tipo(s) encontrados</small>
        </article>
        <article class="metric">
          <span>Processados</span>
          <strong>{{ documentDashboard.processed }}</strong>
          <small>{{ documentDashboard.uploads }} upload(s) pela tela</small>
        </article>
        <article class="metric">
          <span>Pendentes</span>
          <strong>{{ documentDashboard.pending }}</strong>
          <small>Status de extracao em aberto</small>
        </article>
        <article class="metric">
          <span>Erros</span>
          <strong>{{ documentDashboard.errors }}</strong>
          <small>Falhas de documento ou extracao</small>
        </article>
      </section>

      <section class="dashboard-grid">
        <div class="documents-panel">
          <div class="panel-header">
            <div>
              <h2>Quantidade por tipo</h2>
              <p>Tipos identificados nos documentos salvos pela API.</p>
            </div>
          </div>
          <div class="type-summary stacked">
            <article v-for="type in documentsByType" :key="type.value">
              <span>{{ type.label }}</span>
              <strong>{{ type.count }}</strong>
            </article>
            <div v-if="documentsByType.length === 0" class="empty compact">
              Nenhum documento listado pela API.
            </div>
          </div>
        </div>

        <div class="documents-panel">
          <div class="panel-header">
            <div>
              <h2>Ultimos retornos</h2>
              <p>Respostas recebidas nos uploads feitos por esta tela.</p>
            </div>
            <button type="button" class="secondary" @click="activeView = 'documentos'">
              Ver documentos
            </button>
          </div>
          <div class="history-list compact-list">
            <button
              v-for="item in uploadHistory.slice(0, 6)"
              :key="item.id"
              type="button"
              @click="activeView = 'documentos'; selectedUploadId = item.id"
            >
              <strong>{{ item.arquivo }}</strong>
              <span>{{ item.tipoNome }} - {{ dateTime(item.criadoEm) }}</span>
            </button>
            <div v-if="uploadHistory.length === 0" class="empty compact">
              Nenhum retorno registrado nesta estacao.
            </div>
          </div>
        </div>
      </section>
    </template>

    <template v-else-if="activeView === 'openrouter'">
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
    </template>

    <template v-else>
    <section class="documents-panel">
      <div class="panel-header">
        <div>
          <h2>Documentos</h2>
          <p>Envie arquivos, consulte documentos salvos e confira o retorno recebido.</p>
        </div>
        <button type="button" class="secondary" @click="clearUploadHistory" :disabled="uploadHistory.length === 0">
          Limpar historico
        </button>
      </div>

      <div class="upload-grid">
        <form class="upload-box" @submit.prevent="uploadDocument">
          <label>
            Tipo
            <select v-model="uploadType">
              <option v-for="type in documentTypes" :key="type.value" :value="type.value">
                {{ type.value }} - {{ type.label }}
              </option>
            </select>
          </label>
          <label>
            Arquivo
            <input type="file" accept=".pdf,image/*" @change="onFileChange" />
          </label>
          <button type="submit" :disabled="uploading">
            {{ uploading ? 'Processando' : 'Processar arquivo' }}
          </button>
          <small v-if="uploadFile">
            {{ uploadFile.name }} - {{ fileSize(uploadFile.size) }}
          </small>
        </form>

        <div class="type-summary">
          <article v-for="type in uploadsByType" :key="type.value">
            <span>{{ type.value }} - {{ type.label }}</span>
            <strong>{{ type.count }}</strong>
          </article>
        </div>
      </div>

      <section class="table-panel documents-table">
        <div class="table-header">
          <h2>Lista de documentos</h2>
          <span>{{ documentsTotal }} documento(s)</span>
        </div>
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Documento</th>
                <th>Tipo</th>
                <th>Status</th>
                <th>Extracao</th>
                <th>Versao</th>
                <th>Criado em</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="documento in documents" :key="documento.id">
                <td>
                  <strong>{{ documento.cpf || documento.cnpj || documento.id }}</strong>
                  <small>{{ documento.id }}</small>
                </td>
                <td>{{ persistedDocumentTypeName(documento.tipo) }}</td>
                <td>{{ statusDocumentoName(documento.status) }}</td>
                <td>{{ statusExtracaoName(documento.statusExtracao) }}</td>
                <td>{{ documento.versaoAtual }}</td>
                <td>{{ dateTime(documento.criadoEm) }}</td>
              </tr>
              <tr v-if="documents.length === 0">
                <td colspan="6" class="empty">Nenhum documento retornado pela API.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <div class="results-grid">
        <div class="history-list">
          <button
            v-for="item in uploadHistory"
            :key="item.id"
            type="button"
            :class="{ active: selectedUpload?.id === item.id }"
            @click="selectedUploadId = item.id"
          >
            <strong>{{ item.arquivo }}</strong>
            <span>{{ item.tipoNome }} - {{ dateTime(item.criadoEm) }}</span>
          </button>
          <div v-if="uploadHistory.length === 0" class="empty compact">
            Nenhum arquivo processado nesta estacao.
          </div>
        </div>

        <div class="return-view">
          <div class="return-header">
            <div>
              <strong>{{ selectedUpload?.arquivo || 'Retorno' }}</strong>
              <span v-if="selectedUpload">
                {{ selectedUpload.tipoNome }} - {{ fileSize(selectedUpload.tamanhoBytes) }}
              </span>
            </div>
          </div>
          <pre>{{ selectedReturn || '{}' }}</pre>
        </div>
      </div>
    </section>
    </template>
  </main>
</template>
