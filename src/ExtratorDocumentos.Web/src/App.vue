<script setup>
import { computed, onMounted, reactive, ref } from 'vue'

const apiBase = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '')
const activeTab = ref('openrouter')
const tiposView = ref('visualizacao')
const loading = reactive({
  creditos: false,
  modelos: false,
  sincronizar: false,
  tipos: false,
  schemas: false,
  sugestao: false,
  tipoAcao: false,
  schemaAcao: false
})
const errors = reactive({
  openrouter: '',
  tipos: '',
  sugestao: '',
  acao: ''
})

const creditos = ref(null)
const modelos = ref([])
const apenasCompativeis = ref(true)
const sincronizacao = ref(null)

const tipos = ref([])
const tipoSelecionado = ref(null)
const schemas = ref([])
const schemaSelecionado = ref(null)
const sugestao = ref(null)
const filtroTipos = ref('')
const schemaEditor = ref('')
const validacaoSchema = ref(null)
const novoCampoSchema = reactive({
  chave: '',
  nomeExibicao: '',
  descricao: '',
  tipoDado: 'texto',
  obrigatorioSugerido: false
})

const tiposDadoSchema = [
  'texto',
  'inteiro',
  'decimal',
  'booleano',
  'data',
  'data_hora',
  'objeto',
  'lista'
]

const formSugestao = reactive({
  tipoDocumento: 'cnh',
  descricaoTipoDocumento: '',
  pais: 'BRA',
  idioma: 'pt',
  arquivo: null
})

const modelosResumo = computed(() => {
  const ativos = modelos.value.filter((m) => m.ativo ?? m.disponivel ?? true).length
  const imagem = modelos.value.filter((m) => m.aceitaImagem || m.suportaImagem).length
  const json = modelos.value.filter((m) => m.suportaJson || m.suportaStructuredOutputs).length
  return { total: modelos.value.length, ativos, imagem, json }
})

const tiposResumo = computed(() => {
  const ativos = tipos.value.filter((t) => t.ativo).length
  const confirmados = tipos.value.filter((t) => t.confirmado).length
  return { total: tipos.value.length, ativos, confirmados }
})

const tiposFiltrados = computed(() => {
  const termo = filtroTipos.value.trim().toLowerCase()
  if (!termo) return tipos.value
  return tipos.value.filter((tipo) =>
    `${tipo.codigo} ${tipo.nome} ${tipo.descricao || ''}`
      .toLowerCase()
      .includes(termo)
  )
})

function endpoint(path) {
  return `${apiBase}${path}`
}

async function requestJson(path, options = {}) {
  const response = await fetch(endpoint(path), options)
  const contentType = response.headers.get('content-type') || ''
  const body = contentType.includes('application/json')
    ? await response.json()
    : await response.text()

  if (!response.ok) {
    const message = typeof body === 'string'
      ? body
      : body.mensagem || body.erro || JSON.stringify(body)
    throw new Error(message || `Erro HTTP ${response.status}`)
  }

  return body
}

async function carregarCreditos() {
  loading.creditos = true
  errors.openrouter = ''
  try {
    creditos.value = await requestJson('/api/openrouter/creditos')
  } catch (error) {
    errors.openrouter = error.message
  } finally {
    loading.creditos = false
  }
}

async function carregarModelos() {
  loading.modelos = true
  errors.openrouter = ''
  try {
    modelos.value = await requestJson(`/api/openrouter/modelos?apenasCompativeis=${apenasCompativeis.value}`)
  } catch (error) {
    errors.openrouter = error.message
  } finally {
    loading.modelos = false
  }
}

async function sincronizarModelos() {
  loading.sincronizar = true
  errors.openrouter = ''
  try {
    sincronizacao.value = await requestJson('/api/openrouter/modelos/sincronizar', {
      method: 'POST'
    })
    await carregarModelos()
  } catch (error) {
    errors.openrouter = error.message
  } finally {
    loading.sincronizar = false
  }
}

async function carregarTipos() {
  loading.tipos = true
  errors.tipos = ''
  try {
    tipos.value = await requestJson('/api/tipos-documento')
    if (!tipoSelecionado.value && tipos.value.length > 0) {
      selecionarTipo(tipos.value[0])
    }
  } catch (error) {
    errors.tipos = error.message
  } finally {
    loading.tipos = false
  }
}

