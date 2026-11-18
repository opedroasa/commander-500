import { useState, type FormEvent } from 'react'
import { useJogadores, useSalvarDeck } from '../api/consultas'
import type { Deck } from '../api/tipos'
import { nomeComApelido } from '../utilitarios/formatacao'
import { Campo, ErroEmLinha, Janela } from './comum'
import { SeletorComandante, type ComandanteEscolhido } from './SeletorComandante'

/**
 * Novo / editar deck. O comandante vem do Scryfall; o nome do deck é opcional e já vem sugerido com o nome do(s)
 * comandante(s) até o usuário digitar outro. As estatísticas usam sempre os comandantes, não o nome.
 */
export function JanelaDeck({ aberta, deck, idJogador, aoFechar }: {
  aberta: boolean
  /** Deck em edição (null = novo). */
  deck: Deck | null
  /** Dono fixo (ex.: aberto pelo perfil do jogador). */
  idJogador?: number
  aoFechar: () => void
}) {
  const jogadores = useJogadores()
  const salvar = useSalvarDeck()
  const [dono, definirDono] = useState(deck?.idJogador ?? idJogador ?? 0)
  const paraEscolhido = (posicao: number): ComandanteEscolhido | null => {
    const c = deck?.comandantes[posicao]
    return c ? { idScryfall: null, nome: c.nome, urlArte: c.urlArte, identidadeCor: c.identidadeCor } : null
  }
  const [comandante1, definirComandante1] = useState<ComandanteEscolhido | null>(paraEscolhido(0))
  const [comandante2, definirComandante2] = useState<ComandanteEscolhido | null>(paraEscolhido(1))
  const [temParceiro, definirTemParceiro] = useState((deck?.comandantes.length ?? 1) > 1)
  const [nome, definirNome] = useState(deck?.nome ?? '')
  const [nomeDigitado, definirNomeDigitado] = useState(deck !== null)
  const [urlLista, definirUrlLista] = useState(deck?.urlLista ?? '')

  const escolhidos = () => [comandante1, temParceiro ? comandante2 : null].filter((c): c is ComandanteEscolhido => !!c?.nome.trim())
  const nomeSugerido = escolhidos().map((c) => c.nome.trim()).sort().join(' + ')
  const nomeExibido = nomeDigitado ? nome : nomeSugerido

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    const comandantes = escolhidos().map((c) => ({ idScryfall: c.idScryfall, nome: c.nome.trim() }))
    await salvar.mutateAsync({
      id: deck?.id,
      dados: { idJogador: dono, nome: nomeExibido.trim() || null, urlLista: urlLista.trim() || null, comandantes },
    })
    aoFechar()
  }

  const donoFixo = idJogador != null && deck === null
  const jogadorDono = jogadores.data?.find((j) => j.id === dono)

  return (
    <Janela aberta={aberta} aoFechar={aoFechar} titulo={deck ? 'Editar deck' : 'Novo deck'}>
      <form onSubmit={enviar} className="space-y-4">
        {donoFixo ? (
          <p className="text-sm text-zinc-300">Jogador: <b>{jogadorDono ? nomeComApelido(jogadorDono.nome, jogadorDono.apelido) : '…'}</b></p>
        ) : (
          <Campo rotulo="Jogador">
            <select required value={dono || ''} onChange={(e) => definirDono(Number(e.target.value))} className="w-full">
              <option value="" disabled>Selecione…</option>
              {jogadores.data?.map((j) => <option key={j.id} value={j.id}>{nomeComApelido(j.nome, j.apelido)}</option>)}
            </select>
          </Campo>
        )}

        <SeletorComandante rotulo="Comandante" valor={comandante1} aoMudar={definirComandante1} obrigatorio />
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={temParceiro} onChange={(e) => definirTemParceiro(e.target.checked)} /> Tem parceiro / background
        </label>
        {temParceiro && <SeletorComandante rotulo="Segundo comandante (parceiro ou background)" valor={comandante2} aoMudar={definirComandante2} obrigatorio />}

        <Campo rotulo="Nome do deck (opcional)" dica={nomeDigitado ? undefined : 'Sugerido pelo comandante; edite se quiser.'}>
          <input value={nomeExibido} onChange={(e) => { definirNomeDigitado(true); definirNome(e.target.value) }} className="w-full" maxLength={120} />
        </Campo>
        <Campo rotulo="Link da lista (opcional)">
          <input type="url" placeholder="https://www.ligamagic.com.br/?view=dks/deck&id=…" value={urlLista} onChange={(e) => definirUrlLista(e.target.value)} className="w-full" />
        </Campo>
        {!deck && <p className="text-xs text-zinc-500">Decks novos entram como Pendente até a revisão do lojista.</p>}
        <ErroEmLinha erro={salvar.error} />
        <div className="flex justify-end gap-2">
          <button type="button" className="botao-secundario" onClick={aoFechar}>Cancelar</button>
          <button className="botao-primario" disabled={salvar.isPending || !dono}>Salvar</button>
        </div>
      </form>
    </Janela>
  )
}
