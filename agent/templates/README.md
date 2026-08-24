# 🏛️ Full-Stack Clean Architecture Template Blueprint
*Directly mirrored from `ExaminationSystemWebAPI` + `ExaminationSystemReact` (Single-Tenant & Tailwind CSS)*

---

## 🎯 Purpose & Scope
This template serves as the canonical architectural seed for new projects. It takes the battle-tested architectural foundation of the **Examination System** and adapts it for single-tenant, rapid-shipping projects:

1. **Exact Backend Structure:** .NET 8 (`net8.0`), `BaseModel` with integer `int ID`, `ApiResponse<T>`, `SuccessResponse<T>`, `ErrorResponse<T>`, and `PaginatedResponse<T>`.
2. **Tailwind CSS Frontend:** React + Vite + TypeScript with Tailwind CSS (replacing Bootstrap).
3. **Pure Single-Tenant Architecture:** All multi-tenancy middlewares, filters, and headers stripped out for clean, focused domain modeling.
4. **Step-by-Step Commit Roadmap:** 8 isolated, reviewable Git commits for clean project bootstrapping.

---

## 📂 Template Documents

- [Backend Clean Architecture Blueprint](BackendCleanArchitecture.md) — Comprehensive .NET 9 backend architecture, folder structure, file descriptions, core abstractions, and dependency injection setup.
- [Frontend React + Tailwind Blueprint](FrontendReactTailwind.md) — Production-ready React + Vite + TypeScript + Tailwind CSS structure, Axios interceptors, AuthContext, UI primitives, and TanStack Query state patterns.
- [Step-by-Step Bootstrap & Git Commit Guide](BootstrapGuide.md) — Exact CLI commands and an 8-stage isolated Git commit sequence to bootstrap any new project cleanly.

---

## 🏗️ High-Level System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    REACT (VITE + TYPESCRIPT + TAILWIND CSS)                 │
│                                                                             │
│   [ Pages / Views ] ──► [ Custom Hooks / React Query ] ──► [ Services API ] │
│            │                                                    │           │
│   [ UI Components ] ◄────── [ Auth / Toast Context ] ───────────┘           │
└──────────────────────────────────────┬──────────────────────────────────────┘
                                       │ HTTP / JSON (Bearer JWT)
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         .NET 9 WEB API (CLEAN ARCHITECTURE)                 │
│                                                                             │
│   ┌─────────────────────────────────────────────────────────────────────┐   │
│   │                      1. API Layer (Controllers)                     │   │
│   │   • Middlewares (Global Exception, Serilog, RateLimiter)            │   │
│   │   • Controllers & Minimal Endpoints (BaseController Result Mapping) │   │
│   └──────────────────────────────────┬──────────────────────────────────┘   │
│                                      │                                      │
│   ┌──────────────────────────────────▼──────────────────────────────────┐   │
│   │                    2. Application Layer (Logic)                     │   │
│   │   • DTOs & ViewModels            • Service Contracts / Handlers     │   │
│   │   • FluentValidation Rules       • Mapster Object Mappings          │   │
│   └──────────────────────────────────┬──────────────────────────────────┘   │
│                                      │                                      │
│   ┌──────────────────────────────────▼──────────────────────────────────┐   │
│   │                    3. Infrastructure Layer (IO)                     │   │
│   │   • AppDbContext & Migrations    • Repository / UnitOfWork          │   │
│   │   • External Auth / JWT Service  • Email / Background Jobs          │   │
│   └──────────────────────────────────┬──────────────────────────────────┘   │
│                                      │                                      │
│   ┌──────────────────────────────────▼──────────────────────────────────┐   │
│   │                      4. Domain Layer (Enterprise)                   │   │
│   │   • Entities & Value Objects     • Domain Enums & Constants         │   │
│   │   • Base & Auditable Entities    • Domain Events (Optional)         │   │
│   └─────────────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────────────┘
```
