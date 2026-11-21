import { Save } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useConfiguracaoLoja, useSalvarConfiguracaoLoja } from '../../api/consultas'
import { CabecalhoPagina, CaixaErro, Campo, Carregando, ErroEmLinha } from '../../componentes/comum'

/** Configurações gerais da loja: por enquanto, o link do grupo de WhatsApp do Commander 500. */
export function PaginaConfiguracoes() {
  const configuracao = useConfiguracaoLoja()
  if (configuracao.isPending) return <Carregando />
  if (configuracao.error) return <CaixaErro erro={configuracao.error} aoTentarDeNovo={() => configuracao.refetch()} />
  return <Formulario linkAtual={configuracao.data.linkGrupoWhatsapp ?? ''} />
}

function Formulario({ linkAtual }: { linkAtual: string }) {
  const salvar = useSalvarConfiguracaoLoja()
  const [link, definirLink] = useState(linkAtual)
  const [salvo, definirSalvo] = useState(false)

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    await salvar.mutateAsync({ linkGrupoWhatsapp: link.trim() || null })
    definirSalvo(true)
  }

  return (
    <>
      <CabecalhoPagina titulo="Configurações" subtitulo="Informações da loja que aparecem no site." />
      <form onSubmit={enviar} className="cartao max-w-xl space-y-4 p-4">
        <Campo rotulo="Link do grupo de WhatsApp"
          dica="No WhatsApp: dados do grupo → Convidar via link → Copiar link. Deixe vazio para esconder o botão do site.">
          <input type="url" value={link} onChange={(e) => { definirLink(e.target.value); definirSalvo(false) }}
            placeholder="https://chat.whatsapp.com/…" className="w-full" />
        </Campo>
        <p className="text-xs text-zinc-500">O botão do grupo aparece no menu do topo e no rodapé do site.</p>
        <ErroEmLinha erro={salvar.error} />
        {salvo && <p className="text-sm text-emerald-300">Salvo!</p>}
        <div className="flex justify-end">
          <button className="botao-primario" disabled={salvar.isPending || link.trim() === linkAtual}>
            <Save className="size-4" aria-hidden />Salvar
          </button>
        </div>
      </form>
    </>
  )
}
