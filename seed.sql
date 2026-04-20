-- =============================================================
-- SYNQUID - SEED DATA
-- Passwords: todos usan "Password123" -> BCrypt hash
-- $2a$11$K9L1p3j7ZxQ8mRnT2vYu5eWdNsA6bCfGhIjMkOpQrStUvWxYzAb1c
-- =============================================================

BEGIN;

-- =============================================================
-- INSTITUTIONS
-- =============================================================
INSERT INTO institutions (id, name, address, phone, contact_email, type, timezone, is_active, created_at)
VALUES
  ('11111111-0000-0000-0000-000000000001', 'IES Ramón y Cajal',         'Calle Mayor 15, Madrid',         '910001001', 'admin@ramonycajal.es',    0, 'Europe/Madrid', true, NOW()),
  ('11111111-0000-0000-0000-000000000002', 'Colegio San Isidro',        'Av. de la Paz 8, Madrid',        '910001002', 'admin@sanisidro.es',       0, 'Europe/Madrid', true, NOW()),
  ('11111111-0000-0000-0000-000000000003', 'Instituto Cervantes Tech',  'Gran Vía 100, Barcelona',        '930001003', 'admin@cervantestech.es',   0, 'Europe/Madrid', true, NOW());

-- =============================================================
-- USERS
-- Roles: 0=SuperAdmin  1=Admin  2=Profesor  3=Estudiante
-- Password hash para "Password123"
-- =============================================================

-- SuperAdmin
INSERT INTO users (id, email, password_hash, first_name, last_name, role, institution_id, is_active, email_verified, failed_login_attempts, language, created_at)
VALUES
  ('22222222-0000-0000-0000-000000000001', 'superadmin@synquid.dev', '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Super', 'Admin',    0, NULL,                                       true, true, 0, 'es', NOW());

-- Admins
INSERT INTO users (id, email, password_hash, first_name, last_name, role, institution_id, is_active, email_verified, failed_login_attempts, language, created_at)
VALUES
  ('22222222-0000-0000-0000-000000000002', 'admin@ramonycajal.es',    '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Elena',  'Martínez',  1, '11111111-0000-0000-0000-000000000001', true, true, 0, 'es', NOW()),
  ('22222222-0000-0000-0000-000000000003', 'admin@sanisidro.es',      '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Carlos', 'Ruiz',      1, '11111111-0000-0000-0000-000000000002', true, true, 0, 'es', NOW()),
  ('22222222-0000-0000-0000-000000000004', 'admin@cervantestech.es',  '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Marta',  'López',     1, '11111111-0000-0000-0000-000000000003', true, true, 0, 'es', NOW());

-- Profesores (IES Ramón y Cajal)
INSERT INTO users (id, email, password_hash, first_name, last_name, role, institution_id, is_active, email_verified, failed_login_attempts, language, created_at)
VALUES
  ('33333333-0000-0000-0000-000000000001', 'prof.garcia@ramonycajal.es',    '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Antonio',   'García',    2, '11111111-0000-0000-0000-000000000001', true, true, 0, 'es', NOW()),
  ('33333333-0000-0000-0000-000000000002', 'prof.sanchez@ramonycajal.es',   '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Laura',     'Sánchez',   2, '11111111-0000-0000-0000-000000000001', true, true, 0, 'es', NOW()),
  ('33333333-0000-0000-0000-000000000003', 'prof.moreno@ramonycajal.es',    '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Pedro',     'Moreno',    2, '11111111-0000-0000-0000-000000000001', true, true, 0, 'es', NOW());

-- Profesores (San Isidro)
INSERT INTO users (id, email, password_hash, first_name, last_name, role, institution_id, is_active, email_verified, failed_login_attempts, language, created_at)
VALUES
  ('33333333-0000-0000-0000-000000000004', 'prof.jimenez@sanisidro.es',     '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Rosa',      'Jiménez',   2, '11111111-0000-0000-0000-000000000002', true, true, 0, 'es', NOW()),
  ('33333333-0000-0000-0000-000000000005', 'prof.fernandez@sanisidro.es',   '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Jorge',     'Fernández', 2, '11111111-0000-0000-0000-000000000002', true, true, 0, 'es', NOW());

