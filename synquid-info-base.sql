-- INSTITUCION

INSERT INTO institutions ("Id", "Name", "ContactEmail", "Type", "Timezone", "IsActive", "CreatedAt")
VALUES (gen_random_uuid(), 'Instituto Tecnologico Central', 'admin@itc.com', 1, 'Europe/Madrid', true, CURRENT_TIMESTAMP); 

-- DISPOSITIVOS

INSERT INTO devices ("Id", "Name", "InstitutionId", "ApiKeyHash", "Status", "LastHeartbeat", "CreatedAt", "OfflineRecordsPending", "IsActive")
SELECT 
    gen_random_uuid(),
    'Device-' || i,
    i2."Id",
    'hash_' || i,
    1,
    NOW(),
    NOW(),
    0,
    true
FROM generate_series(1,3) i,
     (SELECT "Id" FROM institutions LIMIT 1) i2; 


-- Estudiantes
INSERT INTO users ("Id", "Email", "PasswordHash", "Role", "InstitutionId", "IsActive", "CreatedAt", "FirstName", "LastName", "EmailVerified", "FailedLoginAttempts")
SELECT 
    gen_random_uuid(),
    'student' || i || '@school.com',
    'hashed_password',
    0,
    i2."Id",
    true,
    NOW(),
    'Student ' || i,
    'Test',
    true,   
    0       -- Setting FailedLoginAttempts to 0
FROM generate_series(1, 60) i,
     (SELECT "Id" FROM institutions LIMIT 1) i2; 

-- Profesores
INSERT INTO users ("Id", "Email", "PasswordHash", "Role", "InstitutionId", "IsActive", "CreatedAt", "FirstName", "LastName", "EmailVerified", "FailedLoginAttempts")
SELECT 
    gen_random_uuid(),
    'teacher' || i || '@school.com',
    'hashed_password',
    1,
    i2."Id",
    true,
    NOW(),
    'Teacher ' || i,
    'Test',
    true,   
    0       -- Setting FailedLoginAttempts to 0
FROM generate_series(1, 3) i,
     (SELECT "Id" FROM institutions LIMIT 1) i2; 


-- Grupos
INSERT INTO groups ("Id", "Name", "Level", "InstitutionId", "CreatedAt", "IsActive", "ProfessorId")
SELECT 
    gen_random_uuid(),
    'Aula ' || i,
    i,
    i2."Id",
    NOW(),
    true,
    u."Id"  -- Assigning the professor's ID
FROM generate_series(1,3) i,
     (SELECT "Id" FROM institutions LIMIT 1) i2,
     (SELECT "Id" FROM users WHERE "Role" = 1 LIMIT 1) u; 

-- miembros de grupos
INSERT INTO group_members ("Id", "GroupId", "UserId", "JoinedAt", "IsActive")
SELECT 
    gen_random_uuid(),
    g."Id",
    u."Id",
    NOW(),
    true
FROM groups g
JOIN LATERAL (
    SELECT "Id" FROM users 
    WHERE "Role" = 0
    ORDER BY random()
    LIMIT 20
) u ON true; 


-- tarjetas por usuario
INSERT INTO nfc_cards ("Id", "UserId", "HashUid", "Salt", "CardType", "HceToken", "IsActive", "CreatedAt")
SELECT 
    gen_random_uuid(),
    "Id",
    md5(random()::text),
    md5(random()::text), -- Generating a random dummy salt
    1,
    md5(random()::text),
    true,
    NOW()
FROM users; 

-- horarios

INSERT INTO schedules ("Id", "GroupId", "DayOfWeek", "StartTime", "EndTime", "LateToleranceMinutes", "IsActive")
SELECT 
    gen_random_uuid(),
    g."Id",
    d,
    '08:00:00',
    '14:00:00',
    15,
    true        -- Setting the schedule to active
FROM groups g,
     generate_series(1,5) d; 