async function selecionarTipo(tipo) {
  tipoSelecionado.value = tipo
  schemaSelecionado.value = null
  await carregarSchemas(tipo.codigo || tipo.id)
}

function atualizarTipoSelecionado(tipoAtualizado) {
  const index = tipos.value.findIndex((tipo) => tipo.id === tipoAtualizado.id)
  if (index >= 0) {
    tipos.value[index] = tipoAtualizado
  }
  tipoSelecionado.value = tipoAtualizado
}

async function executarAcaoTipo(acao) {
  if (!tipoSelecionado.value?.id) return
  loading.tipoAcao = true
  errors.acao = ''
  try {
    const atualizado = await requestJson(`/api/tipos-documento/${tipoSelecionado.value.id}/${acao}`, {
      method: 'POST'
    })
    atualizarTipoSelecionado(atualizado)
  } catch (error) {
    errors.acao = error.message
  } finally {
    loading.tipoAcao = false
  }
}

async function executarAcaoSchema(acao) {
  if (!tipoSelecionado.value?.id || !schemaSelecionado.value?.id) return
  loading.schemaAcao = true
  errors.acao = ''
  try {
    const tipoCodigo = tipoSelecionado.value.codigo || tipoSelecionado.value.id
    const schemaId = schemaSelecionado.value.id
    if (acao === 'publicar') {
      await requestJson(`/api/tipos-documento/${encodeURIComponent(tipoCodigo)}/schemas/${schemaId}/publicar`, {
        method: 'POST'
      })
    } else {
      await requestJson(`/api/tipos-documento/${tipoSelecionado.value.id}/schemas/${schemaId}/ativar`, {
        method: 'POST'
      })
    }
    await carregarSchemas(tipoCodigo)
  } catch (error) {
    errors.acao = error.message
  } finally {
    loading.schemaAcao = false
  }
}

function selecionarSchema(schema) {
  schemaSelecionado.value = schema
  schemaEditor.value = stringify(schema)
  validacaoSchema.value = null
}

function schemaPermiteEdicao() {
  return schemaSelecionado.value?.status === 'Rascunho'
}

function normalizarChaveSchema(valor) {
  const semAcento = (valor || '')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .replace(/[^a-zA-Z0-9_\s-]/g, ' ')
    .trim()

  if (!semAcento) return ''

  const partes = semAcento
    .split(/[\s_-]+/)
    .filter(Boolean)
    .map((parte) => parte.toLowerCase())

  return partes
    .map((parte, index) => index === 0 ? parte : parte.charAt(0).toUpperCase() + parte.slice(1))
    .join('')
}

function criarValorExemploCampo(campo) {
  if (campo.tipoDado === 'lista') return []
  if (campo.tipoDado === 'objeto') {
    return criarJsonExemploSchema(campo.camposFilhos || campo.campos || [])
  }
  return null
}

function criarJsonExemploSchema(campos) {
  return (campos || []).reduce((json, campo) => {
    if (campo?.chave) {
      json[campo.chave] = criarValorExemploCampo(campo)
    }
    return json
  }, {})
}

function obterSchemaEditorPayload() {
  return JSON.parse(schemaEditor.value || '{}')
}

async function persistirSchema(payload) {
  if (!tipoSelecionado.value?.codigo || !schemaSelecionado.value?.id) return
  loading.schemaAcao = true
  errors.acao = ''
  try {
    const atualizado = await requestJson(
      `/api/tipos-documento/${encodeURIComponent(tipoSelecionado.value.codigo)}/schemas/${schemaSelecionado.value.id}`,
      {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      }
    )
    selecionarSchema(atualizado)
    await carregarSchemas(tipoSelecionado.value.codigo)
    return atualizado
  } catch (error) {
    errors.acao = error.message
    throw error
  } finally {
    loading.schemaAcao = false
  }
}

async function salvarSchema() {
  if (!schemaPermiteEdicao()) return
  await persistirSchema(obterSchemaEditorPayload())
}

