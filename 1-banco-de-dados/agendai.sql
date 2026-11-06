-- =========================================================
-- AgendAí - Estrutura do banco de dados (MySQL 8+)
-- ATENÇÃO: recria o banco do zero (apaga dados existentes).
-- =========================================================

SET NAMES utf8mb4;

DROP DATABASE IF EXISTS agendai;
CREATE DATABASE agendai
  DEFAULT CHARACTER SET utf8mb4
  DEFAULT COLLATE utf8mb4_unicode_ci;

USE agendai;

-- Usuários: cliente, profissional ou administrador de estabelecimento
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

-- Salões e barbearias
CREATE TABLE estabelecimentos (
  id INT AUTO_INCREMENT PRIMARY KEY,
  administrador_id INT NOT NULL,
  nome VARCHAR(150) NOT NULL,
  descricao TEXT,
  endereco VARCHAR(255),
  latitude DECIMAL(10, 8),
  longitude DECIMAL(11, 8),
  telefone VARCHAR(20),
  whatsapp VARCHAR(20) COMMENT 'Somente dígitos, com DDI (ex.: 5511999990001)',
  foto_url VARCHAR(255),
  horario_abertura TIME,
  horario_fechamento TIME,
  criado_em DATETIME DEFAULT CURRENT_TIMESTAMP,
  atualizado_em DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  CONSTRAINT fk_estabelecimento_admin
    FOREIGN KEY (administrador_id) REFERENCES usuarios(id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- Profissionais: um usuário vinculado a um estabelecimento
CREATE TABLE profissionais (
  id INT AUTO_INCREMENT PRIMARY KEY,
  usuario_id INT NOT NULL,
  estabelecimento_id INT NOT NULL,
  especialidade VARCHAR(150),
  criado_em DATETIME DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_profissional_usuario
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE,
  CONSTRAINT fk_profissional_estabelecimento
    FOREIGN KEY (estabelecimento_id) REFERENCES estabelecimentos(id) ON DELETE CASCADE,
  UNIQUE KEY uq_usuario_estabelecimento (usuario_id, estabelecimento_id)
) ENGINE=InnoDB;

-- Serviços oferecidos por um estabelecimento (categoria alimenta os filtros do app)
CREATE TABLE servicos (
  id INT AUTO_INCREMENT PRIMARY KEY,
  estabelecimento_id INT NOT NULL,
  nome VARCHAR(150) NOT NULL,
  descricao TEXT,
  categoria VARCHAR(40) NOT NULL DEFAULT 'Outros' COMMENT 'Cabelo, Unhas, Estética, Barba...',
  preco DECIMAL(10, 2) NOT NULL,
  duracao_minutos INT NOT NULL,
  ativo BOOLEAN DEFAULT TRUE,
  criado_em DATETIME DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_servico_estabelecimento
    FOREIGN KEY (estabelecimento_id) REFERENCES estabelecimentos(id) ON DELETE CASCADE,
  CONSTRAINT chk_servico_duracao CHECK (duracao_minutos > 0)
) ENGINE=InnoDB;

-- Quais serviços cada profissional realiza (N:N)
CREATE TABLE profissional_servicos (
  profissional_id INT NOT NULL,
  servico_id INT NOT NULL,
  PRIMARY KEY (profissional_id, servico_id),
  CONSTRAINT fk_ps_profissional
    FOREIGN KEY (profissional_id) REFERENCES profissionais(id) ON DELETE CASCADE,
  CONSTRAINT fk_ps_servico
    FOREIGN KEY (servico_id) REFERENCES servicos(id) ON DELETE CASCADE
) ENGINE=InnoDB;

-- Expediente semanal do profissional (pode haver mais de uma janela por dia)
CREATE TABLE horarios_disponiveis (
  id INT AUTO_INCREMENT PRIMARY KEY,
  profissional_id INT NOT NULL,
  dia_semana TINYINT NOT NULL COMMENT '0=Domingo ... 6=Sábado',
  hora_inicio TIME NOT NULL,
  hora_fim TIME NOT NULL,
  CONSTRAINT fk_horario_profissional
    FOREIGN KEY (profissional_id) REFERENCES profissionais(id) ON DELETE CASCADE,
  CONSTRAINT chk_horario_dia CHECK (dia_semana BETWEEN 0 AND 6),
  CONSTRAINT chk_horario_intervalo CHECK (hora_fim > hora_inicio)
) ENGINE=InnoDB;

-- Bloqueios pontuais (folga, almoço, compromisso...)
CREATE TABLE bloqueios_agenda (
  id INT AUTO_INCREMENT PRIMARY KEY,
  profissional_id INT NOT NULL,
  data DATE NOT NULL,
  hora_inicio TIME NOT NULL,
  hora_fim TIME NOT NULL,
  motivo VARCHAR(255),
  CONSTRAINT fk_bloqueio_profissional
    FOREIGN KEY (profissional_id) REFERENCES profissionais(id) ON DELETE CASCADE,
  CONSTRAINT chk_bloqueio_intervalo CHECK (hora_fim > hora_inicio)
) ENGINE=InnoDB;

-- Agendamentos
-- Não há UNIQUE em (profissional, data, hora): isso impediria reagendar um
-- horário cujo agendamento anterior foi cancelado. A prevenção de conflitos
-- é feita pela API, dentro de uma transação que trava a linha do profissional.
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
    FOREIGN KEY (cliente_id) REFERENCES usuarios(id) ON DELETE CASCADE,
  CONSTRAINT fk_agendamento_profissional
    FOREIGN KEY (profissional_id) REFERENCES profissionais(id) ON DELETE CASCADE,
  CONSTRAINT fk_agendamento_servico
    FOREIGN KEY (servico_id) REFERENCES servicos(id) ON DELETE CASCADE,
  CONSTRAINT chk_agendamento_intervalo CHECK (hora_fim > hora_inicio)
) ENGINE=InnoDB;

-- Avaliações (uma por agendamento concluído)
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
    FOREIGN KEY (agendamento_id) REFERENCES agendamentos(id) ON DELETE CASCADE,
  CONSTRAINT fk_avaliacao_cliente
    FOREIGN KEY (cliente_id) REFERENCES usuarios(id) ON DELETE CASCADE,
  CONSTRAINT fk_avaliacao_estabelecimento
    FOREIGN KEY (estabelecimento_id) REFERENCES estabelecimentos(id) ON DELETE CASCADE,
  CONSTRAINT fk_avaliacao_profissional
    FOREIGN KEY (profissional_id) REFERENCES profissionais(id) ON DELETE SET NULL,
  CONSTRAINT chk_nota CHECK (nota BETWEEN 1 AND 5),
  UNIQUE KEY uq_avaliacao_agendamento (agendamento_id)
) ENGINE=InnoDB;

