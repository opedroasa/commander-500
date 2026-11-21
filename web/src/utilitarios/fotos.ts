// Fotos tiradas no celular têm vários MB. Antes de enviar, o site reduz a foto para no máximo 1600 px no lado maior
// e salva em JPEG (~200–400 KB), para caber folgado no armazenamento grátis do Supabase (1 GB).

const LadoMaximo = 1600
const Qualidade = 0.82

/** Reduz a foto e devolve um JPEG. Fotos já pequenas só são convertidas. */
export async function reduzirFoto(arquivo: File): Promise<Blob> {
  // createImageBitmap já respeita a orientação da câmera (foto em pé continua em pé).
  const imagem = await createImageBitmap(arquivo, { imageOrientation: 'from-image' })
  const escala = Math.min(1, LadoMaximo / Math.max(imagem.width, imagem.height))
  const largura = Math.round(imagem.width * escala)
  const altura = Math.round(imagem.height * escala)

  const tela = document.createElement('canvas')
  tela.width = largura
  tela.height = altura
  const contexto = tela.getContext('2d')
  if (!contexto) throw new Error('Não foi possível preparar a foto neste navegador.')
  contexto.drawImage(imagem, 0, 0, largura, altura)
  imagem.close()

  return new Promise((resolver, rejeitar) =>
    tela.toBlob((blob) => (blob ? resolver(blob) : rejeitar(new Error('Não foi possível preparar a foto.'))), 'image/jpeg', Qualidade))
}

/** "1,2 MB" / "350 KB" */
export function formatarTamanho(bytes: number): string {
  return bytes >= 1024 * 1024 ? `${(bytes / 1024 / 1024).toLocaleString('pt-BR', { maximumFractionDigits: 1 })} MB` : `${Math.round(bytes / 1024)} KB`
}