-- Estudiantes (IES Ramón y Cajal)
INSERT INTO users (id, email, password_hash, first_name, last_name, role, institution_id, is_active, email_verified, failed_login_attempts, language, created_at)
VALUES
  ('44444444-0000-0000-0000-000000000001', 'alumno.perez@ramonycajal.es',     '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Miguel',    'Pérez',     3, '11111111-0000-0000-0000-000000000001', true, true, 0, 'es', NOW()),
  ('44444444-0000-0000-0000-000000000002', 'alumno.gomez@ramonycajal.es',      '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Sofía',     'Gómez',     3, '11111111-0000-0000-0000-000000000001', true, true, 0, 'es', NOW()),
  ('44444444-0000-0000-0000-000000000003', 'alumno.diaz@ramonycajal.es',       '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Pablo',     'Díaz',      3, '11111111-0000-0000-0000-000000000001', true, true, 0, 'es', NOW()),
  ('44444444-0000-0000-0000-000000000004', 'alumno.torres@ramonycajal.es',     '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Ana',       'Torres',    3, '11111111-0000-0000-0000-000000000001', true, true, 0, 'es', NOW()),
  ('44444444-0000-0000-0000-000000000005', 'alumno.vargas@ramonycajal.es',     '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Lucía',     'Vargas',    3, '11111111-0000-0000-0000-000000000001', true, true, 0, 'es', NOW()),
  ('44444444-0000-0000-0000-000000000006', 'alumno.castillo@ramonycajal.es',   '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'David',     'Castillo',  3, '11111111-0000-0000-0000-000000000001', true, true, 0, 'es', NOW()),
  ('44444444-0000-0000-0000-000000000007', 'alumno.ramos@ramonycajal.es',      '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Carmen',    'Ramos',     3, '11111111-0000-0000-0000-000000000001', true, true, 0, 'es', NOW()),
  ('44444444-0000-0000-0000-000000000008', 'alumno.ortega@ramonycajal.es',     '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Javier',    'Ortega',    3, '11111111-0000-0000-0000-000000000001', true, true, 0, 'es', NOW()),
  ('44444444-0000-0000-0000-000000000009', 'alumno.molina@ramonycajal.es',     '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Natalia',   'Molina',    3, '11111111-0000-0000-0000-000000000001', true, true, 0, 'es', NOW()),
  ('44444444-0000-0000-0000-000000000010', 'alumno.delgado@ramonycajal.es',    '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Sergio',    'Delgado',   3, '11111111-0000-0000-0000-000000000001', true, true, 0, 'es', NOW());

-- Estudiantes (San Isidro)
INSERT INTO users (id, email, password_hash, first_name, last_name, role, institution_id, is_active, email_verified, failed_login_attempts, language, created_at)
VALUES
  ('44444444-0000-0000-0000-000000000011', 'alumno.herrera@sanisidro.es',    '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Patricia',  'Herrera',   3, '11111111-0000-0000-0000-000000000002', true, true, 0, 'es', NOW()),
  ('44444444-0000-0000-0000-000000000012', 'alumno.medina@sanisidro.es',     '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Rubén',     'Medina',    3, '11111111-0000-0000-0000-000000000002', true, true, 0, 'es', NOW()),
  ('44444444-0000-0000-0000-000000000013', 'alumno.leon@sanisidro.es',       '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Isabel',    'León',      3, '11111111-0000-0000-0000-000000000002', true, true, 0, 'es', NOW()),
  ('44444444-0000-0000-0000-000000000014', 'alumno.cortez@sanisidro.es',     '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Andrés',    'Cortez',    3, '11111111-0000-0000-0000-000000000002', true, true, 0, 'es', NOW()),
  ('44444444-0000-0000-0000-000000000015', 'alumno.reyes@sanisidro.es',      '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Cristina',  'Reyes',     3, '11111111-0000-0000-0000-000000000002', true, true, 0, 'es', NOW()),
  ('44444444-0000-0000-0000-000000000016', 'alumno.silva@sanisidro.es',      '$2a$11$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi', 'Fernando',  'Silva',     3, '11111111-0000-0000-0000-000000000002', true, true, 0, 'es', NOW());

-- =============================================================
-- GRUPOS
-- Level = descripción del aula / horario
-- =============================================================
INSERT INTO groups (id, name, level, institution_id, professor_id, is_active, created_at)
VALUES
  -- Ramón y Cajal
  ('55555555-0000-0000-0000-000000000001', '1º DAW A',   'Aula 101 - Lun/Mié/Vie 08:00-10:00', '11111111-0000-0000-0000-000000000001', '33333333-0000-0000-0000-000000000001', true, NOW()),
  ('55555555-0000-0000-0000-000000000002', '2º DAW A',   'Aula 102 - Mar/Jue 10:00-13:00',     '11111111-0000-0000-0000-000000000001', '33333333-0000-0000-0000-000000000001', true, NOW()),
  ('55555555-0000-0000-0000-000000000003', '1º ASIR B',  'Aula 203 - Lun/Mié 12:00-14:00',     '11111111-0000-0000-0000-000000000001', '33333333-0000-0000-0000-000000000002', true, NOW()),
  ('55555555-0000-0000-0000-000000000004', '2º SMR A',   'Aula 301 - Mar/Jue/Vie 08:00-11:00', '11111111-0000-0000-0000-000000000001', '33333333-0000-0000-0000-000000000003', true, NOW()),
  -- San Isidro
  ('55555555-0000-0000-0000-000000000005', '3º ESO A',   'Aula A1 - Todos los días 09:00-14:00','11111111-0000-0000-0000-000000000002', '33333333-0000-0000-0000-000000000004', true, NOW()),
  ('55555555-0000-0000-0000-000000000006', '4º ESO B',   'Aula B2 - Todos los días 09:00-14:00','11111111-0000-0000-0000-000000000002', '33333333-0000-0000-0000-000000000005', true, NOW());

-- =============================================================
-- HORARIOS (Schedules)
-- DayOfWeek: 0=Dom 1=Lun 2=Mar 3=Mié 4=Jue 5=Vie 6=Sáb
-- =============================================================
INSERT INTO schedules (id, group_id, day_of_week, start_time, end_time, late_tolerance_minutes, is_active)
VALUES
  -- 1º DAW A (Lun/Mié/Vie 08:00-10:00)
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000001', 1, '08:00', '10:00', 10, true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000001', 3, '08:00', '10:00', 10, true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000001', 5, '08:00', '10:00', 10, true),
  -- 2º DAW A (Mar/Jue 10:00-13:00)
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000002', 2, '10:00', '13:00', 10, true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000002', 4, '10:00', '13:00', 10, true),
  -- 1º ASIR B (Lun/Mié 12:00-14:00)
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000003', 1, '12:00', '14:00', 10, true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000003', 3, '12:00', '14:00', 10, true),
  -- 2º SMR A (Mar/Jue/Vie 08:00-11:00)
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000004', 2, '08:00', '11:00', 15, true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000004', 4, '08:00', '11:00', 15, true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000004', 5, '08:00', '11:00', 15, true),
  -- 3º ESO A (Lun-Vie 09:00-14:00)
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000005', 1, '09:00', '14:00', 10, true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000005', 2, '09:00', '14:00', 10, true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000005', 3, '09:00', '14:00', 10, true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000005', 4, '09:00', '14:00', 10, true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000005', 5, '09:00', '14:00', 10, true),
  -- 4º ESO B (Lun-Vie 09:00-14:00)
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000006', 1, '09:00', '14:00', 10, true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000006', 2, '09:00', '14:00', 10, true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000006', 3, '09:00', '14:00', 10, true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000006', 4, '09:00', '14:00', 10, true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000006', 5, '09:00', '14:00', 10, true);

