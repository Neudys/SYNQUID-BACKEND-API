# Synquid Backend API — Documentación Técnica

## Descripción General

Sistema de control de asistencia basado en NFC para instituciones educativas y empresariales. Los dispositivos IoT (Raspberry Pi) leen tarjetas NFC y registran asistencia en tiempo real. Profesores y administradores gestionan grupos, horarios y reportes a través de la API REST.

---

## Stack Tecnológico

| Componente | Tecnología |
|---|---|
| Framework | ASP.NET Core 8 / 9 |
| Base de datos | PostgreSQL 16 |
| ORM | Entity Framework Core 8.0.11 |
| Autenticación | JWT Bearer |
| Hashing contraseñas | BCrypt.Net-Next 4.1.0 |
| Documentación API | Swagger / Swashbuckle 6.9.0 |
| Contenedor DB | Docker |

---

## Arquitectura

Clean Architecture de 4 capas:

```
Synquid.API            → Controllers, DTOs, Program.cs
Synquid.Application    → Interfaces, DTOs compartidos
Synquid.Domain         → Entidades, Constantes
Synquid.Infrastructure → DbContext, Migraciones, Servicios
```

---

## Configuración

### Variables requeridas (`appsettings.development.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=9000;Database=synquid;Username=postgres;Password=postgres"
  },
  "JwtSettings": {
    "SecretKey": "CLAVE_MINIMO_32_CARACTERES",
    "Issuer": "Synquid.API",
    "Audience": "Synquid.Clients",
    "ExpiryMinutes": 1440
  },
  "AdminUser": "correo@gmail.com",
  "AdminPassword": "contraseña_de_aplicacion_gmail",
  "SMTPName": "smtp.gmail.com",
  "SMTPPort": "587"
}
```

### Base de datos con Docker

```bash
docker run -d --name synquid-db \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=synquid \
  -p 9000:5432 \
  postgres:16
```

### Ejecutar el proyecto

```bash
dotnet build
dotnet run --project src/Synquid.API --urls="http://localhost:5000"
```

Las migraciones se aplican automáticamente al iniciar la aplicación.

### Migraciones manuales

```bash
cd src/Synquid.API
dotnet ef database update --project ../Synquid.Infrastructure
```

---

## Roles de Usuario

| Valor | Rol | Descripción |
|---|---|---|
| `0` | SuperAdmin | Control total del sistema |
| `1` | Admin | Administrador de institución |
| `2` | Professor | Profesor / Docente |
| `3` | Student | Estudiante / Alumno |

---

## Entidades del Dominio

### User
Usuarios del sistema. Propiedades: `Id`, `Email`, `PasswordHash`, `FirstName`, `LastName`, `Role`, `InstitutionId`, `GoogleId`, `AvatarUrl`, `Language`, `IsActive`, `EmailVerified`, `FailedLoginAttempts`, `LockedUntil`, `CreatedAt`, `UpdatedAt`.

### Institution
Instituciones educativas o empresariales. `Type`: `0=Educational`, `1=Business`.

### Group
Grupos o clases. Tiene un `ProfessorId` (FK a User) y pertenece a una `Institution`.

### GroupMember
Relación muchos-a-muchos entre `Group` y `User` (estudiantes).

### Schedule
Horarios de clases por grupo. `DayOfWeek` (0-6), `StartTime`, `EndTime`, `LateToleranceMinutes` (default: 10).

### Device
Dispositivos NFC (Raspberry Pi). `Status`: `0=Active`, `1=Disconnected`, `2=Error`. Tiene `ApiKeyHash` para autenticación.

### NfcCard
Tarjetas NFC vinculadas a usuarios. `CardType`: `0=Physical`, `1=HCE`. El UID se almacena hasheado (SHA-256).

### AttendanceRecord
Registro crudo de cada lectura NFC. `Type`: `0=NFC`, `1=HCE`, `2=Manual`. `Status`: `0=Present`, `1=Absent`, `2=Justified`, `3=Late`.

### DailyAttendance
Resumen diario de asistencia por estudiante y horario. Único por `(UserId, ScheduleId, Date)`.

### AuditLog
Log de acciones administrativas (CREATE / UPDATE / DELETE).

### PasswordResetToken
Tokens temporales para recuperación de contraseña.

---

## Endpoints

### Auth — `/api/auth`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| POST | `/Register` | No | Registrar usuario |
| POST | `/login` | No | Login con email/password |
| POST | `/logout` | Sí | Cerrar sesión |
| POST | `/refresh` | No | Refrescar JWT |
| POST | `/forgotPassword` | No | Solicitar reset de contraseña |
| POST | `/resetPassword` | No | Resetear contraseña con token |
| POST | `/validateToken` | No | Validar JWT |
| POST | `/verifyEmail` | No | Verificar email |

### Institutions — `/api/institutions`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/` | Sí | Listar instituciones activas |
| GET | `/{id}` | Sí | Obtener institución |
| POST | `/` | Sí | Crear institución |
| PUT | `/{id}` | Sí | Actualizar institución |
| DELETE | `/{id}` | Sí | Soft delete |
| GET | `/{id}/users` | Sí | Usuarios de la institución |
| POST | `/{id}/users` | Sí | Asignar usuario |
| DELETE | `/{id}/users/{userId}` | Sí | Desasignar usuario |
| GET | `/{id}/devices` | Sí | Dispositivos de la institución |
| GET | `/{id}/attendance` | Sí | Asistencia paginada |

