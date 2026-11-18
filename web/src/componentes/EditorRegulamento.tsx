import { TableKit } from '@tiptap/extension-table'
import { EditorContent, useEditor, useEditorState, type Editor } from '@tiptap/react'
import StarterKit from '@tiptap/starter-kit'
import {
  Bold, Heading1, Heading2, Heading3, Italic, List, ListOrdered, Minus, Quote, Redo2, Table, TableCellsMerge, Trash2, Undo2,
} from 'lucide-react'
import type { ReactNode } from 'react'

/**
 * Editor visual do regulamento (tipo um Word simples): títulos, negrito, listas e tabelas com botões.
 * Gera HTML, que é salvo na Api e mostrado na página pública de regras.
 */
export function EditorRegulamento({ conteudoInicial, aoMudar }: { conteudoInicial: string; aoMudar: (html: string) => void }) {
  const editor = useEditor({
    extensions: [StarterKit.configure({ link: { openOnClick: false } }), TableKit.configure({ table: { resizable: false } })],
    content: conteudoInicial,
    editorProps: { attributes: { class: 'texto-regulamento', 'aria-label': 'Texto do regulamento' } },
    onUpdate: ({ editor: e }) => aoMudar(e.getHTML()),
  })

  return (
    // Sem overflow-hidden aqui: ele prenderia a barra "grudenta" dentro do cartão em vez de no topo da tela.
    <div className="cartao">
      <BarraFerramentas editor={editor} />
      <div className="p-4">
        <EditorContent editor={editor} />
      </div>
    </div>
  )
}

function BarraFerramentas({ editor }: { editor: Editor }) {
  // Lê o estado do editor (o que está ativo) para destacar os botões.
  const estado = useEditorState({
    editor,
    selector: ({ editor: e }) => ({
      negrito: e.isActive('bold'),
      italico: e.isActive('italic'),
      titulo1: e.isActive('heading', { level: 1 }),
      titulo2: e.isActive('heading', { level: 2 }),
      titulo3: e.isActive('heading', { level: 3 }),
      lista: e.isActive('bulletList'),
      listaNumerada: e.isActive('orderedList'),
      citacao: e.isActive('blockquote'),
      naTabela: e.isActive('table'),
      podeDesfazer: e.can().undo(),
      podeRefazer: e.can().redo(),
    }),
  })
  const comando = () => editor.chain().focus()

  return (
    <div className="sticky top-14 z-10 flex flex-wrap items-center gap-0.5 rounded-t-2xl border-b border-zinc-800 bg-zinc-900 p-1.5">
      <Botao titulo="Título" ativo={estado.titulo1} aoClicar={() => comando().toggleHeading({ level: 1 }).run()}><Heading1 className="size-4" /></Botao>
      <Botao titulo="Capítulo" ativo={estado.titulo2} aoClicar={() => comando().toggleHeading({ level: 2 }).run()}><Heading2 className="size-4" /></Botao>
      <Botao titulo="Subtítulo" ativo={estado.titulo3} aoClicar={() => comando().toggleHeading({ level: 3 }).run()}><Heading3 className="size-4" /></Botao>
      <Separador />
      <Botao titulo="Negrito" ativo={estado.negrito} aoClicar={() => comando().toggleBold().run()}><Bold className="size-4" /></Botao>
      <Botao titulo="Itálico" ativo={estado.italico} aoClicar={() => comando().toggleItalic().run()}><Italic className="size-4" /></Botao>
      <Botao titulo="Lista" ativo={estado.lista} aoClicar={() => comando().toggleBulletList().run()}><List className="size-4" /></Botao>
      <Botao titulo="Lista numerada" ativo={estado.listaNumerada} aoClicar={() => comando().toggleOrderedList().run()}><ListOrdered className="size-4" /></Botao>
      <Botao titulo="Destaque (citação)" ativo={estado.citacao} aoClicar={() => comando().toggleBlockquote().run()}><Quote className="size-4" /></Botao>
      <Botao titulo="Linha divisória" aoClicar={() => comando().setHorizontalRule().run()}><Minus className="size-4" /></Botao>
      <Separador />
      <Botao titulo="Inserir tabela" aoClicar={() => comando().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()}><Table className="size-4" /></Botao>
      {estado.naTabela && (
        <>
          <BotaoTexto aoClicar={() => comando().addRowAfter().run()}>+ linha</BotaoTexto>
          <BotaoTexto aoClicar={() => comando().addColumnAfter().run()}>+ coluna</BotaoTexto>
          <BotaoTexto aoClicar={() => comando().deleteRow().run()}>− linha</BotaoTexto>
          <BotaoTexto aoClicar={() => comando().deleteColumn().run()}>− coluna</BotaoTexto>
          <Botao titulo="Juntar/separar células" aoClicar={() => comando().mergeOrSplit().run()}><TableCellsMerge className="size-4" /></Botao>
          <Botao titulo="Apagar tabela" aoClicar={() => comando().deleteTable().run()}><Trash2 className="size-4" /></Botao>
        </>
      )}
      <span className="ml-auto flex gap-0.5">
        <Botao titulo="Desfazer" desabilitado={!estado.podeDesfazer} aoClicar={() => comando().undo().run()}><Undo2 className="size-4" /></Botao>
        <Botao titulo="Refazer" desabilitado={!estado.podeRefazer} aoClicar={() => comando().redo().run()}><Redo2 className="size-4" /></Botao>
      </span>
    </div>
  )
}

function Botao({ titulo, ativo = false, desabilitado = false, aoClicar, children }: {
  titulo: string; ativo?: boolean; desabilitado?: boolean; aoClicar: () => void; children: ReactNode
}) {
  return (
    <button type="button" title={titulo} aria-label={titulo} aria-pressed={ativo} disabled={desabilitado} onClick={aoClicar}
      className={`rounded-md p-2 transition disabled:opacity-40 ${ativo ? 'bg-amber-500/20 text-amber-300' : 'text-zinc-300 hover:bg-zinc-800'}`}>
      {children}
    </button>
  )
}

function BotaoTexto({ aoClicar, children }: { aoClicar: () => void; children: ReactNode }) {
  return <button type="button" onClick={aoClicar} className="rounded-md px-2 py-1.5 text-xs text-zinc-300 hover:bg-zinc-800">{children}</button>
}

function Separador() {
  return <span className="mx-1 h-5 w-px bg-zinc-700" aria-hidden />
}
