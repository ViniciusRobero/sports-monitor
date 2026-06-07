# SportsMonitor

Um painel simples para acompanhar jogos de futebol ao vivo em várias fontes ao mesmo tempo e avisar quando alguma coisa não bate.

O SportsMonitor foi pensado para uma pessoa que precisa agir rápido, mas sem confiar cegamente em uma única fonte. Ele olha para os dados da partida, compara placar, gols, cartões e status do jogo, e mostra um alerta quando encontra diferença entre as fontes.

Ele não aposta sozinho. Ele não decide por você. Ele ajuda você a perceber que algo merece atenção.

---

## Para que ele serve

Durante uma partida ao vivo, fontes diferentes podem mostrar informações com alguns segundos de diferença. Uma fonte pode mostrar um gol antes da outra. Outra pode atribuir o gol ao jogador errado. Outra pode ainda estar com o placar antigo.

O SportsMonitor junta essas informações em um único lugar para responder perguntas simples:

- O placar está igual em todas as fontes?
- O gol apareceu em todas?
- O jogador do gol é o mesmo?
- O cartão foi para o mesmo jogador?
- A partida está ao vivo, no intervalo, encerrada ou suspensa?
- Existe algum sinal externo, como resultados do Google, que ajude a conferir rapidamente?

Quando alguma dessas respostas parece estranha, o painel mostra uma divergência e toca um aviso sonoro.

---

## Como é a experiência de uso

Você abre o painel e acompanha os jogos ao vivo.

Cada partida aparece com as informações recebidas das fontes disponíveis. Quando tudo está coerente, a tela funciona como um acompanhamento normal: times, placar, status e eventos principais.

Quando existe uma diferença, o sistema cria um card de alerta. Esse card mostra:

- qual partida precisa de atenção;
- qual foi a divergência encontrada;
- quais fontes concordam entre si;
- qual fonte está diferente;
- a gravidade do alerta;
- links e resultados de busca para ajudar na verificação manual.

A ideia é reduzir o tempo entre "algo mudou no jogo" e "eu entendi o que preciso conferir".

---

## Exemplo simples

Imagine este jogo:

```text
Flamengo x Palmeiras - 32 minutos

SofaScore:    Gol de Pedro
API-Football: Gol de Pedro
Bet365:       Gol de Arrascaeta
```

Nesse caso, o SportsMonitor mostra um alerta crítico porque duas fontes dizem uma coisa e a Bet365 mostra outra.

O usuário então confere o replay, olha os links de apoio, valida a informação e decide manualmente o que fazer.

---

## O que o sistema compara

### Placar

Compara o placar mostrado pelas fontes. Se uma fonte mostra `1 x 0` e outra mostra `0 x 0`, o painel destaca a diferença.

### Gols

Confere se os gols aparecem nas fontes e se o minuto do gol parece bater.

### Jogador do gol

Quando as fontes informam o autor do gol, o sistema compara os nomes. Se um lugar mostra Pedro e outro mostra Arrascaeta, isso vira alerta.

### Cartões

Compara cartões amarelos e vermelhos quando a fonte disponibiliza esse detalhe.

### Status da partida

Compara se o jogo está ao vivo, no intervalo, encerrado, adiado, cancelado ou suspenso.

### Busca de apoio no Google

O Google entra como apoio visual, não como fonte oficial de placar. O painel pode mostrar resultados de busca relacionados ao jogo para ajudar o usuário a abrir rapidamente uma notícia, tempo real ou página de confirmação.

---

## Fontes usadas

O projeto pode trabalhar com estas fontes:

| Fonte | O que ajuda a conferir |
|---|---|
| Bet365 via BetsAPI | Placar, eventos e referência operacional |
| SofaScore | Placar, gols, cartões e incidentes da partida |
| 365Scores | Principalmente placar e status |
| API-Football | Eventos estruturados, quando configurada |
| Google | Links e trechos para verificação manual |

Nem todas precisam estar ligadas ao mesmo tempo. O sistema fica melhor quando tem mais de uma fonte ativa, porque a comparação fica mais forte.

---

## De quanto em quanto tempo atualiza

Os intervalos podem ser mudados no arquivo `src/SportsMonitor.Bff/appsettings.json`.

Na configuração atual do projeto:

| Fonte ou módulo | Intervalo atual |
|---|---:|
| SofaScore no BFF | 10 segundos |
| 365Scores | 10 segundos |
| Google | 300 segundos, ou 5 minutos |
| API-Football | 30 segundos, quando habilitada |
| BetsAPI / Bet365 | 30 segundos, quando habilitada |
| Modo demonstração | 10 segundos, quando habilitado |

Também existe um agente local para SofaScore. Ele foi criado porque alguns servidores em nuvem podem receber bloqueio do SofaScore, enquanto uma máquina residencial costuma conseguir acessar normalmente. Por padrão, esse agente local envia dados para o BFF a cada 30 segundos.