### Users — `/api/user`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/me` | Sí | Perfil del usuario actual |
| GET | `/` | Sí | Listar usuarios (paginado, 20/pág) |
| GET | `/{id}` | Sí | Obtener usuario |
| POST | `/` | Sí | Crear usuario |
| PUT | `/{id}` | Sí | Actualizar usuario |
| DELETE | `/{id}` | Sí | Soft delete |
| PATCH | `/{id}/role` | Sí | Actualizar rol |
| GET | `/{id}/groups` | Sí | Grupos del usuario |
| POST | `/{id}/nfc` | Sí | Asignar tarjeta NFC |
| DELETE | `/{id}/nfc` | Sí | Desasignar tarjeta NFC |

### Groups — `/api/group`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/` | Sí | Listar grupos activos |
| GET | `/{id}` | Sí | Obtener grupo |
| POST | `/` | Sí | Crear grupo |
| PUT | `/{id}` | Sí | Actualizar grupo |
| DELETE | `/{id}` | Sí | Soft delete |
| GET | `/{id}/members` | Sí | Miembros del grupo |
| GET | `/{id}/schedules` | Sí | Horarios del grupo |
| POST | `/{id}/members` | Sí | Añadir miembro |
| DELETE | `/{id}/members/{userId}` | Sí | Remover miembro |

### Devices — `/api/devices`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/` | Sí | Listar dispositivos |
| GET | `/{id}` | Sí | Obtener dispositivo |
| POST | `/` | Sí | Crear dispositivo |
| PUT | `/{id}` | Sí | Actualizar dispositivo |
| DELETE | `/{id}` | Sí | Desactivar dispositivo |
| POST | `/{id}/regenerateKey` | Sí | Regenerar API key |
| POST | `/{id}/heartbeat` | No | Heartbeat del dispositivo |
| PATCH | `/{id}/status` | Sí | Actualizar estado |
| GET | `/{id}/logs` | Sí | Logs paginados |
| GET | `/byInstitution/{institutionId}` | Sí | Dispositivos por institución |

### NFC — `/api/nfc`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/` | Sí | Listar tarjetas NFC |
| POST | `/register` | Sí | Registrar tarjeta NFC |
| GET | `/{uid}` | Sí | Obtener tarjeta por UID |
| POST | `/AssignCard` | No | Asignar tarjeta a usuario |
| PUT | `/{id}` | Sí | Actualizar tarjeta |
| DELETE | `/{id}` | Sí | Eliminar tarjeta |