-- =============================================================
-- MIEMBROS DE GRUPO (GroupMembers = estudiantes en grupos)
-- =============================================================

-- 1º DAW A: alumnos 1-4
INSERT INTO group_members (id, group_id, user_id, joined_at, is_active)
VALUES
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000001', '44444444-0000-0000-0000-000000000001', NOW(), true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000001', '44444444-0000-0000-0000-000000000002', NOW(), true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000001', '44444444-0000-0000-0000-000000000003', NOW(), true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000001', '44444444-0000-0000-0000-000000000004', NOW(), true);

-- 2º DAW A: alumnos 3-7 (3 y 4 están en dos grupos)
INSERT INTO group_members (id, group_id, user_id, joined_at, is_active)
VALUES
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000002', '44444444-0000-0000-0000-000000000005', NOW(), true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000002', '44444444-0000-0000-0000-000000000006', NOW(), true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000002', '44444444-0000-0000-0000-000000000007', NOW(), true);

-- 1º ASIR B: alumnos 7-9
INSERT INTO group_members (id, group_id, user_id, joined_at, is_active)
VALUES
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000003', '44444444-0000-0000-0000-000000000008', NOW(), true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000003', '44444444-0000-0000-0000-000000000009', NOW(), true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000003', '44444444-0000-0000-0000-000000000010', NOW(), true);

