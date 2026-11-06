# AgendAí

Aplicativo mobile para agendamento de serviços em salões de beleza e barbearias (TCC).
O visual segue as telas do Figma (tema escuro com dourado).

| Camada | Tecnologia | Pasta |
|---|---|---|
| App mobile | .NET MAUI (C#), Windows (Android opcional) | `3-app` |
| API | Node.js + Express | `2-api` |
| Banco de dados | MySQL 8 | `1-banco-de-dados` |

```
Aplicativo (MAUI)  →  API (Node/Express)  →  MySQL  →  API  →  Aplicativo
```

```
AgendAi/
├── AgendAi.slnx          ← abra este arquivo no Visual Studio
├── README.md
├── 1-banco-de-dados/     ← agendai.sql (importe este) e partes/ (estrutura e dados separados)
├── 2-api/                ← API Node.js (src/, tests/)
└── 3-app/                ← app .NET MAUI (Views/, Services/, Models/, Helpers/, Resources/...)
```

Faça nesta ordem: **1) banco → 2) API → 3) app.**

**Atalho (Windows):** dê dois cliques em **`INICIAR-API.bat`**. Ele cria o `.env`, importa o banco, instala as dependências e inicia a API.

**Como o banco se conecta:** o app **não** fala direto com o MySQL. O app chama a API, e a API é quem usa o banco, com a senha configurada em `2-api/.env`. É o desenho do documento (App → API → Banco), e evita deixar a senha do banco dentro do aplicativo.

---

## 1. O que você precisa ter instalado

- **MySQL 8** (e o MySQL Workbench, se preferir interface gráfica)
- **Node.js 18 ou superior**
- **Visual Studio** com a carga de trabalho **.NET MAUI** (a mesma do projeto do professor, .NET 10)

## 2. Banco de dados

O arquivo **`1-banco-de-dados/agendai.sql`** cria o banco `agendai` com todas as tabelas **e** os dados de teste.

- **Workbench:** *File → Open SQL Script…* → escolha o arquivo → clique no raio ⚡.
- **Terminal:** `mysql -u root -p < 1-banco-de-dados/agendai.sql`

> Atenção: o script apaga e recria o banco `agendai`. Rodar de novo "zera" tudo.

Em `1-banco-de-dados/partes/` estão `schema.sql` (só a estrutura) e `seed.sql` (só os dados de teste), separados para consulta e para a documentação do TCC. **Para importar, use só o `agendai.sql`.**

## 3. API

```bash
cd 2-api
npm install
cp .env.example .env      # no Windows: copy .env.example .env
```

Abra o `.env` e preencha `DB_PASSWORD` (senha do seu MySQL) e `JWT_SECRET` (qualquer texto longo). Depois:

```bash
npm run dev
```

Ao iniciar, a API testa o banco e escreve no terminal `Banco de dados conectado` ou diz o que está errado (senha, MySQL desligado, banco não importado).
Você também pode conferir no navegador: **http://localhost:3000/saude** deve mostrar `{"api":"ok","banco":"ok"}`.
**Deixe esse terminal aberto** enquanto usa o app.

Testes automáticos da regra de agendamento (não precisam de banco): `npm test`

## 4. App (Visual Studio)

1. Abra **`AgendAi.slnx`** no Visual Studio (a API precisa estar rodando).
2. No topo, no seletor de destino, escolha **Windows Machine** e aperte **F5**.

**Android (opcional):** por padrão o projeto compila só para Windows, para não exigir a carga de trabalho do Android. Para testar no Android, instale essa carga de trabalho no *Visual Studio Installer*, abra `3-app/AgendAi.csproj` e troque `<IncluirAndroid>false</IncluirAndroid>` por `true`. Depois escolha um emulador Android como destino. O emulador acessa o seu computador por `10.0.2.2` (já configurado em `3-app/Services/ApiConfig.cs`). Em celular físico, preencha `UrlManual` nesse arquivo com o IP do seu PC, por exemplo `http://192.168.0.10:3000/`.

Se aparecer o erro `NETSDK1147` (carga de trabalho ausente) ou `NU1015` (Microsoft.Maui.Controls sem versão), abra o *Visual Studio Installer* → *Modificar* → marque **Desenvolvimento de interface do usuário multiplataforma .NET** → *Modificar*. Depois feche e reabra a solução e use *Compilação → Limpar solução*. Se o Windows pedir o **Modo de Desenvolvedor**, ative.

### Plano B: se a solução não abrir ou não compilar

Os arquivos de plataforma (`3-app/Platforms`, `Properties`, ícones) foram escritos à mão, sem o assistente do Visual Studio. Se der problema neles, use o projeto que o **próprio Visual Studio** gera:

