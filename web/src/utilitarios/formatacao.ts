import type { CriterioDesempate, ModoPontuacao, SituacaoDeck, SituacaoRodada } from '../api/tipos'

export const rotuloSituacaoRodada: Record<SituacaoRodada, string> = {
  Rascunho: 'Rascunho',
  Sorteada: 'Sorteada',
  EmAndamento: 'Em andamento',
  Encerrada: 'Encerrada',
}

export const rotuloSituacaoDeck: Record<SituacaoDeck, string> = {
  Pendente: 'Pendente',
  Aprovado: 'Aprovado',
  Reprovado: 'Reprovado',
}

/** Cores da situação do deck (verde aprovado, laranja pendente, vermelho reprovado): selo do admin e selo da lista na página do jogador. */
export const estiloSituacaoDeck: Record<SituacaoDeck, string> = {
  Pendente: 'bg-amber-950 text-amber-300 ring-1 ring-amber-800/70',
  Aprovado: 'bg-emerald-950 text-emerald-300 ring-1 ring-emerald-800/70',
  Reprovado: 'bg-red-950 text-red-300 ring-1 ring-red-800/70',
}

export const rotuloCriterioDesempate: Record<CriterioDesempate, string> = {
  Vitorias: 'Mais vitórias',
  MenosPartidas: 'Menos partidas jogadas',
  MaisPartidas: 'Mais partidas jogadas',
  Empates: 'Mais empates',
  PercentualVitoria: 'Maior % de vitória',
}

/** "2026-11-10" → "10/11/2026" (data sem hora, sem mudar o dia por causa do fuso). */
export function formatarData(iso: string | null | undefined): string {
  if (!iso) return ''
  const [ano, mes, dia] = iso.slice(0, 10).split('-')
  return `${dia}/${mes}/${ano}`
}

export function formatarDataHora(iso: string | null | undefined): string {
  if (!iso) return ''
  return new Date(iso).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}

/** 0.333 → "33%"; nulo → "–". */
export function formatarPercentual(valor: number | null | undefined): string {
  if (valor === null || valor === undefined) return '–'
  return `${Math.round(valor * 100)}%`
}

/** Segundos → "59:18" ou "1:05:00". */
export function formatarRelogio(totalSegundos: number): string {
  const s = Math.max(0, Math.floor(totalSegundos))
  const horas = Math.floor(s / 3600)
  const minutos = Math.floor((s % 3600) / 60)
  const segundos = s % 60
  const mm = String(minutos).padStart(horas ? 2 : 1, '0')
  const ss = String(segundos).padStart(2, '0')
  return horas ? `${horas}:${mm}:${ss}` : `${mm}:${ss}`
}

/** Hoje no fuso do navegador, no formato aaaa-mm-dd. */
export function hojeIso(): string {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

/** Chave de busca que ignora acento e maiúscula. */
export function chaveBusca(texto: string): string {
  return texto.normalize('NFD').replace(/\p{M}/gu, '').toLowerCase().trim()
}

export function nomeComApelido(nome: string, apelido: string | null | undefined): string {
  return apelido ? `${nome} (${apelido})` : nome
}

export const rotuloModoPontuacao: Record<ModoPontuacao, string> = {
  ColocacaoNaEtapa: 'Colocação na etapa',
  SomaDasRodadas: 'Soma das rodadas',
}

/** Explicação dos modos para o lojista escolher. */
export const explicacaoModoPontuacao: Record<ModoPontuacao, string> = {
  ColocacaoNaEtapa:
    'Cada etapa tem o próprio ranking com os pontos das rodadas (vitória, empate, derrota, bye). No fim do dia, a colocação ' +
    'vira pontos na temporada pela tabela abaixo (ex.: 1º 5, 2º 4, 3º 3, 4º 2, demais 1). Empatados ganham os pontos da melhor ' +
    'posição. Permite formar a mesa final do top 4, que soma do mesmo jeito.',
  SomaDasRodadas:
    'Os pontos de cada rodada (vitória, empate, derrota/presença e bye) somam direto no ranking da temporada. ' +
    'Quem joga mais etapas acumula mais. Não tem mesa final.',
}

/** Data/hora da API (UTC) → valor de um <input type="datetime-local"> no horário do navegador. */
export function paraCampoDataHora(iso: string | null | undefined): string {
  if (!iso) return ''
  const d = new Date(iso)
  const doisDigitos = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${doisDigitos(d.getMonth() + 1)}-${doisDigitos(d.getDate())}T${doisDigitos(d.getHours())}:${doisDigitos(d.getMinutes())}`
}

/** Valor de um <input type="datetime-local"> (horário do navegador) → ISO com fuso, para a API. */
export function deCampoDataHora(valor: string): string | null {
  return valor ? new Date(valor).toISOString() : null
}

/** "2026-11-28T17:00:00Z" → "28/11/2026 · 14h" (ou "14h30"). */
export function formatarDiaEHora(iso: string): string {
  const d = new Date(iso)
  const dia = d.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' })
  const minutos = d.getMinutes()
  return `${dia} · ${d.getHours()}h${minutos ? String(minutos).padStart(2, '0') : ''}`
}
