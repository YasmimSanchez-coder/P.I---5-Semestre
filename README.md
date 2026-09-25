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
