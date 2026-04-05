# SYNQUID — Backend API

Sistema de control de asistencia NFC para centros educativos.

**Stack:** ASP.NET Core 8 · PostgreSQL 16 · Entity Framework Core 8 · Docker

---

## Requisitos

- .NET 8 SDK
- Docker Desktop (para PostgreSQL)
- Git

---

## Configuracion inicial

### 1. Clonar el repositorio

```bash
git clone https://github.com/TU-USUARIO/synquid-backend.git
cd Synquid
```

### 2. Levantar PostgreSQL en Docker

```bash
docker run -d --name synquid-db -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=synquid -p 9000:5432 postgres:16
```

### 3. Crear el archivo de configuracion local

Crea el archivo `src/Synquid.API/appsettings.Development.json` (no se sube a Git):

```json
{
    "ConnectionStrings": {
        "DefaultConnection": "Host=localhost;Port=9000;Database=synquid;Username=postgres;Password=postgres"
    }
}
```

### 4. Aplicar migraciones

```bash
cd src/Synquid.API
dotnet ef database update --project ../Synquid.Infrastructure
```

---

## Ejecutar la API

```bash
cd src/Synquid.API
dotnet run --urls="http://localhost:5000"
```

Abrir en el navegador:

```
http://localhost:5000/swagger
```

---

## Exponer con Ngrok (para Raspberry Pi)

### Primera vez: configurar token

```bash
ngrok config add-authtoken TU_TOKEN
```

### Levantar tunel

```bash
ngrok http 5000
```

Ngrok te dara una URL publica tipo `https://xxxx.ngrok-free.app`. Usala desde la Raspberry Pi:

```bash
curl -X POST https://xxxx.ngrok-free.app/api/attendance/check \
  -H "Content-Type: application/json" \
  -H "ngrok-skip-browser-warning: true" \
  -d '{"uid": "04A3B2C1"}'
```

---

## Endpoints disponibles

| Metodo | Ruta | Descripcion |
|---|---|---|
| GET | `/api/institutions` | Listar instituciones |
| POST | `/api/institutions` | Crear institucion |
| GET | `/api/institutions/{id}` | Obtener una |
| PUT | `/api/institutions/{id}` | Editar |
| DELETE | `/api/institutions/{id}` | Eliminar (soft delete) |
| POST | `/api/attendance/check` | Verificar tarjeta NFC |

---

## Estructura del proyecto

```
Synquid/
├── src/
│   ├── Synquid.API/             ← Controllers, Program.cs, DTOs
│   ├── Synquid.Application/     ← Servicios, Interfaces
│   ├── Synquid.Domain/          ← Entidades, Enums
│   └── Synquid.Infrastructure/  ← DbContext, Repositorios, Migraciones
├── .gitignore
├── Synquid.sln
└── README.md
```

---

## Equipo

| Miembro | Rol |
|---|---|
| Edu Calderon | Frontend Next.js + App Flutter |
| Neudys Tejada | Backend C# + Raspberry Pi C++ |