-- Notificações internas: agendamentos, lembretes (gerados automaticamente) e promoções
CREATE TABLE notificacoes (
  id INT AUTO_INCREMENT PRIMARY KEY,
  usuario_id INT NOT NULL,
  agendamento_id INT,
  tipo ENUM('agendamento', 'lembrete', 'promocao') NOT NULL DEFAULT 'agendamento',
  titulo VARCHAR(150) NOT NULL,
  mensagem TEXT NOT NULL,
  lida BOOLEAN DEFAULT FALSE,
  criado_em DATETIME DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT fk_notificacao_usuario
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE CASCADE,
  CONSTRAINT fk_notificacao_agendamento
    FOREIGN KEY (agendamento_id) REFERENCES agendamentos(id) ON DELETE SET NULL
) ENGINE=InnoDB;

-- Índices para as consultas mais frequentes
CREATE INDEX idx_estabelecimentos_localizacao ON estabelecimentos(latitude, longitude);
CREATE INDEX idx_agendamentos_profissional_data ON agendamentos(profissional_id, data, status);
CREATE INDEX idx_agendamentos_cliente ON agendamentos(cliente_id, data);
CREATE INDEX idx_servicos_estabelecimento ON servicos(estabelecimento_id, categoria);
CREATE INDEX idx_notificacoes_usuario ON notificacoes(usuario_id, lida);

-- ---------- DADOS DE TESTE (senha de todos: 123456) ----------
SET @senha = '$2a$10$abcdefghijklmnopqrstuuxD0vCwu/f2ahNVJLL35ZYA2xuiRyPH6';

INSERT INTO usuarios (id, nome, email, senha_hash, telefone, tipo) VALUES
  (1, 'Dono dos Salões',   'admin@agendai.com',    @senha, '11999990000', 'administrador'),
  (2, 'Ana Souza',         'cliente@teste.com',    @senha, '11988887777', 'cliente'),
  (3, 'Fernanda Martins',  'fernanda@teste.com',   @senha, '11988886666', 'cliente'),
  (4, 'Beatriz Ferreira',  'beatriz@teste.com',    @senha, '11988885555', 'cliente'),
  (5, 'Ana Lima',          'ana@agendai.com',      @senha, '11977770005', 'profissional'),
  (6, 'Bruno Teixeira',    'bruno@agendai.com',    @senha, '11977770006', 'profissional'),
  (7, 'Marcos Vieira',     'marcos@agendai.com',   @senha, '11977770007', 'profissional'),
  (8, 'Juliana Martins',   'juliana@agendai.com',  @senha, '11977770008', 'profissional'),
  (9, 'Rafael Costa',      'rafael@agendai.com',   @senha, '11977770009', 'profissional');

