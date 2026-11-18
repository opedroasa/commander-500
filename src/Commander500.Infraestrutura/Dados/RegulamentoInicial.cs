namespace Commander500.Infraestrutura.Dados;

/// <summary>
/// Texto inicial do regulamento: o "Tratado dos Quinhentos" (V3) que o lojista escreveu no Google Docs.
/// Só é gravado quando o banco ainda não tem regulamento; depois o lojista edita pela tela de admin.
/// </summary>
public static class RegulamentoInicial
{
    public const string Html = """
<h1>Tratado dos Quinhentos</h1>
<p><strong>Regulamento oficial • Pré-temporada 2026</strong><br>Fantasy Store / Commander 500 — Uberlândia · MG</p>
<p>Quatro etapas, um ranking e uma final entre os quatro melhores. A pré-temporada do Commander 500 reúne a comunidade para jogar, conhecer o formato e disputar o título de encerramento de 2026.</p>
<ul>
<li><strong>04</strong> etapas quinzenais</li>
<li><strong>R$ 500</strong> — 99 cartas (sem comandante)</li>
<li><strong>Top 4</strong> — final da pré-temporada</li>
</ul>
<h2>Capítulo I — Do calendário e da competição</h2>
<p><strong>Art. 1º — Do período.</strong> A pré-temporada será realizada entre 14 de outubro e 5 de dezembro de 2026, com quatro torneios aos sábados. A data da primeira etapa será anunciada pela Fantasy Store; as demais datas estão previstas no calendário abaixo. Cada etapa somará pontos para o ranking geral.</p>
<h3>Calendário das classificatórias</h3>
<table>
<tr><th><p>Etapa</p></th><th><p>Data</p></th><th><p>Dia</p></th><th><p>Função</p></th></tr>
<tr><td><p>01</p></td><td><p>??/10/2026</p></td><td><p>Sábado (a definir)</p></td><td><p>Abertura do ranking</p></td></tr>
<tr><td><p>02</p></td><td><p>07/11/2026</p></td><td><p>Sábado</p></td><td><p>Classificatória</p></td></tr>
<tr><td><p>03</p></td><td><p>21/11/2026</p></td><td><p>Sábado</p></td><td><p>Classificatória</p></td></tr>
<tr><td><p>04</p></td><td><p>05/12/2026</p></td><td><p>Sábado</p></td><td><p>Última classificatória</p></td></tr>
</table>
<p>A final Top 4 acontecerá em 05/12, após a apuração dos pontos da 4ª etapa.</p>
<h2>Capítulo II — Dos decks, valores e proxies</h2>
<p><strong>Art. 2º — Do limite financeiro.</strong> As 99 cartas do deck deverão respeitar, juntas, o limite de R$ 500,00, segundo os valores de referência da Liga Magic adotados pela Fantasy Store no início da pré-temporada. O comandante não entra no cálculo desse valor.</p>
<p>O jogador que montar seu deck e desejar participar deverá enviar uma foto do deck dele via Liga Magic (lista) para que fique salvo o congelamento das cartas.</p>
<p>Pode-se utilizar até um total de 20 cartas extras do maybeboard, caso queira testar algumas modificações no deck.</p>
<p><strong>Art. 3º — Do congelamento.</strong> Os valores das cartas e a lista do deck ficarão congelados durante toda a pré-temporada. Cada participante deverá apresentar sua lista antes de disputar a primeira etapa; após o registro, não serão permitidas trocas de cartas ou de comandante até o encerramento, salvo necessidade de adequação a uma regra oficial do formato. A substituição de uma proxy pela carta original correspondente é permitida.</p>
<p><strong>Art. 4º — Das proxies.</strong> Proxies estarão liberadas sem limite de quantidade em todas as etapas. Devem ser legíveis, identificar claramente a carta representada e não permitir distinção entre cartas embaralhadas. O teto de R$ 500,00 para as 99 cartas permanece válido mesmo com proxies; o comandante não é contabilizado nesse limite.</p>
<h2>Capítulo III — Da pontuação e da classificação</h2>
<p><strong>Art. 5º — Do ranking.</strong> Os quatro torneios classificatórios somarão pontos para um único ranking. Cada participante que concluir a etapa receberá 1 ponto de presença, acrescido da bonificação correspondente à sua colocação final.</p>
<h3>Pontuação por etapa</h3>
<table>
<tr><th><p>Colocação</p></th><th><p>Pontos de presença</p></th><th><p>Bônus</p></th><th><p>Total</p></th></tr>
<tr><td><p>1º lugar</p></td><td><p>1</p></td><td><p>+4</p></td><td><p>5 pontos</p></td></tr>
<tr><td><p>2º lugar</p></td><td><p>1</p></td><td><p>+3</p></td><td><p>4 pontos</p></td></tr>
<tr><td><p>3º lugar</p></td><td><p>1</p></td><td><p>+2</p></td><td><p>3 pontos</p></td></tr>
<tr><td><p>4º lugar</p></td><td><p>1</p></td><td><p>+1</p></td><td><p>2 pontos</p></td></tr>
<tr><td><p>5º lugar em diante</p></td><td><p>1</p></td><td><p>—</p></td><td><p>1 ponto</p></td></tr>
</table>
<p><strong>Art. 6º — Dos desempates.</strong> Em caso de empate no ranking, serão considerados, nesta ordem: maior número de 1º lugares; maior número de participações concluídas; melhor colocação na última etapa em que os empatados participaram. Persistindo o empate por vaga no Top 4, a Fantasy Store realizará um desempate entre os envolvidos.</p>
<h2>Capítulo IV — Da final Top 4</h2>
<p><strong>Art. 7º — Da classificação.</strong> Os quatro jogadores mais bem colocados no ranking após a 4ª etapa disputarão a final. Se algum classificado não puder participar, a vaga passará ao próximo jogador do ranking.</p>
<p><strong>Art. 8º — Da disputa final.</strong> A final será disputada em uma mesa de Commander com quatro jogadores, no mês de dezembro de 2026, após a última classificatória. O vencedor receberá o título de Campeão da Pré-temporada do Tratado dos Quinhentos.</p>
<h3>Premiação do Top 4</h3>
<ul>
<li><strong>1º lugar:</strong> Troféu Campeão Pré-temporada + 2 Boosters + 1 Kit de Dados Viper + 1 Shield Foil</li>
<li><strong>2º lugar:</strong> 1 Kit de Dados Viper + 1 Booster + 1 Shield Básico</li>
<li><strong>3º lugar:</strong> 2 Boosters</li>
<li><strong>4º lugar:</strong> 1 Booster</li>
</ul>
<p><strong>Art. 9º — Do encerramento.</strong> A Fantasy Store divulgará os horários e inscrições nos canais oficiais da loja. A premiação do Top 4 será definida e divulgada antes da final. O ranking se encerra em 05/12/2026 e não será transferido para competições futuras.</p>
""";
}
