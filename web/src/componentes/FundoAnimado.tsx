/**
 * Fundo animado do site: brilho nas cores da logo, brasas subindo e os símbolos de mana do Magic flutuando bem de leve
 * (as imagens vêm do CDN do Scryfall, o mesmo das artes dos comandantes).
 * Fica atrás de tudo (não recebe clique). Quem pediu menos movimento no celular/computador (prefers-reduced-motion)
 * vê o fundo parado. Os valores são fixos (e não aleatórios) para o desenho ser igual a cada visita.
 */

/** Brasas: posição horizontal (%), tamanho (px), duração e atraso (s). */
const brasas: [number, number, number, number][] = [
  [4, 3, 14, 0], [11, 2, 18, 6], [17, 4, 16, 3], [23, 2, 20, 9], [29, 3, 15, 1], [36, 2, 19, 11], [42, 3, 17, 5],
  [48, 2, 21, 2], [54, 4, 16, 8], [60, 2, 18, 12], [66, 3, 15, 4], [72, 2, 20, 7], [78, 3, 17, 10], [84, 2, 19, 1],
  [90, 4, 16, 6], [96, 2, 18, 3], [8, 2, 22, 13], [33, 3, 23, 15], [57, 2, 24, 14], [81, 3, 21, 16],
]

/** Símbolos flutuando: mana (W branco, U azul, B preto, R vermelho, G verde, C incolor), posição (%), tamanho (px), duração e atraso (s). */
const simbolos: [string, number, number, number, number, number][] = [
  ['W', 7, 16, 60, 26, 0], ['U', 86, 10, 50, 30, 4], ['B', 72, 60, 68, 34, 8], ['R', 12, 68, 56, 28, 2], ['G', 46, 36, 46, 32, 6],
  ['C', 92, 82, 40, 29, 10], ['R', 58, 88, 36, 31, 12], ['G', 28, 44, 34, 27, 9], ['U', 38, 8, 32, 33, 14], ['B', 3, 92, 38, 30, 5],
]

const enderecoSimbolo = (mana: string) => `https://svgs.scryfall.io/card-symbols/${mana}.svg`

export function FundoAnimado() {
  return (
    <div className="fundo-animado pointer-events-none fixed inset-0 -z-10 overflow-hidden" aria-hidden>
      {/* brilho laranja vindo de baixo e um toque vermelho no canto, como a faixa da logo */}
      <div className="absolute inset-0 bg-[radial-gradient(ellipse_70%_45%_at_50%_100%,rgba(242,101,34,.16),transparent_70%),radial-gradient(ellipse_45%_35%_at_0%_0%,rgba(215,31,38,.10),transparent_70%)]" />
      {simbolos.map(([mana, x, y, tamanho, duracao, atraso], indice) => (
        <img key={indice} src={enderecoSimbolo(mana)} alt="" loading="lazy" draggable={false} className="fundo-simbolo absolute"
          style={{ left: `${x}%`, top: `${y}%`, width: tamanho, height: tamanho, animationDuration: `${duracao}s`, animationDelay: `-${atraso}s` }} />
      ))}
      {brasas.map(([x, tamanho, duracao, atraso], indice) => (
        <span key={indice} className="fundo-brasa absolute rounded-full"
          style={{ left: `${x}%`, width: tamanho, height: tamanho, animationDuration: `${duracao}s`, animationDelay: `-${atraso}s` }} />
      ))}
    </div>
  )
}