---

## Onde o usuário vê as informações

O usuário vê tudo no dashboard web.

Em desenvolvimento, ele normalmente abre:

```text
http://localhost:4200
```

Quando o pacote desktop é usado, o aplicativo abre a tela sozinho. A pessoa não precisa entender os serviços por trás: ela executa o app e acompanha o painel.

No painel aparecem:

- jogos ao vivo;
- dados separados por fonte;
- divergências detectadas;
- alertas em tempo real;
- resultados de apoio do Google;
- estado de verificação manual quando o usuário marca uma divergência como conferida.

---

## O que acontece quando aparece um alerta

O alerta não significa automaticamente que uma fonte está errada. Ele significa que existe uma diferença que merece atenção.

O fluxo esperado é:

1. O painel toca um aviso.
2. O usuário abre o card da divergência.
3. O usuário vê quais fontes estão divergindo.
4. O usuário confere replay, tempo real, Google ou outra fonte confiável.
5. O usuário decide manualmente se precisa agir.

Esse ponto é importante: o SportsMonitor não substitui o julgamento humano. Ele organiza os sinais para que a pessoa não precise procurar tudo do zero.

---

## Modo demonstração

O projeto tem um modo demo para mostrar o funcionamento sem depender de APIs reais.

Quando o modo demo está ligado, o sistema cria partidas simuladas e divergências de exemplo, como:

- placar atrasado em uma fonte;
- gol aparecendo em uma fonte antes da outra;
- jogador do gol diferente;
- cartão divergente;
- mudança de status da partida.

Isso ajuda em apresentações, testes e validação do painel.

Para uso real, o modo demo deve ficar desligado.

---

## Como rodar para testar

Esta parte é para quem vai abrir o projeto em ambiente de desenvolvimento.

Requisitos:

- .NET 10 SDK
- Node.js 18 ou superior
- Angular CLI

Instale as dependências do painel:

```powershell
cd src\SportsMonitor.Web
npm install
```

Rode o BFF:

```powershell
dotnet run --project src\SportsMonitor.Bff\SportsMonitor.Bff.csproj
```

Rode o dashboard:

```powershell
cd src\SportsMonitor.Web
ng serve
```

Abra:

```text
http://localhost:4200
```

---

## Como rodar o agente local do SofaScore

Use o agente local quando o BFF estiver em uma VM ou servidor que não consegue acessar o SofaScore diretamente.

Exemplo:

```powershell
dotnet run --project src\SportsMonitor.LocalAgent -- --bff-url http://34.151.245.70 --interval 30
```

O que ele faz:

- busca os jogos no SofaScore usando a internet da máquina local;
- normaliza os dados no mesmo formato do sistema;
- envia as partidas para o BFF;
- repete isso no intervalo configurado.

---

## Como gerar um pacote desktop

Para gerar uma pasta pronta para uso:

```powershell
.\publish.ps1
```

Isso cria a pasta `publish\`.

Para usar, execute:

```text
publish\SportsMonitor.Desktop.exe
```

O aplicativo desktop inicia o servidor local e abre o painel automaticamente.

---

## O que o projeto não faz

Para deixar o objetivo claro:

- não faz apostas automaticamente;
- não acessa conta da Bet365;
- não tenta burlar login, captcha ou bloqueios de conta;
- não garante que uma fonte está certa;
- não substitui a confirmação humana;
- não deve ser usado como única base de decisão.

Ele é uma ferramenta de monitoramento e apoio à verificação.

---

## Estrutura do projeto, em linguagem simples

| Pasta | O que tem dentro |
|---|---|
| `SportsMonitor.Bff` | O servidor que junta os dados e entrega para o painel |
| `SportsMonitor.Web` | A tela que o usuário vê no navegador |
| `SportsMonitor.Desktop` | O aplicativo Windows que abre o painel automaticamente |
| `SportsMonitor.Infrastructure` | As conexões com fontes externas, como SofaScore e 365Scores |
| `SportsMonitor.Workers` | Rotinas que buscam informações de tempos em tempos |
| `SportsMonitor.Application` | As regras que decidem se existe divergência |
| `SportsMonitor.Domain` | Os modelos principais do sistema |
| `SportsMonitor.LocalAgent` | O agente local que envia dados do SofaScore para o BFF |
| `SportsMonitor.Tests` | Testes automatizados do projeto |

---

## Estado atual

O projeto já tem:

- dashboard Angular;
- BFF em .NET;
- alertas em tempo real;
- comparação entre fontes;
- regras para placar, gols, cartões e status;
- modo demonstração;
- agente local para SofaScore;
- empacotamento desktop;
- testes automatizados.

Última validação local: build completo e 78 testes passando.