-- 2º SMR A: alumnos 1,2 (turno tarde, comparten)
INSERT INTO group_members (id, group_id, user_id, joined_at, is_active)
VALUES
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000004', '44444444-0000-0000-0000-000000000001', NOW(), true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000004', '44444444-0000-0000-0000-000000000002', NOW(), true);

-- 3º ESO A: alumnos San Isidro 11-13
INSERT INTO group_members (id, group_id, user_id, joined_at, is_active)
VALUES
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000005', '44444444-0000-0000-0000-000000000011', NOW(), true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000005', '44444444-0000-0000-0000-000000000012', NOW(), true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000005', '44444444-0000-0000-0000-000000000013', NOW(), true);

-- 4º ESO B: alumnos San Isidro 14-16
INSERT INTO group_members (id, group_id, user_id, joined_at, is_active)
VALUES
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000006', '44444444-0000-0000-0000-000000000014', NOW(), true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000006', '44444444-0000-0000-0000-000000000015', NOW(), true),
  (gen_random_uuid(), '55555555-0000-0000-0000-000000000006', '44444444-0000-0000-0000-000000000016', NOW(), true);

-- =============================================================
-- DISPOSITIVOS NFC
-- ApiKeyHash = hash de "synquid-device-key-{n}"
-- Usamos texto plano como placeholder (cámbialo con BCrypt si lo necesitas)
-- =============================================================
INSERT INTO devices (id, name, location, institution_id, api_key_hash, status, last_heartbeat, firmware_version, cpu_temp, memory_usage_mb, offline_records_pending, is_active, created_at)
VALUES
  ('66666666-0000-0000-0000-000000000001', 'Lector Entrada Principal',  'Puerta principal planta baja',   '11111111-0000-0000-0000-000000000001', 'hash-key-placeholder-1', 0, NOW() - INTERVAL '5 minutes',  'v1.2.0', 42.5, 128, 0, true, NOW()),
  ('66666666-0000-0000-0000-000000000002', 'Lector Aula 101',           'Aula 101 - 1ª planta',           '11111111-0000-0000-0000-000000000001', 'hash-key-placeholder-2', 0, NOW() - INTERVAL '3 minutes',  'v1.2.0', 38.1, 128, 0, true, NOW()),
  ('66666666-0000-0000-0000-000000000003', 'Lector Aula 102',           'Aula 102 - 1ª planta',           '11111111-0000-0000-0000-000000000001', 'hash-key-placeholder-3', 0, NOW() - INTERVAL '10 minutes', 'v1.1.5', 41.0, 96,  0, true, NOW()),
  ('66666666-0000-0000-0000-000000000004', 'Lector Biblioteca',         'Biblioteca - planta baja',       '11111111-0000-0000-0000-000000000001', 'hash-key-placeholder-4', 1, NOW() - INTERVAL '2 hours',    'v1.1.5', 0.0,  0,   3, true, NOW()),
  ('66666666-0000-0000-0000-000000000005', 'Lector Entrada San Isidro', 'Puerta principal',               '11111111-0000-0000-0000-000000000002', 'hash-key-placeholder-5', 0, NOW() - INTERVAL '1 minute',   'v1.2.1', 39.8, 128, 0, true, NOW()),
  ('66666666-0000-0000-0000-000000000006', 'Lector Aula A1',            'Aula A1 - San Isidro',           '11111111-0000-0000-0000-000000000002', 'hash-key-placeholder-6', 0, NOW() - INTERVAL '7 minutes',  'v1.2.1', 37.2, 128, 0, true, NOW());