### Attendance — `/api/attendance`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/All` | Sí | Todos los registros |
| GET | `/{id}` | Sí | Registro por ID |
| PUT | `/{id}` | Sí | Actualizar registro |
| GET | `/export` | Sí | Exportar CSV (rango de fechas) |
| GET | `/stats` | Sí | Estadísticas (rango de fechas) |
| POST | `/Register` | No | Registrar desde dispositivo NFC |
| POST | `/sync` | Sí | Sincronización batch desde dispositivo |
| POST | `/manual` | Sí | Registro manual (profesor) |
| GET | `/today` | Sí | Asistencia de hoy por grupo |
| GET | `/history` | Sí | Historial del grupo (profesor) |
| GET | `/myHistory` | Sí | Mi historial personal |
| PUT | `/daily` | Sí | Crear/actualizar asistencia diaria |
| GET | `/daily/group/{groupId}` | Sí | Asistencia diaria por grupo |

### Teacher — `/api/teacher`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/myGroups` | Sí | Mis grupos |
| GET | `/groups/{groupId}/students` | Sí | Estudiantes del grupo (paginado) |
| GET | `/schedule` | Sí | Mi horario |

### Student — `/api/student`

| Método | Ruta | Auth | Descripción |
|---|---|---|---|
| GET | `/myGroups` | Sí | Mis grupos |
| GET | `/schedule` | Sí | Mi horario por fecha |

---

## Flujo de Registro de Asistencia (NFC)

1. El dispositivo envía: `UUID tarjeta + UUID módulo + timestamps (UTC y local)`
2. Se valida: tarjeta activa → usuario activo → usuario en la institución del dispositivo
3. Se crea un `AttendanceRecord` (registro crudo)
4. Se buscan horarios activos del usuario para ese día y hora
5. Se calcula el estado según `LateToleranceMinutes`:
   - Dentro del margen → `Presente (0)`
   - Fuera del margen → `Tarde (3)`
6. Se crea o actualiza el `DailyAttendance` para ese día

---

## Seguridad

### JWT
- Claims incluidos: `sub` (userId), `email`, `role`, `institutionId`
- Expiración por defecto: 1440 minutos (24 horas)
- Validación: Issuer, Audience, Signing Key, Lifetime

### Contraseñas
- Algoritmo: BCrypt con salt automático
- Funciones: `BCrypt.HashPassword()` / `BCrypt.Verify()`

### API Keys (Dispositivos)
- Generadas con `RandomNumberGenerator` (32 bytes, Base64)
- Almacenadas hasheadas en BD
- Regenerables via `POST /{id}/regenerateKey`

### Tarjetas NFC
- UID hasheado con SHA-256 + Base64
- Nunca se almacena el UID en texto plano

### CORS
Configurado como `AllowAll` (cualquier origen, método y header).

---

## Auditoría

El servicio `AuditService` registra en `AuditLogs`:
- Acciones: `CREATE`, `UPDATE`, `DELETE`
- Entidades: `User`, `Group`, `Device`, etc.
- Datos: `UserId`, `EntityId`, `Details`, `IpAddress`, `CreatedAt`

---

## Migraciones Aplicadas

| Fecha | Migración |
|---|---|
| 2026-03-17 | `InitialCreate` — tablas base |
| 2026-04-10 | `AddAuditLogs` — tabla de auditoría |
| 2026-04-28 | `AddDailyAttendance` — tabla de asistencia diaria |

---

## Notas de Implementación

- Las migraciones se aplican automáticamente al iniciar la app (`db.Database.Migrate()`)
- Soft delete en la mayoría de entidades (campo `IsActive`)
- Timestamps con `HasDefaultValueSql("now()")` en PostgreSQL
- UUIDs generados por PostgreSQL con `gen_random_uuid()`
- Configuración Npgsql: `AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true)`
- SignalR configurado para futura funcionalidad en tiempo real