async function adicionarCampoSchema() {
  if (!schemaPermiteEdicao()) return

  const chave = normalizarChaveSchema(novoCampoSchema.chave)
  if (!chave) {
    errors.acao = 'Informe uma chave valida para o campo.'
    return
  }

  const payload = obterSchemaEditorPayload()
  payload.campos = Array.isArray(payload.campos) ? payload.campos : []

  const duplicado = payload.campos.some((campo) =>
    normalizarChaveSchema(campo.chave) === chave
  )
  if (duplicado) {
    errors.acao = `O campo ${chave} ja existe no schema.`
    return
  }

  payload.campos.push({
    chave,
    nomeExibicao: novoCampoSchema.nomeExibicao || chave,
    descricao: novoCampoSchema.descricao || null,
    tipoDado: novoCampoSchema.tipoDado,
    obrigatorioSugerido: novoCampoSchema.obrigatorioSugerido,
    obrigatorio: novoCampoSchema.obrigatorioSugerido,
    origemSugestao: 'manual',
    encontradoNoArquivo: false,
    confianca: null,
    aliases: [],
    regraNormalizacao: null,
    regraValidacao: null,
    ordem: payload.campos.length + 1,
    camposFilhos: [],
    itemLista: novoCampoSchema.tipoDado === 'lista'
      ? { tipoDado: 'objeto', camposFilhos: [] }
      : null
  })
  payload.jsonExemplo = criarJsonExemploSchema(payload.campos)

  try {
    await persistirSchema(payload)
    novoCampoSchema.chave = ''
    novoCampoSchema.nomeExibicao = ''
    novoCampoSchema.descricao = ''
    novoCampoSchema.tipoDado = 'texto'
    novoCampoSchema.obrigatorioSugerido = false
  } catch {
    // A mensagem ja foi registrada em errors.acao.
  }
}

async function removerCampoSchema(campoRemovido) {
  if (!schemaPermiteEdicao() || !campoRemovido?.chave) return

  const payload = obterSchemaEditorPayload()
  payload.campos = (payload.campos || [])
    .filter((campo) => campo.chave !== campoRemovido.chave)
    .map((campo, index) => ({ ...campo, ordem: index + 1 }))
  payload.jsonExemplo = criarJsonExemploSchema(payload.campos)

  try {
    await persistirSchema(payload)
  } catch {
    // A mensagem ja foi registrada em errors.acao.
  }
}

async function validarSchema() {
  if (!tipoSelecionado.value?.codigo || !schemaSelecionado.value?.id) return
  loading.schemaAcao = true
  errors.acao = ''
  try {
    validacaoSchema.value = await requestJson(
      `/api/tipos-documento/${encodeURIComponent(tipoSelecionado.value.codigo)}/schemas/${schemaSelecionado.value.id}/validar`,
      { method: 'POST' }
    )
  } catch (error) {
    errors.acao = error.message
  } finally {
    loading.schemaAcao = false
  }
}

async function carregarSchemas(tipoDocumento) {
  if (!tipoDocumento) return
  loading.schemas = true
  errors.tipos = ''
  try {
    schemas.value = await requestJson(`/api/tipos-documento/${encodeURIComponent(tipoDocumento)}/schemas`)
    selecionarSchema(schemas.value[0] || null)
  } catch (error) {
    schemas.value = []
    selecionarSchema(null)
    errors.tipos = error.message
  } finally {
    loading.schemas = false
  }
}

async function sugerirSchema() {
  if (!formSugestao.tipoDocumento || !formSugestao.arquivo) {
    errors.sugestao = 'Informe o tipo de documento e selecione um arquivo.'
    return
  }

  loading.sugestao = true
  errors.sugestao = ''
  sugestao.value = null
  const data = new FormData()
  data.append('TipoDocumento', formSugestao.tipoDocumento)
  data.append('Arquivo', formSugestao.arquivo)
  if (formSugestao.descricaoTipoDocumento) {
    data.append('DescricaoTipoDocumento', formSugestao.descricaoTipoDocumento)
  }
  if (formSugestao.pais) data.append('Pais', formSugestao.pais)
  if (formSugestao.idioma) data.append('Idioma', formSugestao.idioma)

  try {
    sugestao.value = await requestJson('/api/tipos-documento/sugerir-schema', {
      method: 'POST',
      body: data
    })
    await carregarTipos()
  } catch (error) {
    errors.sugestao = error.message
  } finally {
    loading.sugestao = false
  }
}

function onArquivoChange(event) {
  formSugestao.arquivo = event.target.files?.[0] || null
}

function formatCurrency(value) {
  if (value === null || value === undefined || value === '') return '-'
  return new Intl.NumberFormat('pt-BR', {
    style: 'currency',
    currency: 'USD'
  }).format(Number(value))
}

function formatDate(value) {
  if (!value) return '-'
  return new Intl.DateTimeFormat('pt-BR', {
    dateStyle: 'short',
    timeStyle: 'short'
  }).format(new Date(value))
}

