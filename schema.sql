-- =========================================================
-- BeautyAgenda - Schema do Banco de Dados (MySQL)
-- =========================================================

CREATE DATABASE IF NOT EXISTS beautyagenda
  DEFAULT CHARACTER SET utf8mb4
  DEFAULT COLLATE utf8mb4_unicode_ci;

USE beautyagenda;

-- ---------------------------------------------------------
-- USUÁRIOS
-- Tipo: cliente, profissional ou administrador de estabelecimento
-- ---------------------------------------------------------
CREATE TABLE usuarios (
  id INT AUTO_INCREMENT PRIMARY KEY,
  nome VARCHAR(150) NOT NULL,
  email VARCHAR(150) NOT NULL UNIQUE,
  senha_hash VARCHAR(255) NOT NULL,
  telefone VARCHAR(20),
  tipo ENUM('cliente', 'profissional', 'administrador') NOT NULL DEFAULT 'cliente',
  foto_url VARCHAR(255),
  criado_em DATETIME DEFAULT CURRENT_TIMESTAMP,
  atualizado_em DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
) ENGINE=InnoDB;

-- ---------------------------------------------------------
-- ESTABELECIMENTOS (salões e barbearias)
-- ---------------------------------------------------------
CREATE TABLE estabelecimentos (
  id INT AUTO_INCREMENT PRIMARY KEY,
  administrador_id INT NOT NULL,
  nome VARCHAR(150) NOT NULL,
  descricao TEXT,
  endereco VARCHAR(255),
  latitude DECIMAL(10, 8),
  longitude DECIMAL(11, 8),
  telefone VARCHAR(20),
  whatsapp VARCHAR(20),
  foto_url VARCHAR(255),
  horario_abertura TIME,
  horario_fechamento TIME,
  criado_em DATETIME DEFAULT CURRENT_TIMESTAMP,
  atualizado_em DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT fk_estabelecimento_admin
    FOREIGN KEY (administrador_id) REFERENCES usuarios(id)
    ON DELETE CASCADE
) ENGINE=InnoDB;

-- ---------------------------------------------------------
-- PROFISSIONAIS (vinculados a um usuário e a um estabelecimento)
-- ---------------------------------------------------------
CREATE TABLE profissionais (
  id INT AUTO_INCREMENT PRIMARY KEY,
  usuario_id INT NOT NULL,
  estabelecimento_id INT NOT NULL,
  especialidade VARCHAR(150),
  criado_em DATETIME DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_profissional_usuario
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
    ON DELETE CASCADE,
  CONSTRAINT fk_profissional_estabelecimento
    FOREIGN KEY (estabelecimento_id) REFERENCES estabelecimentos(id)
    ON DELETE CASCADE,
  UNIQUE KEY uq_usuario_estabelecimento (usuario_id, estabelecimento_id)
) ENGINE=InnoDB;

-- ---------------------------------------------------------
-- SERVIÇOS oferecidos por um estabelecimento
-- ---------------------------------------------------------
CREATE TABLE servicos (
  id INT AUTO_INCREMENT PRIMARY KEY,
  estabelecimento_id INT NOT NULL,
  nome VARCHAR(150) NOT NULL,
  descricao TEXT,
  preco DECIMAL(10, 2) NOT NULL,
  duracao_minutos INT NOT NULL,
  ativo BOOLEAN DEFAULT TRUE,
  criado_em DATETIME DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_servico_estabelecimento
    FOREIGN KEY (estabelecimento_id) REFERENCES estabelecimentos(id)
    ON DELETE CASCADE
) ENGINE=InnoDB;

-- Relação N:N entre profissionais e os serviços que cada um realiza
CREATE TABLE profissional_servicos (
  profissional_id INT NOT NULL,
  servico_id INT NOT NULL,
  PRIMARY KEY (profissional_id, servico_id),
  CONSTRAINT fk_ps_profissional
    FOREIGN KEY (profissional_id) REFERENCES profissionais(id)
    ON DELETE CASCADE,
  CONSTRAINT fk_ps_servico
    FOREIGN KEY (servico_id) REFERENCES servicos(id)
    ON DELETE CASCADE
) ENGINE=InnoDB;

-- ---------------------------------------------------------
-- HORÁRIOS DE TRABALHO do profissional (disponibilidade recorrente)
-- ---------------------------------------------------------
CREATE TABLE horarios_disponiveis (
  id INT AUTO_INCREMENT PRIMARY KEY,
  profissional_id INT NOT NULL,
  dia_semana TINYINT NOT NULL COMMENT '0=Domingo ... 6=Sábado',
  hora_inicio TIME NOT NULL,
  hora_fim TIME NOT NULL,
  CONSTRAINT fk_horario_profissional
    FOREIGN KEY (profissional_id) REFERENCES profissionais(id)
    ON DELETE CASCADE
) ENGINE=InnoDB;