-- =============================================================
-- TARJETAS NFC
-- HashUid = simulación de UID hasheado (en producción iría el hash real)
-- Salt = requerido por schema
-- =============================================================
INSERT INTO nfc_cards (id, user_id, hash_uid, salt, card_type, is_active, created_at)
VALUES
  ('77777777-0000-0000-0000-000000000001', '44444444-0000-0000-0000-000000000001', 'uid-hash-a1b2c3d4', 'salt01', 0, true, NOW()),
  ('77777777-0000-0000-0000-000000000002', '44444444-0000-0000-0000-000000000002', 'uid-hash-e5f6g7h8', 'salt02', 0, true, NOW()),
  ('77777777-0000-0000-0000-000000000003', '44444444-0000-0000-0000-000000000003', 'uid-hash-i9j0k1l2', 'salt03', 0, true, NOW()),
  ('77777777-0000-0000-0000-000000000004', '44444444-0000-0000-0000-000000000004', 'uid-hash-m3n4o5p6', 'salt04', 0, true, NOW()),
  ('77777777-0000-0000-0000-000000000005', '44444444-0000-0000-0000-000000000005', 'uid-hash-q7r8s9t0', 'salt05', 0, true, NOW()),
  ('77777777-0000-0000-0000-000000000006', '44444444-0000-0000-0000-000000000006', 'uid-hash-u1v2w3x4', 'salt06', 0, true, NOW()),
  -- Alumnos 7-10 sin NFC (para probar ese caso)
  ('77777777-0000-0000-0000-000000000011', '44444444-0000-0000-0000-000000000011', 'uid-hash-san01aaaa', 'salt11', 0, true, NOW()),
  ('77777777-0000-0000-0000-000000000012', '44444444-0000-0000-0000-000000000012', 'uid-hash-san02bbbb', 'salt12', 0, true, NOW()),
  ('77777777-0000-0000-0000-000000000013', '44444444-0000-0000-0000-000000000013', 'uid-hash-san03cccc', 'salt13', 0, true, NOW()),
  ('77777777-0000-0000-0000-000000000014', '44444444-0000-0000-0000-000000000014', 'uid-hash-san04dddd', 'salt14', 0, true, NOW());
  -- alumnos 15 y 16 sin tarjeta (null en user, para probar nfcCardId=null)

