import { Camera, ImagePlus, Trash2 } from 'lucide-react'
import { useEffect, useRef, useState } from 'react'
import { useEnviarFoto, useRemoverFoto } from '../api/consultas'
import { formatarTamanho, reduzirFoto } from '../utilitarios/fotos'
import { ErroEmLinha, Janela } from './comum'

/**
 * Foto de destaque com legenda (etapa ou temporada), na página pública e no Hall da Fama.
 * Mostra a foto inteira no formato original (em pé, tipo story, ou deitada): sem cortar, sem esticar e sem zoom.
 * As sobras dos lados ganham um fundo desfocado da própria foto.
 */
export function FotoDestaque({ url, legenda, className = '' }: { url: string; legenda: string | null; className?: string }) {
  return (
    <figure className={`overflow-hidden rounded-2xl border border-zinc-800 bg-zinc-900 ${className}`}>
      <a href={url} target="_blank" rel="noreferrer noopener" title="Abrir a foto inteira" className="relative isolate block">
        <FundoDesfocado url={url} />
        <img src={url} alt={legenda ?? 'Foto do top'} loading="lazy" className="mx-auto block h-auto max-h-[75vh] w-auto max-w-full" />
      </a>
      {legenda && <figcaption className="px-3 py-2 text-sm text-zinc-300">{legenda}</figcaption>}
    </figure>
  )
}

/** A própria foto, desfocada e escurecida, preenchendo o fundo atrás da foto inteira. */
export function FundoDesfocado({ url }: { url: string }) {
  return (
    <>
      <img src={url} alt="" aria-hidden loading="lazy" className="absolute inset-0 -z-20 size-full scale-110 object-cover blur-2xl" />
      <div className="absolute inset-0 -z-10 bg-black/45" />
    </>
  )
}

/**
 * Janela do admin para enviar a foto do top (etapa ou temporada): tirar foto com a câmera do celular ou escolher da
 * galeria, legenda, trocar e remover. A foto é reduzida no navegador antes de subir.
 */
export function JanelaFoto({ tipo, id, titulo, urlAtual, legendaAtual, aoFechar }: {
  tipo: 'etapas' | 'temporadas'
  id: number
  titulo: string
  urlAtual: string | null
  legendaAtual: string | null
  aoFechar: () => void
}) {
  const enviar = useEnviarFoto()
  const remover = useRemoverFoto()
  const [legenda, definirLegenda] = useState(legendaAtual ?? '')
  const [foto, definirFoto] = useState<{ blob: Blob; url: string; tamanhoOriginal: number } | null>(null)
  const [preparando, definirPreparando] = useState(false)
  const [erroFoto, definirErroFoto] = useState<string | null>(null)
  const camera = useRef<HTMLInputElement>(null)
  const galeria = useRef<HTMLInputElement>(null)

  // Libera a prévia da memória ao trocar de foto ou fechar.
  useEffect(() => () => { if (foto) URL.revokeObjectURL(foto.url) }, [foto])

  async function escolher(arquivo: File | undefined) {
    if (!arquivo) return
    definirErroFoto(null)
    definirPreparando(true)
    try {
      const blob = await reduzirFoto(arquivo)
      definirFoto({ blob, url: URL.createObjectURL(blob), tamanhoOriginal: arquivo.size })
    } catch (erro) {
      definirErroFoto(erro instanceof Error ? erro.message : 'Não foi possível abrir a foto.')
    } finally {
      definirPreparando(false)
    }
  }

  async function salvar() {
    await enviar.mutateAsync({ tipo, id, arquivo: foto?.blob ?? null, legenda })
    aoFechar()
  }

  async function confirmarRemocao() {
    if (!confirm('Remover a foto?')) return
    await remover.mutateAsync({ tipo, id })
    aoFechar()
  }

  const previa = foto?.url ?? urlAtual
  const ocupado = enviar.isPending || remover.isPending || preparando

  return (
    <Janela aberta aoFechar={aoFechar} titulo={titulo}>
      <div className="space-y-4">
        {previa
          ? <img src={previa} alt="Prévia da foto" className="max-h-80 w-full rounded-xl border border-zinc-800 object-contain" />
          : <div className="grid h-40 place-items-center rounded-xl border border-dashed border-zinc-700 text-sm text-zinc-500">Nenhuma foto ainda</div>}
        {foto && (
          <p className="text-xs text-zinc-500">
            Foto reduzida de {formatarTamanho(foto.tamanhoOriginal)} para {formatarTamanho(foto.blob.size)} antes de enviar.
          </p>
        )}

        {/* capture="environment" abre direto a câmera traseira no celular; no computador vira um seletor de arquivo. */}
        <input ref={camera} type="file" accept="image/*" capture="environment" className="hidden" onChange={(e) => escolher(e.target.files?.[0])} />
        <input ref={galeria} type="file" accept="image/jpeg,image/png,image/webp,image/*" className="hidden" onChange={(e) => escolher(e.target.files?.[0])} />
        <div className="grid grid-cols-2 gap-2">
          <button type="button" className="botao-secundario" disabled={ocupado} onClick={() => camera.current?.click()}>
            <Camera className="size-4" aria-hidden /> Tirar foto
          </button>
          <button type="button" className="botao-secundario" disabled={ocupado} onClick={() => galeria.current?.click()}>
            <ImagePlus className="size-4" aria-hidden /> Escolher foto
          </button>
        </div>

        <label className="block">
          <span className="rotulo">Legenda (opcional)</span>
          <input value={legenda} maxLength={200} onChange={(e) => definirLegenda(e.target.value)} className="w-full"
            placeholder="Ex.: Top 3 da etapa 2 — Pedro, João e Miguel" />
        </label>

        {preparando && <p className="text-sm text-zinc-400">Preparando a foto…</p>}
        {erroFoto && <p className="text-sm text-red-300">{erroFoto}</p>}
        <ErroEmLinha erro={enviar.error ?? remover.error} />

        <div className="flex flex-wrap justify-end gap-2">
          {urlAtual && (
            <button type="button" className="botao-discreto mr-auto text-red-300" disabled={ocupado} onClick={confirmarRemocao}>
              <Trash2 className="size-4" aria-hidden /> Remover
            </button>
          )}
          <button type="button" className="botao-secundario" onClick={aoFechar}>Cancelar</button>
          <button type="button" className="botao-primario" disabled={ocupado || (!foto && !urlAtual)} onClick={salvar}>
            {enviar.isPending ? 'Enviando…' : 'Salvar'}
          </button>
        </div>
      </div>
    </Janela>
  )
}
