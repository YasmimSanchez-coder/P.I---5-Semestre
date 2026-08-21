# Projeto Integrador ---5-Semestre

1) Visão de produto
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
