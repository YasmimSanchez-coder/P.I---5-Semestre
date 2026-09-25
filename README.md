<<<<<<< HEAD
# BeautyAgenda

Aplicativo para agendamento de serviços em salões de beleza e barbearias.

## Status: Sprint 1 — Configuração do projeto, cadastro e login de usuários

## Estrutura do projeto

```
beautyagenda/
├── database/
│   └── schema.sql          # Script de criação do banco MySQL
├── backend/
│   ├── src/
│   │   ├── config/
│   │   │   └── database.js       # Conexão com o MySQL
│   │   ├── controllers/
│   │   │   └── authController.js # Lógica de cadastro/login
│   │   ├── middlewares/
│   │   │   └── authMiddleware.js # Validação de token JWT
│   │   ├── models/
│   │   │   └── usuarioModel.js   # Queries da tabela usuarios
│   │   ├── routes/
│   │   │   ├── authRoutes.js
│   │   │   └── index.js
│   │   └── server.js             # Ponto de entrada da API
│   ├── package.json
│   └── .env.example
└── README.md
```

## Como rodar o backend

1. Instale o MySQL localmente (ou use Docker) e crie o banco executando:
   ```bash
   mysql -u root -p < database/schema.sql
   ```

2. Entre na pasta do backend e instale as dependências:
   ```bash
   cd backend
   npm install
   ```

3. Copie o arquivo de variáveis de ambiente e preencha com seus dados:
   ```bash
   cp .env.example .env
   ```

4. Rode o servidor em modo desenvolvimento:
   ```bash
   npm run dev
   ```

   A API vai subir em `http://localhost:3000`.

## Rotas disponíveis (Sprint 1)

| Método | Rota              | Descrição                          | Autenticação |
|--------|-------------------|-------------------------------------|--------------|
| POST   | /api/auth/cadastro| Cria um novo usuário (cliente/profissional/administrador) | Não |
| POST   | /api/auth/login   | Autentica e retorna um token JWT   | Não |
| GET    | /api/auth/me      | Retorna os dados do usuário logado | Sim (Bearer token) |

### Exemplo — Cadastro
```json
POST /api/auth/cadastro
{
  "nome": "Maria Silva",
  "email": "maria@email.com",
  "senha": "123456",
  "telefone": "14999998888",
  "tipo": "cliente"
}
```

### Exemplo — Login
```json
POST /api/auth/login
{
  "email": "maria@email.com",
  "senha": "123456"
}
```

A resposta traz um `token` que deve ser enviado no header
`Authorization: Bearer <token>` nas rotas protegidas.

## Próximos passos (Sprint 2)

- Cadastro e visualização de estabelecimentos, serviços e profissionais.
=======
# Projeto Integrador 5-Semestre

1) Visão de produtos
Nome provisório: BeautyAgenda

  -Objectivos:
    Desenvolver um aplicativo mobile para facilitar o agendamento de serviços em salões de beleza e barbearias.

  -Problema:
    Muitos estabelecimentos realizam agendamentos manualmente por telefone ou WhatsApp, o que pode causar conflitos de horários, esquecimentos e dificuldade para organizar a agenda.

  -Solução:
    O aplicativo permitirá que os clientes encontrem estabelecimentos próximos, consultem serviços e profissionais, escolham horários disponíveis e realizem agendamentos pelo celular.

  -Público-alvo:
    Clientes de salões de beleza e barbearias;
    Profissionais da área;
    Proprietários e administradores dos estabelecimentos.

  -Principais funcionalidades:
    Cadastro e login;
    Busca por salões e barbearias;
    Localização de estabelecimentos próximos;
    Visualização de serviços e profissionais;
    Agendamento de horários;
    Cancelamento de agendamentos;
    Notificações de lembrete;
    Avaliações;
    Integração com WhatsApp;
    Visualização da localização e rota;
    Gerenciamento da agenda pelos profissionais.

  -Pagamento:
    O pagamento será realizado diretamente no estabelecimento, sem processamento financeiro pelo aplicativo.

2) Backlog do produto
  -Alta prioridade:
    Cadastro e login de usuários;
    Cadastro de salões e barbearias;
    Cadastro de profissionais;
    Cadastro de serviços;
    Visualização de estabelecimentos;
    Busca por localização;
    Agendamento de serviços;
    Verificação de horários disponíveis;
    Cancelamento de agendamentos;
    Visualização da agenda do profissional;
    Impedimento de conflitos de horários.

  -Média prioridade:
    Notificações;
    Avaliações dos estabelecimentos e profissionais;
    Integração com WhatsApp;
    Visualização da localização;
    Rotas até o estabelecimento;
    Histórico de agendamentos.

  -Baixa prioridade:
    Escolha da forma de pagamento;
    Relatórios administrativos;
    Outras funcionalidades que poderão ser adicionadas futuramente.

3) Arquitetura de Solução
  -Aplicativo Mobile:
    Desenvolvido em React Native;
    Responsável pelas telas e interação com os usuários;
    Permite realizar agendamentos, consultar estabelecimentos, avaliações e localização.

  -API:
    Desenvolvida com Node.js e Express;
    Faz a comunicação entre o aplicativo e o banco de dados;
    Controla usuários, serviços, profissionais, agendamentos e avaliações.

  -Banco de Dados:
    SQL;
    Armazena usuários, estabelecimentos, profissionais, serviços, horários, agendamentos e avaliações.

  -Integrações externas:
    GPS: identifica a localização do usuário;
    Mapas: mostra a localização e permite traçar uma rota;
    WhatsApp: permite contato direto com o estabelecimento;
    Notificações: envia lembretes e informações sobre os agendamentos.

  -Funcionamento geral:
  -O usuário utiliza o aplicativo → o aplicativo envia as informações para a API → a API consulta ou altera os dados no banco de dados → o resultado retorna para o aplicativo.
  -Assim fica bem mais enxuto para entregar na atividade, mas ainda deixa claro o que o produto faz, o que será desenvolvido primeiro e como o sistema será estruturado.
>>>>>>> 539cc263e416cf953a812c0682ab1e62334596c1