1. *Arquivo → Novo → Projeto → **Aplicativo .NET MAUI*** (mesmo modelo do professor), com o nome **AgendAi**.
2. Apague do projeto novo: `MainPage.xaml`, `MainPage.xaml.cs`, `App.xaml`, `App.xaml.cs`, `AppShell.xaml`, `AppShell.xaml.cs` e `MauiProgram.cs`.
3. Copie para dentro do projeto novo, de `3-app`: as pastas `Controles`, `Helpers`, `Models`, `Services` e `Views`; as pastas `Resources/Styles` e `Resources/Images`; e os arquivos `App.xaml`, `App.xaml.cs`, `AppShell.xaml`, `AppShell.xaml.cs` e `MauiProgram.cs`.
4. Se o seu projeto usar um nome diferente de `AgendAi`, troque o namespace `AgendAi` nos arquivos copiados (*Ctrl+H*).
5. Em `Platforms/Android/AndroidManifest.xml` do projeto novo, copie as linhas de `uses-permission` (localização), `usesCleartextTraffic` e `queries` do arquivo de `3-app`.

## 5. Usuários de teste (senha de todos: `123456`)

| Perfil | Email | O que faz no app |
|---|---|---|
| Cliente | `cliente@teste.com` | descobre salões, agenda, reagenda, cancela, avalia |
| Cliente | `fernanda@teste.com`, `beatriz@teste.com` | idem (para testar concorrência de horários) |
| Profissional | `ana@agendai.com` (Studio Lumière) | agenda do dia, confirma, conclui, cancela, bloqueia horário |
| Profissional | `bruno@`, `marcos@`, `juliana@`, `rafael@agendai.com` | idem, em outros salões |
| Administrador | `admin@agendai.com` | vê a agenda de todos os profissionais dos seus salões |

**Roteiro de teste completo**

1. Entre como `cliente@teste.com`. Na **Home**, filtre por *Cabelo* e abra o *Studio Lumière*.
2. Na aba **Serviços**, toque em **+ Add** em *Corte + Escova* e depois em **Agendar**.
3. Escolha o profissional, um dia (segunda a sábado) e um horário → **Confirmar**.
4. Na aba **Agenda**, o pedido aparece como *Pendente*. Teste **Reagendar** e **Cancelar**.
5. Saia (**Perfil → Sair**) e entre como `ana@agendai.com`. Na **Agenda Profissional**, toque em **⋮** no atendimento → **Confirmar** → depois **Concluir atendimento**.
6. Ainda como Ana, toque em **+ Adicionar Horário Bloqueado** e bloqueie um horário livre.
7. Volte como cliente: em **Agenda → Histórico**, toque em **★ Avaliar**. A nota aparece na aba **Avaliações** do salão.
8. Crie outra conta (**Cadastrar**) e tente o **mesmo horário**: ele não aparece mais disponível.
9. O sino da Home mostra as **Notificações** (agendamentos, lembretes e promoções).

## 6. Telas (Figma → app)

| Figma | Tela no app |
|---|---|
| Login & Cadastro | `Views/Login` (abas Entrar / Cadastrar) |
| Descobrir Salões | `Views/Home` |
| Buscar | `Views/Buscar` (busca, categorias e ordenação) |
| Perfil do Salão | `Views/PerfilSalao` (serviços, avaliações, mapa, WhatsApp) |
| Agendar Horário | `Views/Agendar` (3 passos, semana, horários) |
| Meus Agendamentos | `Views/MeusAgendamentos` (Todos / Próximos / Histórico) |
| Agenda Profissional | `Views/AgendaProfissional` (resumo do dia e linha do tempo) |
| Adicionar Horário Bloqueado | `Views/BloquearHorario` |
| Notificações | `Views/Notificacoes` |

## 7. Endpoints da API

Todas as rotas, exceto `cadastro` e `login`, exigem o header `Authorization: Bearer <token>`.

| Método | Rota | Quem | Descrição |
|---|---|---|---|
| POST | `/api/auth/cadastro` | público | cria conta de **cliente** |
| POST | `/api/auth/login` | público | retorna usuário + token |
| GET | `/api/auth/me` | logado | dados do usuário logado |
| GET | `/api/estabelecimentos?busca=&categoria=&latitude=&longitude=` | logado | lista (nota, preço mínimo, distância) |
| GET | `/api/estabelecimentos/:id` | logado | detalhes, serviços e profissionais |
| GET | `/api/estabelecimentos/:id/avaliacoes` | logado | avaliações |
| POST | `/api/estabelecimentos` | administrador | cadastra estabelecimento |
| POST | `/api/estabelecimentos/:id/servicos` | administrador dono | cadastra serviço |
| POST | `/api/estabelecimentos/:id/profissionais` | administrador dono | cadastra profissional (cria o login dele) |
| GET | `/api/profissionais/meus` | profissional / admin | profissionais que o usuário representa |
| GET | `/api/profissionais/:id/horarios-disponiveis?data=&servicoId=` | logado | horários livres |
| PUT | `/api/profissionais/:id/horarios` | profissional / admin | define o expediente semanal |
| POST | `/api/profissionais/:id/bloqueios` | profissional / admin | bloqueia um horário |
| DELETE | `/api/profissionais/bloqueios/:id` | profissional / admin | remove um bloqueio |
| POST | `/api/agendamentos` | cliente | cria agendamento |
| GET | `/api/agendamentos/meus?periodo=todos\|proximos\|historico` | cliente | lista os do cliente |
| PATCH | `/api/agendamentos/:id/reagendar` | cliente dono | muda data e horário |
| GET | `/api/agendamentos/agenda?data=` | profissional / admin | resumo, expediente, atendimentos e bloqueios do dia |
| PATCH | `/api/agendamentos/:id/cancelar` | cliente dono / equipe | cancela |
| PATCH | `/api/agendamentos/:id/confirmar` | profissional / admin | confirma |
| PATCH | `/api/agendamentos/:id/concluir` | profissional / admin | conclui |
| POST | `/api/avaliacoes` | cliente | avalia atendimento concluído |
| GET | `/api/notificacoes` | logado | notificações e total de não lidas |
| PATCH | `/api/notificacoes/:id/lida` | logado | marca como lida |
| PATCH | `/api/notificacoes/marcar-todas` | logado | marca todas como lidas |