-- =============================================================
-- REGISTROS DE ASISTENCIA (últimos 7 días)
-- Status: 0=Presente 1=Ausente 2=Justificado 3=Tarde
-- Type:   0=NFC      1=HCE     2=Manual
-- =============================================================
INSERT INTO attendance_records (id, user_id, device_id, timestamp_utc, timestamp_local, type, status, nfc_hash, is_synced, created_at)
VALUES
  -- Hoy
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000001', '66666666-0000-0000-0000-000000000002', NOW() - INTERVAL '2 hours', NOW() - INTERVAL '2 hours', 0, 0, 'uid-hash-a1b2c3d4', true, NOW()),
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000002', '66666666-0000-0000-0000-000000000002', NOW() - INTERVAL '2 hours', NOW() - INTERVAL '2 hours', 0, 0, 'uid-hash-e5f6g7h8', true, NOW()),
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000003', '66666666-0000-0000-0000-000000000002', NOW() - INTERVAL '2 hours', NOW() - INTERVAL '2 hours', 0, 3, 'uid-hash-i9j0k1l2', true, NOW()),
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000004', '66666666-0000-0000-0000-000000000002', NOW() - INTERVAL '2 hours', NOW() - INTERVAL '2 hours', 0, 1, NULL,               true, NOW()),

  -- Ayer
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000001', '66666666-0000-0000-0000-000000000002', NOW() - INTERVAL '1 day 2 hours', NOW() - INTERVAL '1 day 2 hours', 0, 0, 'uid-hash-a1b2c3d4', true, NOW()),
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000002', '66666666-0000-0000-0000-000000000002', NOW() - INTERVAL '1 day 2 hours', NOW() - INTERVAL '1 day 2 hours', 0, 0, 'uid-hash-e5f6g7h8', true, NOW()),
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000003', '66666666-0000-0000-0000-000000000002', NOW() - INTERVAL '1 day 2 hours', NOW() - INTERVAL '1 day 2 hours', 0, 0, 'uid-hash-i9j0k1l2', true, NOW()),

  -- Hace 2 días
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000005', '66666666-0000-0000-0000-000000000003', NOW() - INTERVAL '2 days 3 hours', NOW() - INTERVAL '2 days 3 hours', 0, 0, 'uid-hash-q7r8s9t0', true, NOW()),
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000006', '66666666-0000-0000-0000-000000000003', NOW() - INTERVAL '2 days 3 hours', NOW() - INTERVAL '2 days 3 hours', 0, 3, 'uid-hash-u1v2w3x4', true, NOW()),
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000007', '66666666-0000-0000-0000-000000000003', NOW() - INTERVAL '2 days 3 hours', NOW() - INTERVAL '2 days 3 hours', 2, 2, NULL,               true, NOW()),

  -- San Isidro - hoy
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000011', '66666666-0000-0000-0000-000000000006', NOW() - INTERVAL '3 hours', NOW() - INTERVAL '3 hours', 0, 0, 'uid-hash-san01aaaa', true, NOW()),
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000012', '66666666-0000-0000-0000-000000000006', NOW() - INTERVAL '3 hours', NOW() - INTERVAL '3 hours', 0, 0, 'uid-hash-san02bbbb', true, NOW()),
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000013', '66666666-0000-0000-0000-000000000006', NOW() - INTERVAL '3 hours', NOW() - INTERVAL '3 hours', 0, 1, NULL,                true, NOW()),

  -- Hace 3 días
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000001', '66666666-0000-0000-0000-000000000002', NOW() - INTERVAL '3 days 2 hours', NOW() - INTERVAL '3 days 2 hours', 0, 0, 'uid-hash-a1b2c3d4', true, NOW()),
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000002', '66666666-0000-0000-0000-000000000002', NOW() - INTERVAL '3 days 2 hours', NOW() - INTERVAL '3 days 2 hours', 0, 2, NULL,               true, NOW()),

  -- Manual registrado por profesor
  (gen_random_uuid(), '44444444-0000-0000-0000-000000000004', NULL, NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days', 2, 0, NULL, true, NOW());

  

COMMIT;

-- =============================================================
-- RESUMEN DE DATOS CREADOS
-- =============================================================
-- Instituciones : 3
-- SuperAdmin    : 1   superadmin@synquid.dev   / Password123
-- Admins        : 3   admin@ramonycajal.es     / Password123
--                     admin@sanisidro.es       / Password123
--                     admin@cervantestech.es   / Password123
-- Profesores    : 5   prof.garcia@ramonycajal.es   (grupos: 1º DAW A, 2º DAW A)
--                     prof.sanchez@ramonycajal.es  (grupo: 1º ASIR B)
--                     prof.moreno@ramonycajal.es   (grupo: 2º SMR A)
--                     prof.jimenez@sanisidro.es    (grupo: 3º ESO A)
--                     prof.fernandez@sanisidro.es  (grupo: 4º ESO B)
-- Estudiantes   : 16  (10 Ramón y Cajal, 6 San Isidro)
-- Grupos        : 6
-- Dispositivos  : 6   (4 Ramón y Cajal, 2 San Isidro)
-- NFC cards     : 10  (alumnos 7-10 y 15-16 sin tarjeta para test null)
-- Asistencias   : 16  (mezcla de estados y días)
-- =============================================================


UPDATE users 
SET "PasswordHash" = '$2a$11$m9.v2XH1eYdKXJb5rZc8uO23slvtKNRz9cTiMfowtcFwB87lSa6RC'
WHERE "Email" IN (
  'superadmin@synquid.dev',
  'admin@ramonycajal.es',
  'prof.garcia@ramonycajal.es',
  'alumno.perez@ramonycajal.es'
);