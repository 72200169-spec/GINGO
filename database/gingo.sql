CREATE DATABASE IF NOT EXISTS gingo
  CHARACTER SET utf8mb4
  COLLATE utf8mb4_0900_ai_ci;

USE gingo;

CREATE TABLE IF NOT EXISTS users (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  username VARCHAR(50) NOT NULL,
  email VARCHAR(254) NOT NULL,
  phone VARCHAR(30) NULL,
  password_hash VARCHAR(255) NOT NULL,
  role VARCHAR(20) NOT NULL DEFAULT 'user',
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uk_users_username (username),
  UNIQUE KEY uk_users_email (email),
  KEY ix_users_created_at (created_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE IF NOT EXISTS comprobantes (
  id INT NOT NULL AUTO_INCREMENT,
  tipo_doc VARCHAR(2) NOT NULL,
  serie VARCHAR(4) NOT NULL,
  correlativo VARCHAR(10) NOT NULL,
  fecha_emision DATETIME NOT NULL,
  ruc_emisor VARCHAR(11) NOT NULL,
  doc_cliente VARCHAR(15) NOT NULL,
  nombre_cliente VARCHAR(255) NOT NULL,
  total DECIMAL(12, 2) NOT NULL,
  xml_path TEXT NULL,
  cdr_path TEXT NULL,
  pdf_path TEXT NULL,
  estado_sunat VARCHAR(50) NULL DEFAULT 'PENDIENTE',
  created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY ix_comprobantes_fecha_emision (fecha_emision),
  KEY ix_comprobantes_estado_sunat (estado_sunat),
  UNIQUE KEY uk_comprobantes_doc (tipo_doc, serie, correlativo, ruc_emisor)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Insertar usuario administrador por defecto
-- Contraseña por defecto: Admin123!
INSERT IGNORE INTO users (username, email, phone, password_hash, role)
VALUES (
  'admin',
  'admin@gingo.com',
  '000000000',
  '$2a$11$HqFunx.OBs4zIgiP4bknRuaeDm2wK8h.pZn2Sgl8pB0liBoH9SVv.',
  'admin'
);