**Exemplo: cadastrar um profissional** (use o token do administrador, obtido em `/api/auth/login`):

```json
POST /api/estabelecimentos/1/profissionais
{
  "nome": "Julia Alves",
  "email": "julia@agendai.com",
  "senha": "123456",
  "especialidade": "Manicure",
  "servicosIds": [1]
}
```

## 8. Como o conflito de horários é evitado

1. `GET .../horarios-disponiveis` calcula os horários livres: expediente do profissional, menos agendamentos ativos, menos bloqueios, menos horários que já passaram.
2. `POST /api/agendamentos` (e o reagendamento) abre uma **transação**, trava a linha do profissional (`SELECT ... FOR UPDATE`) e **recalcula** os horários livres antes de gravar. Duas requisições simultâneas para o mesmo profissional são processadas uma por vez, e a segunda recebe `409 Este horário não está mais disponível`.
3. Agendamento cancelado libera o horário.
4. No reagendamento, o horário atual do próprio cliente conta como livre.

Código: `2-api/src/services/agendamentoService.js` e `2-api/src/utils/horarios.js`.

## 9. Lembretes automáticos

A API gera, a cada 15 minutos, um lembrete para cada agendamento ativo que acontece nas próximas 24 horas
(`2-api/src/services/lembreteService.js`). Eles aparecem na tela de Notificações, no filtro *Lembretes*.

## 10. Cobertura em relação ao documento do TCC

| Item do documento | Situação |
|---|---|
| Cadastro e login | feito |
| Busca por salões e barbearias | feito (nome, endereço, serviço e categoria) |
| Localização de estabelecimentos próximos | feito (GPS + ordenação por distância) |
| Serviços e profissionais | feito |
| Agendamento, horários disponíveis, sem conflito | feito |
| Cancelamento, reagendamento e histórico | feito |
| Agenda do profissional (confirmar/concluir/cancelar/bloquear) | feito |
| Avaliações | feito (nota de 1 a 5 e comentário) |
| Notificações e lembretes | feito dentro do app; **não há push** com o app fechado |
| Integração com WhatsApp | feito |
| Localização e rota | feito (abre o aplicativo de mapas do aparelho) |
| Pagamento no estabelecimento | sem processamento no app, conforme o documento |
| Cadastro de salões, serviços e profissionais | **só pela API** (exemplo na seção 7); falta a tela de administração |

## 11. O que ficou diferente do Figma

- **Login social (Google, Apple, Facebook) e "Esqueci minha senha":** não incluídos, porque a API não tem esses recursos.
- **Fotos dos salões e aba "Fotos":** não há upload de imagens. Os salões aparecem com a inicial sobre um degradê dourado.
- **Mapa:** o cartão do mapa é ilustrativo (mostra os preços) e o botão **Mapa completo** abre o aplicativo de mapas do aparelho. Um mapa embutido exigiria chave de API do Google/Bing.
- **Fonte dos títulos:** o Figma usa uma fonte serifada. O app usa a serifada do sistema (`serif` no Android, Georgia no Windows). Para usar a fonte exata, coloque o `.ttf` em `Resources/Fonts` e registre em `MauiProgram.cs`.
- **iOS e Mac:** não incluídos no projeto (exigem um Mac para compilar).
- **Sessão:** não persiste. Ao fechar o app, é preciso entrar de novo.

## 12. Atualize o documento do TCC

- Nome do aplicativo: **AgendAí**.
- Seções **4.1** e **6**: troque *React Native* por **.NET MAUI (C#)**.
- Seção **4.3**: o banco tem 10 tabelas (veja `1-banco-de-dados/partes/schema.sql`), incluindo `profissional_servicos`, `horarios_disponiveis` e `bloqueios_agenda`.