function stringify(value) {
  return JSON.stringify(value ?? {}, null, 2)
}

onMounted(async () => {
  await Promise.all([carregarModelos(), carregarTipos()])
})
</script>

<template>
  <main class="app-shell">
    <aside class="sidebar">
      <div class="brand">
        <div class="brand-mark">ED</div>
        <div>
          <strong>Extrator Documentos</strong>
          <span>Operacao</span>
        </div>
      </div>

      <nav class="nav">
        <button :class="{ active: activeTab === 'openrouter' }" @click="activeTab = 'openrouter'">
          OpenRouter
        </button>
        <button :class="{ active: activeTab === 'tipos' }" @click="activeTab = 'tipos'">
          TiposDocumento
        </button>
      </nav>
    </aside>

    <section class="workspace">
      <header class="topbar">
        <div>
          <h1>{{ activeTab === 'openrouter' ? 'OpenRouter' : 'TiposDocumento' }}</h1>
          <p>{{ activeTab === 'openrouter'
            ? 'Modelos, creditos e sincronizacao do catalogo.'
            : 'Tipos documentais, schemas e sugestao por arquivo exemplo.' }}</p>
        </div>
      </header>

      <section v-if="activeTab === 'openrouter'" class="content-grid">
        <div class="toolbar">
          <button class="primary" :disabled="loading.creditos" @click="carregarCreditos">
            {{ loading.creditos ? 'Consultando...' : 'Consultar creditos' }}
          </button>
          <button :disabled="loading.modelos" @click="carregarModelos">
            {{ loading.modelos ? 'Atualizando...' : 'Recarregar modelos' }}
          </button>
          <button :disabled="loading.sincronizar" @click="sincronizarModelos">
            {{ loading.sincronizar ? 'Sincronizando...' : 'Sincronizar catalogo' }}
          </button>
          <label class="check">
            <input v-model="apenasCompativeis" type="checkbox" @change="carregarModelos" />
            compativeis
          </label>
        </div>

        <p v-if="errors.openrouter" class="alert">{{ errors.openrouter }}</p>

        <div class="metrics">
          <article>
            <span>Modelos</span>
            <strong>{{ modelosResumo.total }}</strong>
          </article>
          <article>
            <span>Ativos</span>
            <strong>{{ modelosResumo.ativos }}</strong>
          </article>
          <article>
            <span>Imagem</span>
            <strong>{{ modelosResumo.imagem }}</strong>
          </article>
          <article>
            <span>JSON</span>
            <strong>{{ modelosResumo.json }}</strong>
          </article>
        </div>

        <div v-if="creditos" class="panel">
          <h2>Conta</h2>
          <div class="kv-grid">
            <div v-for="(value, key) in creditos" :key="key">
              <span>{{ key }}</span>
              <strong>{{ typeof value === 'number' ? formatCurrency(value) : value }}</strong>
            </div>
          </div>
        </div>

        <div v-if="sincronizacao" class="panel compact">
          <h2>Ultima sincronizacao</h2>
          <pre>{{ stringify(sincronizacao) }}</pre>
        </div>

        <div class="panel">
          <h2>Modelos</h2>
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Modelo</th>
                  <th>Contexto</th>
                  <th>Entrada</th>
                  <th>Saida</th>
                  <th>Imagem</th>
                  <th>JSON</th>
                  <th>Atualizado</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="modelo in modelos" :key="modelo.id || modelo.modeloId">
                  <td>
                    <strong>{{ modelo.nome || modelo.id || modelo.modeloId }}</strong>
                    <span>{{ modelo.id || modelo.modeloId }}</span>
                  </td>
                  <td>{{ modelo.contextoTokens || modelo.contextLength || '-' }}</td>
                  <td>{{ modelo.precoEntradaPorMilhaoTokens ?? modelo.custoEntrada ?? '-' }}</td>
                  <td>{{ modelo.precoSaidaPorMilhaoTokens ?? modelo.custoSaida ?? '-' }}</td>
                  <td>{{ modelo.aceitaImagem || modelo.suportaImagem ? 'sim' : 'nao' }}</td>
                  <td>{{ modelo.suportaJson || modelo.suportaStructuredOutputs ? 'sim' : 'nao' }}</td>
                  <td>{{ formatDate(modelo.atualizadoEm || modelo.ultimoVistoEm) }}</td>
                </tr>
                <tr v-if="!modelos.length">
                  <td colspan="7" class="empty">Nenhum modelo carregado.</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </section>

      <section v-else class="content-grid">
        <div class="subnav">
          <button
            :class="{ active: tiposView === 'visualizacao' }"
            @click="tiposView = 'visualizacao'"
          >
            Visualizacao
          </button>
          <button
            :class="{ active: tiposView === 'carga' }"
            @click="tiposView = 'carga'"
          >
            Carga
          </button>
          <button
            :class="{ active: tiposView === 'publicacao' }"
            @click="tiposView = 'publicacao'"
          >
            Confirmar e publicar
          </button>
        </div>

        <section v-if="tiposView === 'visualizacao'" class="content-grid">
          <div class="panel">
            <div class="panel-title">
              <h2>Tipos importados</h2>
              <button :disabled="loading.tipos" @click="carregarTipos">
                {{ loading.tipos ? 'Atualizando...' : 'Recarregar' }}
              </button>
            </div>
            <p v-if="errors.tipos" class="alert">{{ errors.tipos }}</p>
          <div class="metrics inline">
            <article>
              <span>Total</span>
              <strong>{{ tiposResumo.total }}</strong>
            </article>
            <article>
              <span>Ativos</span>
              <strong>{{ tiposResumo.ativos }}</strong>
            </article>
            <article>
              <span>Confirmados</span>
              <strong>{{ tiposResumo.confirmados }}</strong>
            </article>
          </div>
          <label class="search-field">
            Buscar tipo importado
            <input v-model.trim="filtroTipos" placeholder="cnh, contrato, passaporte" />
          </label>
            <div class="panel-title list-title">
              <h2>Lista</h2>
              <span class="muted-text">{{ tiposFiltrados.length }} de {{ tipos.length }}</span>
            </div>
          <div class="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Codigo</th>
                  <th>Nome</th>
                  <th>Status</th>
                  <th>Confirmacao</th>
                  <th>Atualizado</th>
                  <th>Acao</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="tipo in tiposFiltrados" :key="`linha-${tipo.id}`">
                  <td>
                    <strong>{{ tipo.codigo }}</strong>
                    <span>{{ tipo.id }}</span>
                  </td>
                  <td>{{ tipo.nome }}</td>
                  <td>
                    <span :class="['badge', tipo.ativo ? 'ok' : 'muted']">
                      {{ tipo.ativo ? 'Ativo' : 'Inativo' }}
                    </span>
                  </td>
                  <td>
                    <span :class="['badge', tipo.confirmado ? 'ok' : 'warn']">
                      {{ tipo.confirmado ? 'Confirmado' : 'Pendente' }}
                    </span>
                  </td>
                  <td>{{ formatDate(tipo.atualizadoEm || tipo.criadoEm) }}</td>
                  <td>
                    <button @click="selecionarTipo(tipo); tiposView = 'publicacao'">Selecionar</button>
                  </td>
                </tr>
                <tr v-if="!tiposFiltrados.length">
                  <td colspan="6" class="empty">Nenhum tipo importado encontrado.</td>
                </tr>
              </tbody>
            </table>
          </div>
          </div>
        </section>

        <section v-else-if="tiposView === 'carga'" class="content-grid two-columns">
          <div class="panel">
            <h2>Carga de schema</h2>
            <form class="schema-form" @submit.prevent="sugerirSchema">
              <label>
                TipoDocumento
                <input v-model.trim="formSugestao.tipoDocumento" placeholder="cnh" />
              </label>
              <label>
                Descricao
                <input v-model.trim="formSugestao.descricaoTipoDocumento" placeholder="Documento de exemplo" />
              </label>
              <div class="form-row">
                <label>
                  Pais
                  <input v-model.trim="formSugestao.pais" maxlength="3" />
                </label>
                <label>
                  Idioma
                  <input v-model.trim="formSugestao.idioma" maxlength="5" />
                </label>
              </div>
              <label>
                Arquivo
                <input type="file" @change="onArquivoChange" />
              </label>
              <button class="primary" :disabled="loading.sugestao">
                {{ loading.sugestao ? 'Gerando rascunho...' : 'Sugerir schema' }}
              </button>
            </form>
            <p v-if="errors.sugestao" class="alert">{{ errors.sugestao }}</p>
          </div>

          <div v-if="sugestao" class="panel">
            <h2>Schema sugerido</h2>
            <div class="suggestion-summary">
              <span>{{ sugestao.tipoDocumento?.codigo }}</span>
              <span>{{ sugestao.schema?.status }}</span>
              <span>{{ sugestao.analiseArquivo?.quantidadeTotalCamposSugeridos || 0 }} campos</span>
            </div>
            <div class="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>Chave</th>
                    <th>Tipo</th>
                    <th>Origem</th>
                    <th>Arquivo</th>
                    <th>Obrigatorio</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="campo in sugestao.schema?.campos || []" :key="campo.chave">
                    <td>
                      <strong>{{ campo.chave }}</strong>
                      <span>{{ campo.nomeExibicao }}</span>
                    </td>
                    <td>{{ campo.tipoDado }}</td>
                    <td>{{ campo.origemSugestao }}</td>
                    <td>{{ campo.encontradoNoArquivo ? 'sim' : 'nao' }}</td>
                    <td>{{ campo.obrigatorioSugerido ? 'sim' : 'nao' }}</td>
                  </tr>
                </tbody>
              </table>
            </div>
            <pre>{{ stringify(sugestao.schema?.jsonExemplo) }}</pre>
          </div>

          <div v-else class="panel">
            <h2>Resultado</h2>
            <p class="empty">Envie um arquivo de exemplo para criar um rascunho de schema.</p>
          </div>
        </section>

        <section v-else class="content-grid two-columns">
          <div class="panel">
            <div class="panel-title">
              <h2>Selecionar tipo</h2>
              <button :disabled="loading.tipos" @click="carregarTipos">
                {{ loading.tipos ? 'Atualizando...' : 'Recarregar' }}
              </button>
            </div>
            <label class="search-field">
              Buscar tipo
              <input v-model.trim="filtroTipos" placeholder="codigo ou nome" />
            </label>
            <div class="type-list">
              <button
                v-for="tipo in tiposFiltrados"
                :key="tipo.id"
                :class="{ selected: tipoSelecionado?.id === tipo.id }"
                @click="selecionarTipo(tipo)"
              >
                <strong>{{ tipo.codigo }}</strong>
                <span>{{ tipo.nome }}</span>
              </button>
              <p v-if="!tiposFiltrados.length" class="empty">Nenhum tipo importado encontrado.</p>
            </div>
          </div>

          <div class="panel">
            <h2>Confirmacao do tipo</h2>
            <div v-if="tipoSelecionado" class="selected-record">
              <div>
                <strong>{{ tipoSelecionado.codigo }}</strong>
                <span>{{ tipoSelecionado.nome }}</span>
              </div>
              <div class="badges">
                <span :class="['badge', tipoSelecionado.ativo ? 'ok' : 'muted']">
                  {{ tipoSelecionado.ativo ? 'Ativo' : 'Inativo' }}
                </span>
                <span :class="['badge', tipoSelecionado.confirmado ? 'ok' : 'warn']">
                  {{ tipoSelecionado.confirmado ? 'Confirmado' : 'Pendente' }}
                </span>
              </div>
            </div>
            <div v-if="tipoSelecionado" class="action-row">
              <button
                class="primary"
                :disabled="loading.tipoAcao || tipoSelecionado.ativo"
                @click="executarAcaoTipo('ativar')"
              >
                Ativar tipo
              </button>
              <button
                :disabled="loading.tipoAcao || !tipoSelecionado.ativo"
                @click="executarAcaoTipo('desativar')"
              >
                Desativar
              </button>
              <button
                :disabled="loading.tipoAcao || tipoSelecionado.confirmado"
                @click="executarAcaoTipo('confirmar')"
              >
                Confirmar
              </button>
            </div>
            <p v-if="!tipoSelecionado" class="empty">Selecione um tipo importado.</p>
            <p v-if="errors.acao" class="alert">{{ errors.acao }}</p>
            <p v-if="errors.tipos" class="alert">{{ errors.tipos }}</p>
          </div>

          <div class="panel wide">
          <div class="panel-title">
            <h2>Schemas de {{ tipoSelecionado?.codigo || '-' }}</h2>
            <div v-if="schemaSelecionado" class="action-row compact-actions">
              <button
                class="primary"
                :disabled="loading.schemaAcao || schemaSelecionado.status === 'Ativo' || schemaSelecionado.ativo"
                @click="executarAcaoSchema('publicar')"
              >
                Publicar schema
              </button>
              <button
                :disabled="loading.schemaAcao || schemaSelecionado.status === 'Ativo' || schemaSelecionado.ativo"
                @click="executarAcaoSchema('ativar')"
              >
                Ativar direto
              </button>
            </div>
          </div>
          <div class="schema-tabs">
            <button
              v-for="schema in schemas"
              :key="schema.id"
              :class="{ selected: schemaSelecionado?.id === schema.id }"
              @click="selecionarSchema(schema)"
            >
              {{ schema.versao }} · {{ schema.status ?? (schema.ativo ? 'Ativo' : 'Inativo') }}
            </button>
          </div>
          <div v-if="schemaSelecionado" class="schema-editor-grid">
            <div>
              <div class="panel-title editor-title">
                <h2>Campos do schema</h2>
                <span :class="['badge', schemaPermiteEdicao() ? 'warn' : 'muted']">
                  {{ schemaPermiteEdicao() ? 'Rascunho editavel' : 'Somente leitura' }}
                </span>
              </div>
              <form v-if="schemaPermiteEdicao()" class="field-form" @submit.prevent="adicionarCampoSchema">
                <label>
                  Chave
                  <input v-model.trim="novoCampoSchema.chave" placeholder="numeroContrato" />
                </label>
                <label>
                  Nome exibicao
                  <input v-model.trim="novoCampoSchema.nomeExibicao" placeholder="Numero do contrato" />
                </label>
                <label>
                  Tipo
                  <select v-model="novoCampoSchema.tipoDado">
                    <option v-for="tipo in tiposDadoSchema" :key="tipo" :value="tipo">
                      {{ tipo }}
                    </option>
                  </select>
                </label>
                <label class="check field-check">
                  <input v-model="novoCampoSchema.obrigatorioSugerido" type="checkbox" />
                  obrigatorio
                </label>
                <label class="field-description">
                  Descricao
                  <input v-model.trim="novoCampoSchema.descricao" placeholder="Descricao curta do campo" />
                </label>
                <button class="primary" :disabled="loading.schemaAcao">Adicionar e registrar</button>
              </form>
              <div class="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Chave</th>
                      <th>Tipo</th>
                      <th>Obrigatorio</th>
                      <th>Origem</th>
                      <th>Acao</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="campo in schemaSelecionado.campos || []" :key="campo.chave">
                      <td>
                        <strong>{{ campo.chave }}</strong>
                        <span>{{ campo.nomeExibicao }}</span>
                      </td>
                      <td>{{ campo.tipoDado }}</td>
                      <td>{{ campo.obrigatorioSugerido ? 'sim' : 'nao' }}</td>
                      <td>{{ campo.origemSugestao || '-' }}</td>
                      <td>
                        <button
                          class="danger"
                          :disabled="loading.schemaAcao || !schemaPermiteEdicao()"
                          @click="removerCampoSchema(campo)"
                        >
                          Remover
                        </button>
                      </td>
                    </tr>
                    <tr v-if="!(schemaSelecionado.campos || []).length">
                      <td colspan="5" class="empty">Schema sem campos materializados.</td>
                    </tr>
                  </tbody>
                </table>
              </div>
            </div>
            <div>
              <div class="panel-title editor-title">
                <h2>Editor JSON</h2>
                <div class="action-row compact-actions">
                  <button :disabled="loading.schemaAcao" @click="validarSchema">Validar</button>
                  <button
                    class="primary"
                    :disabled="loading.schemaAcao || schemaSelecionado.status !== 'Rascunho'"
                    @click="salvarSchema"
                  >
                    Salvar rascunho
                  </button>
                </div>
              </div>
              <textarea
                v-model="schemaEditor"
                class="schema-editor"
                :readonly="schemaSelecionado.status !== 'Rascunho'"
                spellcheck="false"
              />
              <div v-if="validacaoSchema" class="validation-box">
                <strong>{{ validacaoSchema.valido ? 'Schema valido' : 'Schema invalido' }}</strong>
                <ul v-if="validacaoSchema.erros?.length">
                  <li v-for="erro in validacaoSchema.erros" :key="erro">{{ erro }}</li>
                </ul>
                <ul v-if="validacaoSchema.alertas?.length">
                  <li v-for="alerta in validacaoSchema.alertas" :key="alerta">{{ alerta }}</li>
                </ul>
              </div>
            </div>
          </div>
          <p v-else class="empty">Selecione um tipo para visualizar schemas.</p>
          </div>
        </section>
      </section>
    </section>
  </main>
</template>