INSERT INTO estabelecimentos
  (id, administrador_id, nome, descricao, endereco, latitude, longitude, telefone, whatsapp, horario_abertura, horario_fechamento) VALUES
  (1, 1, 'Studio Lumière',
      'Cabelos com acabamento de passarela: corte, coloração, hidratação e tratamentos.',
      'Alameda Lorena, 1200 - Jardins, São Paulo - SP',
      -23.56550000, -46.65550000, '1133330001', '5511999990001', '09:00:00', '18:00:00'),
  (2, 1, 'Noir Salon',
      'Salão autoral em Pinheiros, especializado em coloração e cortes femininos.',
      'Rua dos Pinheiros, 800 - Pinheiros, São Paulo - SP',
      -23.56700000, -46.69250000, '1133330002', '5511999990002', '09:00:00', '18:00:00'),
  (3, 1, 'Atelier Rosa',
      'Unhas e estética em um ambiente aconchegante na Vila Madalena.',
      'Rua Harmonia, 300 - Vila Madalena, São Paulo - SP',
      -23.55600000, -46.69100000, '1133330003', '5511999990003', '09:00:00', '18:00:00'),
  (4, 1, 'Barbearia Imperial',
      'Barbearia clássica: corte, barba com toalha quente e sobrancelha.',
      'Rua Augusta, 500 - Consolação, São Paulo - SP',
      -23.55300000, -46.65220000, '1133330004', '5511999990004', '09:00:00', '18:00:00');

INSERT INTO profissionais (id, usuario_id, estabelecimento_id, especialidade) VALUES
  (1, 5, 1, 'Colorista Sênior'),
  (2, 6, 1, 'Cortes e escovas'),
  (3, 7, 2, 'Cabeleireiro'),
  (4, 8, 3, 'Manicure e estética'),
  (5, 9, 4, 'Barbeiro');

INSERT INTO servicos (id, estabelecimento_id, nome, descricao, categoria, preco, duracao_minutos) VALUES
  ( 1, 1, 'Corte + Escova',        'Corte personalizado com escova modeladora.', 'Cabelo',   120.00,  60),
  ( 2, 1, 'Coloração Completa',    'Coloração completa com produtos premium.',   'Cabelo',   380.00, 180),
  ( 3, 1, 'Hidratação Premium',    'Hidratação profunda com finalização.',       'Cabelo',   180.00,  90),
  ( 4, 1, 'Botox Capilar',         'Redução de volume e brilho intenso.',        'Cabelo',   260.00, 120),
  ( 5, 1, 'Progressiva',           'Alinhamento completo dos fios.',             'Cabelo',   450.00, 240),
  ( 6, 2, 'Corte Feminino',        'Corte com lavagem e finalização.',           'Cabelo',    90.00,  45),
  ( 7, 2, 'Coloração',             'Coloração com consultoria de tom.',          'Cabelo',   280.00, 150),
  ( 8, 2, 'Design de Sobrancelha', 'Design com henna opcional.',                 'Estética',  45.00,  30),
  ( 9, 3, 'Manicure & Pedicure',   'Cuidado completo para mãos e pés.',          'Unhas',     95.00,  90),
  (10, 3, 'Esmaltação em Gel',     'Esmaltação em gel com longa duração.',       'Unhas',     80.00,  60),
  (11, 3, 'Limpeza de Pele',       'Limpeza profunda e hidratação.',             'Estética', 130.00,  60),
  (12, 4, 'Corte Masculino',       'Corte na máquina ou na tesoura.',            'Barba',     55.00,  30),
  (13, 4, 'Barba',                 'Barba com toalha quente.',                   'Barba',     40.00,  30),
  (14, 4, 'Corte + Barba',         'Combo corte e barba.',                       'Barba',     85.00,  60);

INSERT INTO profissional_servicos (profissional_id, servico_id) VALUES
  (1, 1), (1, 2), (1, 3), (1, 4), (1, 5),
  (2, 1), (2, 3),
  (3, 6), (3, 7), (3, 8),
  (4, 9), (4, 10), (4, 11),
  (5, 12), (5, 13), (5, 14);

