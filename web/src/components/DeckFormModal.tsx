import { useState, type FormEvent } from 'react'
import { usePlayers, useSaveDeck } from '../api/hooks'
import type { Deck } from '../api/types'
import { playerLabel } from '../lib/format'
import { CommanderPicker, type PickedCommander } from './CommanderPicker'
import { Field, InlineError, Modal } from './ui'

/**
 * New / edit deck. The commander comes from Scryfall; the deck name is optional and is pre-filled with the
 * commander name(s) until the user types their own. Statistics always use the commanders, not the name.
 */
export function DeckFormModal({ open, deck, playerId, onClose }: {
  open: boolean
  /** Deck being edited (null = new). */
  deck: Deck | null
  /** Fixed owner (e.g. opened from the player profile). */
  playerId?: number
  onClose: () => void
}) {
  const players = usePlayers()
  const save = useSaveDeck()
  const [owner, setOwner] = useState(deck?.playerId ?? playerId ?? 0)
  const toPicked = (i: number): PickedCommander | null => {
    const c = deck?.commanders[i]
    return c ? { scryfallId: null, name: c.name, artUrl: c.artUrl, colorIdentity: c.colorIdentity } : null
  }
  const [cmd1, setCmd1] = useState<PickedCommander | null>(toPicked(0))
  const [cmd2, setCmd2] = useState<PickedCommander | null>(toPicked(1))
  const [hasPartner, setHasPartner] = useState((deck?.commanders.length ?? 1) > 1)
  const [name, setName] = useState(deck?.name ?? '')
  const [nameTouched, setNameTouched] = useState(deck !== null)
  const [listUrl, setListUrl] = useState(deck?.listUrl ?? '')

  const suggested = [cmd1, hasPartner ? cmd2 : null].filter((c): c is PickedCommander => !!c?.name.trim()).map((c) => c.name.trim()).sort().join(' + ')
  const shownName = nameTouched ? name : suggested

  async function submit(e: FormEvent) {
    e.preventDefault()
    const commanders = [cmd1, hasPartner ? cmd2 : null]
      .filter((c): c is PickedCommander => !!c?.name.trim())
      .map((c) => ({ scryfallId: c.scryfallId, name: c.name.trim() }))
    await save.mutateAsync({
      id: deck?.id,
      data: { playerId: owner, name: shownName.trim() || null, listUrl: listUrl.trim() || null, commanders },
    })
    onClose()
  }

  const fixedOwner = playerId != null && deck === null
  const ownerName = players.data?.find((p) => p.id === owner)

  return (
    <Modal open={open} onClose={onClose} title={deck ? 'Editar deck' : 'Novo deck'}>
      <form onSubmit={submit} className="space-y-4">
        {fixedOwner ? (
          <p className="text-sm text-zinc-300">Jogador: <b>{ownerName ? playerLabel(ownerName.name, ownerName.nickname) : '…'}</b></p>
        ) : (
          <Field label="Jogador">
            <select required value={owner || ''} onChange={(e) => setOwner(Number(e.target.value))} className="w-full">
              <option value="" disabled>Selecione…</option>
              {players.data?.map((p) => <option key={p.id} value={p.id}>{playerLabel(p.name, p.nickname)}</option>)}
            </select>
          </Field>
        )}

        <CommanderPicker label="Comandante" value={cmd1} onChange={setCmd1} required />
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={hasPartner} onChange={(e) => setHasPartner(e.target.checked)} /> Tem parceiro / background
        </label>
        {hasPartner && <CommanderPicker label="Segundo comandante (parceiro ou background)" value={cmd2} onChange={setCmd2} required />}

        <Field label="Nome do deck (opcional)" hint={nameTouched ? undefined : 'Sugerido pelo comandante; edite se quiser.'}>
          <input value={shownName} onChange={(e) => { setNameTouched(true); setName(e.target.value) }} className="w-full" maxLength={120} />
        </Field>
        <Field label="Link da lista (opcional)">
          <input type="url" placeholder="https://www.ligamagic.com.br/?view=dks/deck&id=…" value={listUrl} onChange={(e) => setListUrl(e.target.value)} className="w-full" />
        </Field>
        {!deck && <p className="text-xs text-zinc-500">Decks novos entram como Pendente até a revisão do lojista.</p>}
        <InlineError error={save.error} />
        <div className="flex justify-end gap-2">
          <button type="button" className="btn-secondary" onClick={onClose}>Cancelar</button>
          <button className="btn-primary" disabled={save.isPending || !owner}>Salvar</button>
        </div>
      </form>
    </Modal>
  )
}
