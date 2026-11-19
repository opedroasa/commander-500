import { KeyRound, Plus, Trash2 } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { useAdministradores, useAlterarSenhaAdministrador, useCriarAdministrador, useExcluirAdministrador } from '../../api/consultas'
import type { Administrador } from '../../api/tipos'
import { CabecalhoPagina, CaixaErro, Campo, Carregando, ErroEmLinha, Janela } from '../../componentes/comum'

/** Mesmo mínimo da Api (RegrasAdministrador.TamanhoMinimoSenha). */
const TAMANHO_MINIMO_SENHA = 8

/**
 * Quem pode entrar na área do lojista. Qualquer administrador cadastra outro, troca senhas e exclui
 * (menos a si mesmo e o último que sobrar).
 */
export function PaginaAdministradores() {
  const administradores = useAdministradores()
  const excluir = useExcluirAdministrador()
  const [criando, definirCriando] = useState(false)
  const [trocandoSenha, definirTrocandoSenha] = useState<Administrador | null>(null)

  if (administradores.isPending) return <Carregando />
  if (administradores.error) return <CaixaErro erro={administradores.error} aoTentarDeNovo={() => administradores.refetch()} />

  const lista = administradores.data
  const unico = lista.length <= 1

  async function confirmarExclusao(administrador: Administrador) {
    if (confirm(`Excluir o acesso de ${administrador.email}? A pessoa sai da área do admin na hora.`)) {
      await excluir.mutateAsync(administrador.id)
    }
  }

  return (
    <>
      <CabecalhoPagina
        titulo="Administradores"
        subtitulo="Quem pode entrar na área do lojista"
        acoes={<button className="botao-primario" onClick={() => definirCriando(true)}><Plus className="size-4" aria-hidden />Novo administrador</button>}
      />
      <ErroEmLinha erro={excluir.error} />
      <ul className="space-y-1.5">
        {lista.map((a) => (
          <li key={a.id} className="cartao flex items-center gap-3 p-1.5 pl-3">
            <div className="min-w-0 flex-1 py-1.5">
              <div className="truncate font-medium">
                {a.email}
                {a.usuarioAtual && <span className="ml-2 rounded bg-amber-950 px-1.5 py-0.5 text-xs text-amber-300 ring-1 ring-amber-800">você</span>}
              </div>
            </div>
            <button className="botao-discreto botao-pequeno" onClick={() => definirTrocandoSenha(a)} aria-label={`Trocar a senha de ${a.email}`} title="Trocar senha">
              <KeyRound className="size-4" />
            </button>
            {!a.usuarioAtual && (
              <button className="botao-discreto botao-pequeno text-red-300 disabled:opacity-40" onClick={() => confirmarExclusao(a)} disabled={unico}
                aria-label={`Excluir ${a.email}`} title={unico ? 'Não é possível excluir o último administrador' : 'Excluir'}>
                <Trash2 className="size-4" />
              </button>
            )}
          </li>
        ))}
      </ul>
      <p className="mt-4 text-xs text-zinc-500">
        Esqueceu a senha? Outro administrador troca por aqui (ícone da chave). Para excluir o seu próprio acesso, peça para outro administrador.
      </p>
      {criando && <JanelaNovoAdministrador aoFechar={() => definirCriando(false)} />}
      {trocandoSenha && <JanelaTrocarSenha administrador={trocandoSenha} aoFechar={() => definirTrocandoSenha(null)} />}
    </>
  )
}

function JanelaNovoAdministrador({ aoFechar }: { aoFechar: () => void }) {
  const criar = useCriarAdministrador()
  const [email, definirEmail] = useState('')
  const [senha, definirSenha] = useState('')
  const [confirmacao, definirConfirmacao] = useState('')
  const senhasDiferentes = confirmacao.length > 0 && senha !== confirmacao

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    if (senha !== confirmacao) return
    await criar.mutateAsync({ email, senha })
    aoFechar()
  }

  return (
    <Janela aberta aoFechar={aoFechar} titulo="Novo administrador">
      <form onSubmit={enviar} className="space-y-4">
        <Campo rotulo="E-mail (é o login)">
          <input type="email" required value={email} onChange={(e) => definirEmail(e.target.value)} className="w-full" autoFocus autoComplete="off" />
        </Campo>
        <CamposSenha senha={senha} confirmacao={confirmacao} aoMudarSenha={definirSenha} aoMudarConfirmacao={definirConfirmacao} />
        {senhasDiferentes && <p className="text-sm text-red-300" role="alert">As senhas não conferem.</p>}
        <ErroEmLinha erro={criar.error} />
        <div className="flex justify-end gap-2">
          <button type="button" className="botao-secundario" onClick={aoFechar}>Cancelar</button>
          <button className="botao-primario" disabled={criar.isPending || senhasDiferentes}>Cadastrar</button>
        </div>
      </form>
    </Janela>
  )
}

function JanelaTrocarSenha({ administrador, aoFechar }: { administrador: Administrador; aoFechar: () => void }) {
  const alterar = useAlterarSenhaAdministrador()
  const [senha, definirSenha] = useState('')
  const [confirmacao, definirConfirmacao] = useState('')
  const senhasDiferentes = confirmacao.length > 0 && senha !== confirmacao

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    if (senha !== confirmacao) return
    await alterar.mutateAsync({ id: administrador.id, senha })
    aoFechar()
  }

  return (
    <Janela aberta aoFechar={aoFechar} titulo={administrador.usuarioAtual ? 'Trocar minha senha' : `Nova senha de ${administrador.email}`}>
      <form onSubmit={enviar} className="space-y-4">
        <CamposSenha senha={senha} confirmacao={confirmacao} aoMudarSenha={definirSenha} aoMudarConfirmacao={definirConfirmacao} autoFocus />
        {senhasDiferentes && <p className="text-sm text-red-300" role="alert">As senhas não conferem.</p>}
        <ErroEmLinha erro={alterar.error} />
        <div className="flex justify-end gap-2">
          <button type="button" className="botao-secundario" onClick={aoFechar}>Cancelar</button>
          <button className="botao-primario" disabled={alterar.isPending || senhasDiferentes}>Salvar senha</button>
        </div>
      </form>
    </Janela>
  )
}

function CamposSenha({ senha, confirmacao, aoMudarSenha, aoMudarConfirmacao, autoFocus = false }: {
  senha: string
  confirmacao: string
  aoMudarSenha: (valor: string) => void
  aoMudarConfirmacao: (valor: string) => void
  autoFocus?: boolean
}) {
  return (
    <>
      <Campo rotulo={`Senha (mínimo ${TAMANHO_MINIMO_SENHA} caracteres)`}>
        <input type="password" required minLength={TAMANHO_MINIMO_SENHA} maxLength={128} value={senha}
          onChange={(e) => aoMudarSenha(e.target.value)} className="w-full" autoComplete="new-password" autoFocus={autoFocus} />
      </Campo>
      <Campo rotulo="Repita a senha">
        <input type="password" required minLength={TAMANHO_MINIMO_SENHA} maxLength={128} value={confirmacao}
          onChange={(e) => aoMudarConfirmacao(e.target.value)} className="w-full" autoComplete="new-password" />
      </Campo>
    </>
  )
}