-- Expediente: segunda a sábado, 09:00-12:00 e 13:00-18:00 (domingo fechado)
INSERT INTO horarios_disponiveis (profissional_id, dia_semana, hora_inicio, hora_fim)
SELECT p.id, d.dia, j.ini, j.fim
FROM profissionais p
CROSS JOIN (SELECT 1 AS dia UNION ALL SELECT 2 UNION ALL SELECT 3
            UNION ALL SELECT 4 UNION ALL SELECT 5 UNION ALL SELECT 6) d
CROSS JOIN (SELECT '09:00:00' AS ini, '12:00:00' AS fim
            UNION ALL SELECT '13:00:00', '18:00:00') j;

-- Agendamentos (ids 1-3 e 9-11: concluídos e avaliados | 4-5: futuros da cliente | 6-8: agenda de hoje)
INSERT INTO agendamentos
  (id, cliente_id, profissional_id, servico_id, data, hora_inicio, hora_fim, status) VALUES
  ( 1, 2, 3,  7, DATE_SUB(CURDATE(), INTERVAL 12 DAY), '09:00:00', '11:30:00', 'concluido'),
  ( 2, 2, 4,  9, DATE_SUB(CURDATE(), INTERVAL 15 DAY), '16:00:00', '17:30:00', 'concluido'),
  ( 3, 2, 5, 14, DATE_SUB(CURDATE(), INTERVAL 5 DAY),  '11:00:00', '12:00:00', 'concluido'),
  ( 4, 2, 1,  1, DATE_ADD(CURDATE(), INTERVAL 1 DAY),  '14:30:00', '15:30:00', 'confirmado'),
  ( 5, 2, 2,  3, DATE_ADD(CURDATE(), INTERVAL 3 DAY),  '10:00:00', '11:30:00', 'pendente'),
  ( 6, 3, 1,  1, CURDATE(),                            '09:00:00', '10:00:00', 'confirmado'),
  ( 7, 4, 1,  3, CURDATE(),                            '10:30:00', '12:00:00', 'pendente'),
  ( 8, 3, 1,  1, CURDATE(),                            '15:00:00', '16:00:00', 'pendente'),
  ( 9, 3, 1,  1, DATE_SUB(CURDATE(), INTERVAL 20 DAY), '10:00:00', '11:00:00', 'concluido'),
  (10, 4, 2,  3, DATE_SUB(CURDATE(), INTERVAL 9 DAY),  '14:00:00', '15:30:00', 'concluido'),
  (11, 4, 1,  2, DATE_SUB(CURDATE(), INTERVAL 30 DAY), '09:00:00', '12:00:00', 'concluido');

INSERT INTO bloqueios_agenda (profissional_id, data, hora_inicio, hora_fim, motivo) VALUES
  (1, CURDATE(), '13:00:00', '14:00:00', 'Almoço');

INSERT INTO avaliacoes (agendamento_id, cliente_id, estabelecimento_id, profissional_id, nota, comentario) VALUES
  ( 1, 2, 2, 3, 5, 'Coloração impecável!'),
  ( 2, 2, 3, 4, 4, 'Ótimo atendimento e ambiente.'),
  ( 3, 2, 4, 5, 5, 'Melhor barbearia da região.'),
  ( 9, 3, 1, 1, 5, 'Amei o resultado!'),
  (10, 4, 1, 2, 5, 'Super recomendo!'),
  (11, 4, 1, 1, 4, 'Resultado lindo, valeu cada minuto.');

INSERT INTO notificacoes (usuario_id, agendamento_id, tipo, titulo, mensagem, lida, criado_em) VALUES
  (2, 4,    'lembrete',    'Lembrete de amanhã',
      'Seu agendamento com Ana Lima é amanhã às 14:30. Não esqueça!', FALSE, DATE_SUB(NOW(), INTERVAL 2 HOUR)),
  (2, NULL, 'promocao',    'Oferta especial ✦',
      'Ganhe 20% de desconto na sua próxima hidratação no Studio Lumière. Válido até domingo!', FALSE, DATE_SUB(NOW(), INTERVAL 5 HOUR)),
  (2, 4,    'agendamento', 'Agendamento confirmado',
      'Seu agendamento de Corte + Escova foi confirmado.', TRUE, DATE_SUB(NOW(), INTERVAL 1 DAY)),
  (2, 3,    'agendamento', 'Como foi o serviço?',
      'Avalie sua experiência na Barbearia Imperial. Sua opinião é importante!', TRUE, DATE_SUB(NOW(), INTERVAL 2 DAY)),
  (5, 8,    'agendamento', 'Novo agendamento',
      'Fernanda Martins agendou Corte + Escova para hoje às 15:00. Confirme o atendimento.', FALSE, DATE_SUB(NOW(), INTERVAL 1 HOUR));
