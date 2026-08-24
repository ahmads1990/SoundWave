# 🚀 Project Bootstrap & Git Commit Execution Guide
*Automated step-by-step instructions for AI agents and developers to scaffold and commit a clean full-stack project.*

---

## 🛠️ Phase 1: Backend Solution Scaffolding (.NET 8 Flat Layout)

Run these PowerShell commands in the root of the backend repository (e.g. `RecipeManagerWebAPI/`):

```powershell
# 1. Create Solution
dotnet new sln -n SolutionName

# 2. Create Projects directly at root (No src/ or tests/ folders)
dotnet new classlib -o SolutionName.Domain -f net8.0
dotnet new classlib -o SolutionName.Application -f net8.0
dotnet new classlib -o SolutionName.Infrastructure -f net8.0
dotnet new webapi -o SolutionName.API -f net8.0 --no-openapi false
dotnet new xunit -o SolutionName.UnitTests -f net8.0

# 3. Add Projects to Solution
dotnet sln add SolutionName.Domain/SolutionName.Domain.csproj
dotnet sln add SolutionName.Application/SolutionName.Application.csproj
dotnet sln add SolutionName.Infrastructure/SolutionName.Infrastructure.csproj
dotnet sln add SolutionName.API/SolutionName.API.csproj
dotnet sln add SolutionName.UnitTests/SolutionName.UnitTests.csproj

# 4. Add Project References (Clean Architecture Rules)
dotnet add SolutionName.Application/SolutionName.Application.csproj reference SolutionName.Domain/SolutionName.Domain.csproj
dotnet add SolutionName.Infrastructure/SolutionName.Infrastructure.csproj reference SolutionName.Application/SolutionName.Application.csproj
dotnet add SolutionName.Infrastructure/SolutionName.Infrastructure.csproj reference SolutionName.Domain/SolutionName.Domain.csproj
dotnet add SolutionName.API/SolutionName.API.csproj reference SolutionName.Application/SolutionName.Application.csproj
dotnet add SolutionName.API/SolutionName.API.csproj reference SolutionName.Infrastructure/SolutionName.Infrastructure.csproj
dotnet add SolutionName.UnitTests/SolutionName.UnitTests.csproj reference SolutionName.Application/SolutionName.Application.csproj
dotnet add SolutionName.UnitTests/SolutionName.UnitTests.csproj reference SolutionName.Domain/SolutionName.Domain.csproj

# 5. Install Core NuGet Packages
# Application Layer
dotnet add SolutionName.Application/SolutionName.Application.csproj package FluentValidation.DependencyInjectionExtensions -v 11.11.0
dotnet add SolutionName.Application/SolutionName.Application.csproj package Mapster -v 7.4.0
dotnet add SolutionName.Application/SolutionName.Application.csproj package Mapster.DependencyInjection -v 1.0.1

# Infrastructure Layer
dotnet add SolutionName.Infrastructure/SolutionName.Infrastructure.csproj package Microsoft.EntityFrameworkCore -v 8.0.13
dotnet add SolutionName.Infrastructure/SolutionName.Infrastructure.csproj package Microsoft.EntityFrameworkCore.SqlServer -v 8.0.13 # or Npgsql / Sqlite
dotnet add SolutionName.Infrastructure/SolutionName.Infrastructure.csproj package Microsoft.EntityFrameworkCore.Tools -v 8.0.13
dotnet add SolutionName.Infrastructure/SolutionName.Infrastructure.csproj package Microsoft.AspNetCore.Authentication.JwtBearer -v 8.0.13
dotnet add SolutionName.Infrastructure/SolutionName.Infrastructure.csproj package System.IdentityModel.Tokens.Jwt -v 8.0.1
dotnet add SolutionName.Infrastructure/SolutionName.Infrastructure.csproj package BCrypt.Net-Next -v 4.0.3
dotnet add SolutionName.Infrastructure/SolutionName.Infrastructure.csproj package StackExchange.Redis -v 2.8.0
dotnet add SolutionName.Infrastructure/SolutionName.Infrastructure.csproj package MailKit -v 4.7.1.1
dotnet add SolutionName.Infrastructure/SolutionName.Infrastructure.csproj package Hangfire.AspNetCore -v 1.8.14
dotnet add SolutionName.Infrastructure/SolutionName.Infrastructure.csproj package Hangfire.SqlServer -v 1.8.14

# API Layer
dotnet add SolutionName.API/SolutionName.API.csproj package Microsoft.EntityFrameworkCore.Design -v 8.0.13
dotnet add SolutionName.API/SolutionName.API.csproj package Serilog.AspNetCore -v 8.0.3
dotnet add SolutionName.API/SolutionName.API.csproj package Serilog.Sinks.Console -v 6.0.0
dotnet add SolutionName.API/SolutionName.API.csproj package Serilog.Sinks.Seq -v 8.0.0
dotnet add SolutionName.API/SolutionName.API.csproj package Swashbuckle.AspNetCore -v 6.6.2
dotnet add SolutionName.API/SolutionName.API.csproj package Asp.Versioning.Mvc -v 8.1.1
dotnet add SolutionName.API/SolutionName.API.csproj package Asp.Versioning.Mvc.ApiExplorer -v 8.1.1
dotnet add SolutionName.API/SolutionName.API.csproj package FluentValidation.AspNetCore -v 11.3.0
dotnet add SolutionName.API/SolutionName.API.csproj package Microsoft.AspNetCore.ResponseCompression -v 2.2.0

# Test Layer
dotnet add SolutionName.UnitTests/SolutionName.UnitTests.csproj package FluentAssertions -v 6.12.0
dotnet add SolutionName.UnitTests/SolutionName.UnitTests.csproj package NSubstitute -v 5.1.0
```

