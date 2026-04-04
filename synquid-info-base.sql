-- INSTITUCION

/*INSERT INTO institutions ("Id", "Name", "ContactEmail", "Type", "Timezone", "IsActive", "CreatedAt")
VALUES (gen_random_uuid(), 'Instituto Tecnologico Central', 'admin@itc.com', 1, 'Europe/Madrid', true, CURRENT_TIMESTAMP); */

-- DISPOSITIVOS

/* INSERT INTO devices ("Id", "Name", "InstitutionId", "ApiKeyHash", "Status", "LastHeartbeat", "CreatedAt", "OfflineRecordsPending", "IsActive")
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
     (SELECT "Id" FROM institutions LIMIT 1) i2; */

--



