# DomainCopilot — Industrial Field Maintenance Copilot

[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![Clean Architecture](https://img.shields.io/badge/Architecture-Clean%20Architecture-brightgreen.svg)]()
[![PostgreSQL](https://img.shields.io/badge/Database-PostgreSQL%2018%20Native-blue.svg)](https://www.postgresql.org/)
[![Tests](https://img.shields.io/badge/Tests-25%20Passing-success.svg)]()
[![Twist T2](https://img.shields.io/badge/Twist-T2%20Offline%20Degraded%20Mode-orange.svg)]()

> **ITI Instructor Assessment Task**  
> **Domain**: **D5 Industrial Field Maintenance**  
> **Assigned Twist**: **T2 Offline / Degraded Mode** (Zero external dependencies; deterministic local fallbacks with explicit capability difference logging)

---

## 🚀 Live Interactive Web Demo

When the API server is running, the interactive dark-mode dashboard is available at:

👉 **Demo URL**: [http://localhost:5174/](http://192.168.11.6:5174/)  
*(Or navigate to `/index.html` on the API host)*

### 🎮 What You Can Try in the Demo
1. **👤 User Authentication Modal (PostgreSQL Database)**:
   - Click **`🔑 Login`** in the top bar.
   - Select either the **1 System Administrator** (`supervisor` — Eng. Mohamed) or one of the **Field Technicians** (`tech_ahmed`, `tech_omar`, `tech_sara`, `tech_youssef`).
   - Or switch to **`➕ Register New Technician`** to create and persist a new field technician account directly into PostgreSQL.
2. **⚙️ Multi-Agent Diagnostic Workflow**:
   - Click **"Run Multi-Agent Diagnostic Workflow"** to coordinate Symptom Matcher, Diagnostic Planner, C# Safety Guardrail, and Work Order Generator.
   - Click **"Test Safety Guardrail Block"** to see the deterministic C# guardrail block an unsafe work order missing Lockout-Tagout (LOTO) protocols.
3. **🔍 Vector & Hybrid Search Comparison**:
   - Test **Dense Vector Search** (`/api/rag/search`) vs **Hybrid RRF Fusion** (`/api/rag/hybrid-search`).
   - Click preset query chips (`🔧 Hydraulic Circuit`, `⚡ Pump Vibration`, `🔒 Zero-Energy LOTO`).
   - Click **`🛡️ Refusal Test`** to verify grounded refusal on out-of-domain queries (e.g. cupcake recipes).
4. **💬 Real-Time Streaming Assistant (SSE)**:
   - Ask any maintenance question and receive token-by-token streaming guidance in real time.

---

## 🏗️ Architecture & Core Components

The solution strictly adheres to **Clean Architecture** principles across four layers:

```
DomainCopilot/
├── DomainCopilot.Domain/            # Enterprise entities & business rules (No external dependencies)
│   ├── Documents/                  # Document, DocumentChunk, Embedding abstractions
│   └── Users/                      # User entity, UserRoles (Supervisor vs Technician)
├── DomainCopilot.Application/       # Use cases, interfaces, orchestrators, safety guardrails
│   ├── Agents/                     # Multi-Agent system (SymptomMatcher, DiagnosticPlanner, WorkOrderGenerator)
│   │   ├── Guardrails/             # DeterministicSafetyGuardrail.cs (C# deterministic enforcement)
│   │   └── MaintenanceCopilotOrchestrator.cs
│   ├── Auth/                       # JWT Authentication interfaces & DTOs
│   ├── Documents/                  # Ingestion pipeline, parsers, chunking strategies
│   └── Rag/                        # Vector and Hybrid retrieval contracts & DTOs
├── DomainCopilot.Infrastructure/    # External technologies & persistence
│   ├── Auth/                       # JwtAuthService.cs (PostgreSQL user lookup, single-admin policy)
│   ├── Data/                       # EF Core ApplicationDbContext (PostgreSQL 18 native vector cosine)
│   ├── Documents/                  # PDF/DOCX extractors, text cleaning, repository
│   ├── Providers/                  # Resilient LLM & Embedding providers with Twist T2 fallback chain
│   └── Rag/                        # PostgresVectorSearchService & PostgresHybridRetrievalService (RRF)
├── DomainCopilot.Api/               # ASP.NET Core Web API presentation & Static UI
│   ├── Controllers/                # Auth, Rag, Copilot, Agents, Documents endpoints
│   └── wwwroot/index.html          # Interactive Dark-Mode Web Dashboard & Authentication Popup
└── DomainCopilot.UnitTests/         # 25 Automated Unit, Integration, and Contract Tests
```

---

## 🔑 Key Implementations & Innovations

### 1. PostgreSQL Native Vector & Hybrid Search (RRF)
- **Zero Binary Dependency**: Uses native PostgreSQL `real[]` columns with an immutable PL/pgSQL cosine similarity function.
- **Reciprocal Rank Fusion (RRF)**: Merges dense vector similarity scores with PostgreSQL BM25 full-text keyword rankings ($k = 60$, $w_{dense} = 0.5$, $w_{keyword} = 0.5$).
- **Structured Citations & Grounded Refusal**: Returns document source, section name, page number, relevance score, and exact snippet. If no relevant chunks match, returns an explicit refusal message instead of hallucinating.

### 2. Twist T2: Offline / Degraded Mode Resilient Chain
- **Automatic Fallback Chain**:
  - `HostedEmbeddingProvider` (OpenAI / Cloud) $\to$ `LocalEmbeddingProvider` (Ollama) $\to$ `OfflineDeterministicEmbeddingProvider` (384-dimensional unit-normalized pseudo-semantic vector).
  - `HostedLlmProvider` $\to$ `LocalLlmProvider` $\to$ `DeterministicMaintenanceLlmProvider`.
- **Zero System Halting**: The entire system operates smoothly in completely air-gapped or offline industrial facilities.

### 3. Multi-Agent Maintenance Diagnostics & C# Safety Guardrail
- **Agent 1 (Symptom Matcher)**: Analyzes telemetry and symptoms (e.g. vibration, overheating).
- **Agent 2 (Diagnostic Planner)**: Outlines root cause analysis and corrective actions.
- **C# Deterministic Safety Guardrail**: Validates high-risk equipment strictly in C# code:
  - Enforces Lockout-Tagout (LOTO) isolation point identification.
  - Enforces complete Personal Protective Equipment (PPE) checklists.
  - Enforces physical zero-energy checks (electrical, hydraulic, mechanical) before certifying any work order.
- **Agent 3 (Work Order Generator)**: Produces the certified Work Order only when the safety guardrail passes.

### 4. Role-Based JWT Security & Single-Admin Policy
- **Supervisor Role** (`supervisor`): Full administrative access, manual ingestion (`POST /api/documents/ingest`), diagnostics, and search.
- **Technician Role** (`tech_ahmed`, etc.): Diagnostics and search access; document ingestion is blocked (403 Forbidden).
- **Single-Admin Enforcement**: Enforced both on the frontend and deterministically in backend C# (`JwtAuthService.cs`)—registration of additional admin accounts is rejected.

---

## 🛠️ How to Run Locally

### Prerequisites
1. **.NET 8.0 SDK** installed.
2. **PostgreSQL 18** (or 16+) running on `localhost:5432` with database `domaincopilot`, user `postgres`, password `123456`.

### 1. Clone & Build
```powershell
git clone <repo-url>
cd DomainCopilot
dotnet build
```

### 2. Run Tests
```powershell
dotnet test
```
*Output: **25 Passed** (1 Contract Test, 3 Integration Tests, 21 Unit Tests).*

### 3. Run Web API & Launch Demo
```powershell
dotnet run --project DomainCopilot.Api --launch-profile http
```
The application will start listening on:
- **Interactive UI Dashboard**: `http://localhost:5174/`
- **Swagger Documentation**: `http://localhost:5174/swagger`

---

## 📡 API Testing Examples (cURL & PowerShell)

### 1. Authenticate (Login as Supervisor or Technician)
```powershell
# Login as Admin (Supervisor)
$login = Invoke-RestMethod -Uri "http://localhost:5174/api/auth/login" `
  -Method Post -ContentType "application/json" `
  -Body '{"username":"supervisor","password":"123456"}'

$token = $login.token
$headers = @{ Authorization = "Bearer $token" }
```

### 2. Dense Vector Search (`/api/rag/search`)
```powershell
Invoke-RestMethod -Uri "http://localhost:5174/api/rag/search" `
  -Method Post -ContentType "application/json" `
  -Headers $headers `
  -Body '{"query":"hydraulic pressure relief valve","topK":3}'
```

### 3. Hybrid RRF Retrieval (`/api/rag/hybrid-search`)
```powershell
Invoke-RestMethod -Uri "http://localhost:5174/api/rag/hybrid-search" `
  -Method Post -ContentType "application/json" `
  -Headers $headers `
  -Body '{"query":"pump vibration cavitation","topK":3}'
```

### 4. Multi-Agent Maintenance Diagnostic (`/api/agents/diagnose`)
```powershell
$diagnosticPayload = @{
    equipmentId = "PUMP-CENT-04"
    equipmentType = "Centrifugal Slurry Pump"
    reportedSymptoms = "High bearing temperature (88C) and excessive axial vibration"
    priority = "High"
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5174/api/agents/diagnose" `
  -Method Post -ContentType "application/json" `
  -Headers $headers `
  -Body $diagnosticPayload
```

### 5. Deterministic Safety Guardrail Violation Test (`/api/agents/verify-safety`)
```powershell
# Missing LOTO on high-risk equipment -> Deterministically blocked with HTTP 400
$unsafePayload = @{
    requiresLockoutTagout = $false
    isolationPoint = ""
    requiredPpe = @()
    zeroEnergyChecks = @()
} | ConvertTo-Json

Invoke-RestMethod -Uri "http://localhost:5174/api/agents/verify-safety?equipmentType=Centrifugal%20Pump" `
  -Method Post -ContentType "application/json" `
  -Headers $headers `
  -Body $unsafePayload
```

### 6. Real-Time Streaming Copilot (SSE)
```powershell
curl -N "http://localhost:5174/api/copilot/stream?prompt=Explain%20zero-energy%20verification%20steps"
```

---

## 👥 Seeded User Accounts (PostgreSQL)

| Username | Role | Full Name / Description | Password |
|---|---|---|---|
| `supervisor` | **Supervisor (Admin)** | Eng. Mohamed — Lead Maintenance Supervisor | `123456` |
| `tech_ahmed` | **Technician** | Ahmed — Vibration & Bearing Specialist | `123456` |
| `tech_omar` | **Technician** | Omar — Electrical & LOTO Safety Tech | `123456` |
| `tech_sara` | **Technician** | Sara — Mechanical Alignment Specialist | `123456` |
| `tech_youssef` | **Technician** | Youssef — Lubrication & Seals Tech | `123456` |

*New technicians can be registered dynamically via the UI modal or `POST /api/auth/register`.*