---

## ⚡ Phase 2: Frontend Scaffolding (React + Vite + Tailwind)

Run these PowerShell commands in the root of the frontend repository (e.g. `RecipeManagerReact/`):

```powershell
# 1. Initialize Vite React TypeScript project
npx -y create-vite@latest ./ --template react-ts

# 2. Install Core Dependencies
npm install react-router-dom @tanstack/react-query axios lucide-react clsx tailwind-merge react-hook-form zod

# 3. Install Dev Dependencies (Tailwind CSS, PostCSS, Autoprefixer)
npm install -D tailwindcss postcss autoprefixer @types/node

# 4. Initialize Tailwind CSS Config
npx tailwindcss init -p
```

---

## 📋 The 8-Stage Isolated Git Commit Roadmap

> ⚠️ **Rule for Coding Agents:** Commit each phase individually with conventional commit messages so Git history remains clean, modular, and reviewable.

---

### 📦 Commit 1: Solution & Project Scaffolding
- **Action:** Scaffold .NET solution, projects at root level, project references, `.gitignore`, and install NuGet packages.
- **Git Command:**
  ```bash
  git add .
  git commit -m "chore: initialize .NET 8 Clean Architecture solution and project references"
  ```

---

### 📦 Commit 2: Domain Layer Entities & Contracts
- **Action:** Add `BaseModel.cs` (`int ID`, `bool Deleted`, audit dates), `Constants.cs`, `Enumeration.cs`, `AppUser.cs`, `RefreshToken.cs`, and domain models in `Domain/`.
- **Git Command:**
  ```bash
  git add SolutionName.Domain/
  git commit -m "feat(domain): define BaseModel, AppUser, RefreshToken, and domain entities"
  ```

---

### 📦 Commit 3: Infrastructure Layer (EF Core, DbContext & Helpers)
- **Action:** Add `AppDbContext.cs`, EntityConfigurations, `AppDbSeeder.cs`, `JwtConfig.cs`, `PasswordHelper.cs`, `TokenHelper.cs`, and `EmailService.cs` in `Infrastructure/`.
- **Git Command:**
  ```bash
  git add SolutionName.Infrastructure/
  git commit -m "feat(infrastructure): configure AppDbContext, entity configurations, JWT token helper, and password hasher"
  ```

---

### 📦 Commit 4: Application Layer (DTOs, Validation & Services)
- **Action:** Add `BasePaginatedDto.cs`, DTOs, `InfraInterfaces/`, `IAuthService.cs`, `AuthService.cs`, Mapster configurations, and `ServiceEnums.cs` in `Application/`.
- **Git Command:**
  ```bash
  git add SolutionName.Application/
  git commit -m "feat(application): add DTOs, service interfaces, Mapster config, and AuthService implementation"
  ```

---

### 📦 Commit 5: API Layer Bootstrap (Controllers, Middlewares & Program.cs)
- **Action:** Add `BaseController.cs`, `AuthController.cs`, request/response models (`ApiResponse<T>`, `SuccessResponse<T>`, `ErrorResponse<T>`), `GlobalExceptionHandlerMiddleware.cs`, `ProgramExtensions.cs`, `appsettings.json`, and `Program.cs`.
- **Git Command:**
  ```bash
  git add SolutionName.API/
  git commit -m "feat(api): implement Program.cs, BaseController, AuthController, global exception handler, and Swagger"
  ```

---

### 📦 Commit 6: Frontend Scaffolding & Tailwind Setup
- **Action:** Initialize Vite project, configure `tailwind.config.js`, `index.css`, `postcss.config.js`, and `tsconfig.json`.
- **Git Command:**
  ```bash
  git add .
  git commit -m "chore(client): initialize React Vite TypeScript project with Tailwind CSS configuration"
  ```

---

### 📦 Commit 7: Frontend Core Infrastructure (Axios, AuthContext & Routing)
- **Action:** Add `src/api/api.ts` (with Bearer interceptors), `src/contexts/AuthContext.tsx`, `ProtectedRoute.tsx`, and standard layouts (`AppLayout.tsx`, `AuthLayout.tsx`, `Navbar.tsx`, `Sidebar.tsx`).
- **Git Command:**
  ```bash
  git add src/
  git commit -m "feat(client-core): setup configured Axios client, AuthContext, route guards, and layouts"
  ```

---

### 📦 Commit 8: Frontend UI Primitives & Components
- **Action:** Add reusable components: `Button.tsx`, `Input.tsx`, `Card.tsx`, `Modal.tsx`, `Badge.tsx`, `Skeleton.tsx`, and `ToastContext.tsx`.
- **Git Command:**
  ```bash
  git add src/components/ src/contexts/ToastContext.tsx
  git commit -m "feat(client-ui): implement reusable UI primitives (Button, Input, Card, Modal, Badge, Skeleton)"
  ```