-- Bloqueios pontuais de agenda (folgas, feriados, indisponibilidade específica)
CREATE TABLE bloqueios_agenda (
  id INT AUTO_INCREMENT PRIMARY KEY,
  profissional_id INT NOT NULL,
  data DATE NOT NULL,
  hora_inicio TIME NOT NULL,
  hora_fim TIME NOT NULL,
  motivo VARCHAR(255),
  CONSTRAINT fk_bloqueio_profissional
    FOREIGN KEY (profissional_id) REFERENCES profissionais(id)
    ON DELETE CASCADE
) ENGINE=InnoDB;

-- ---------------------------------------------------------
-- AGENDAMENTOS
-- Regra de negócio principal: impedir que o mesmo profissional
-- seja agendado duas vezes no mesmo horário.
-- ---------------------------------------------------------
CREATE TABLE agendamentos (
  id INT AUTO_INCREMENT PRIMARY KEY,
  cliente_id INT NOT NULL,
  profissional_id INT NOT NULL,
  servico_id INT NOT NULL,
  data DATE NOT NULL,
  hora_inicio TIME NOT NULL,
  hora_fim TIME NOT NULL,
  status ENUM('pendente', 'confirmado', 'cancelado', 'concluido') NOT NULL DEFAULT 'pendente',
  observacoes TEXT,
  criado_em DATETIME DEFAULT CURRENT_TIMESTAMP,
  atualizado_em DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT fk_agendamento_cliente
    FOREIGN KEY (cliente_id) REFERENCES usuarios(id)
    ON DELETE CASCADE,
  CONSTRAINT fk_agendamento_profissional
    FOREIGN KEY (profissional_id) REFERENCES profissionais(id)
    ON DELETE CASCADE,
  CONSTRAINT fk_agendamento_servico
    FOREIGN KEY (servico_id) REFERENCES servicos(id)
    ON DELETE CASCADE,
  -- Impede duas linhas idênticas de profissional+data+hora_inicio (proteção extra
  -- de banco; a validação de sobreposição de horários é feita na API)
  UNIQUE KEY uq_profissional_data_hora (profissional_id, data, hora_inicio)
) ENGINE=InnoDB;

-- ---------------------------------------------------------
-- AVALIAÇÕES de estabelecimentos e profissionais
-- ---------------------------------------------------------
CREATE TABLE avaliacoes (
  id INT AUTO_INCREMENT PRIMARY KEY,
  agendamento_id INT NOT NULL,
  cliente_id INT NOT NULL,
  estabelecimento_id INT NOT NULL,
  profissional_id INT,
  nota TINYINT NOT NULL COMMENT '1 a 5',
  comentario TEXT,
  criado_em DATETIME DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_avaliacao_agendamento
    FOREIGN KEY (agendamento_id) REFERENCES agendamentos(id)
    ON DELETE CASCADE,
  CONSTRAINT fk_avaliacao_cliente
    FOREIGN KEY (cliente_id) REFERENCES usuarios(id)
    ON DELETE CASCADE,
  CONSTRAINT fk_avaliacao_estabelecimento
    FOREIGN KEY (estabelecimento_id) REFERENCES estabelecimentos(id)
    ON DELETE CASCADE,
  CONSTRAINT fk_avaliacao_profissional
    FOREIGN KEY (profissional_id) REFERENCES profissionais(id)
    ON DELETE SET NULL,
  CONSTRAINT chk_nota CHECK (nota BETWEEN 1 AND 5),
  UNIQUE KEY uq_avaliacao_agendamento (agendamento_id)
) ENGINE=InnoDB;

-- ---------------------------------------------------------
-- NOTIFICAÇÕES (lembretes de agendamento, etc.)
-- ---------------------------------------------------------
CREATE TABLE notificacoes (
  id INT AUTO_INCREMENT PRIMARY KEY,
  usuario_id INT NOT NULL,
  agendamento_id INT,
  titulo VARCHAR(150) NOT NULL,
  mensagem TEXT NOT NULL,
  lida BOOLEAN DEFAULT FALSE,
  criado_em DATETIME DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_notificacao_usuario
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id)
    ON DELETE CASCADE,
  CONSTRAINT fk_notificacao_agendamento
    FOREIGN KEY (agendamento_id) REFERENCES agendamentos(id)
    ON DELETE SET NULL
) ENGINE=InnoDB;

-- ---------------------------------------------------------
-- ÍNDICES adicionais para consultas frequentes
-- ---------------------------------------------------------
CREATE INDEX idx_estabelecimentos_localizacao ON estabelecimentos(latitude, longitude);
CREATE INDEX idx_agendamentos_profissional_data ON agendamentos(profissional_id, data);
CREATE INDEX idx_agendamentos_cliente ON agendamentos(cliente_id);
CREATE INDEX idx_servicos_estabelecimento ON servicos(estabelecimento_id);
