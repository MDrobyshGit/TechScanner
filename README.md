# TechScanner — Codebase Technology Scanner

A tool that scans a codebase (local folder or Git repository), detects all technologies and dependencies, and enriches them with AI-powered analysis: support status, last release date, and actionable recommendations.

**Built end-to-end using the [BMAD Method](https://github.com/bmadcode/bmad-method) — an AI-native software development workflow.**

---

## What It Does

Point TechScanner at a project and get back a table of every technology it uses:

| Technology | Version | Manifest | Status | Active in Code | Recommendation |
|---|---|---|---|---|---|
| React | 18.2.0 | package.json | Active | ✓ | — |
| moment | 2.29.1 | package.json | Abandoned | ✓ | Migrate to date-fns or dayjs |
| EntityFramework | 6.x | *.csproj | Abandoned | ✓ | Upgrade to EF Core 8 |

Supports:
- `package.json` — Node.js / npm dependencies
- `*.csproj` — .NET / NuGet packages
- `requirements.txt` — Python / pip packages
- **Git repositories** — clone & scan directly from URL (with optional auth token)
- **Local folders** — scan any path on disk

---

## How It Was Built — BMAD Method

This project was built from zero through a structured AI-native development process using the **BMAD (Breakthrough Method for Agile AI-Native Development)** methodology and GitHub Copilot as the primary AI pair programmer.

### Development Steps

#### Step 1 — Product Brief
Created a concise product brief defining the core problem, target user, and success criteria using the `bmad-create-product-brief` workflow.

#### Step 2 — PRD (Product Requirements Document)
Elaborated full functional and non-functional requirements with the `bmad-create-prd` workflow, including user stories, acceptance criteria, and scope boundaries.

#### Step 3 — UX Design
Designed all UI screens and component interactions (scan form, progress bar, results table with filtering/sorting) using `bmad-create-ux-design`.

#### Step 4 — Architecture
Produced a complete technical architecture document defining:
- Clean Architecture layers (Core / Infrastructure / Scanner / Api)
- Technology choices (.NET 10, EF Core, SQLite, React 18, Vite)
- Data flow, API contracts, background job design
- LLM integration strategy

Using the `bmad-create-architecture` workflow.

#### Step 5 — Tech Spec & Implementation
Generated a detailed Tech Spec covering all 26 implementation tasks, then executed them one by one with the `bmad-dev-story` and `bmad-quick-dev` workflows. Each task was implemented, tested, and validated before moving to the next.

The entire codebase — backend, frontend, tests, config — was produced through iterative AI-assisted development with continuous validation.

### BMAD Artifacts
All planning artifacts are preserved in the repository:
- [`_bmad-output/planning-artifacts/`](./_bmad-output/planning-artifacts/) — PRD, UX Design, Architecture docs
- [`_bmad-output/implementation-artifacts/tech-spec-codebase-technology-scanner-mvp.md`](./_bmad-output/implementation-artifacts/tech-spec-codebase-technology-scanner-mvp.md) — Full tech spec with all 26 tasks

---

## Architecture

```
TechScanner/
├── src/
│   ├── TechScanner.Core/          # Domain entities, interfaces, enums, models
│   ├── TechScanner.Infrastructure/ # EF Core DbContext, repositories, LLM enrichers
│   ├── TechScanner.Scanner/       # Scan engine: parsers, collectors, orchestrator, background service
│   └── TechScanner.Api/           # ASP.NET Core Web API + static file hosting
├── frontend/                      # React 18 + Vite + TypeScript SPA
└── tests/
    └── TechScanner.Tests/         # xUnit unit tests (24 tests)
```

**Backend:** .NET 10 · ASP.NET Core · EF Core 8 · SQLite · Background Channels · SSE progress streaming  
**Frontend:** React 18 · TypeScript · Vite · Vitest (12 tests)  
**AI Enrichment:** GitHub Models API (`gpt-4o-mini`) — free with a GitHub PAT

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 18+](https://nodejs.org/)
- A GitHub Personal Access Token (for LLM enrichment — optional)

### 1. Clone the repository
```bash
git clone https://github.com/MDrobyshGit/TechScanner.git
cd TechScanner
```

### 2. Configure LLM (optional but recommended for demo)

Create `src/TechScanner.Api/appsettings.Development.json`:
```json
{
  "LlmSettings": {
    "Provider": "GitHubModels",
    "ApiKey": "YOUR_GITHUB_PAT_HERE",
    "Model": "gpt-4o-mini"
  }
}
```

**Getting a GitHub PAT:** GitHub → Settings → Developer settings → Personal access tokens → Generate new token (classic). No scopes required — just create and copy.

> Without a token, the app still works — technologies will have `Unknown` support status instead of AI-analyzed status.

### 3. Run the backend
```bash
cd src/TechScanner.Api
dotnet run
# API available at http://localhost:5059
```

The SQLite database (`techscanner.db`) is created automatically on first run.

### 4. Run the frontend
```bash
cd frontend
npm install
npm run dev
# UI available at http://localhost:5173
```

### 5. Run tests
```bash
# Backend (24 tests)
dotnet test

# Frontend (12 tests)
cd frontend
npx vitest run
```

---

## Usage

1. Open **http://localhost:5173** in your browser
2. Choose source type: **Local Folder** or **Git Repository**
3. Enter the path or Git URL (optionally provide a Git token for private repos)
4. Click **Start Scan**
5. Watch real-time progress via the progress bar
6. Explore results: filter by support status, sort by any column, delete scans from history

### Example inputs to try
- Local folder: `C:\Projects\my-app` (or any path with `package.json` / `.csproj` / `requirements.txt`)
- Git repo: `https://github.com/facebook/react`
- This repo itself: `https://github.com/MDrobyshGit/TechScanner`

---

## API Reference

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/scans` | Start a new scan |
| `GET` | `/api/scans` | Get scan history |
| `GET` | `/api/scans/{id}` | Get scan result |
| `GET` | `/api/scans/{id}/progress` | SSE progress stream |
| `DELETE` | `/api/scans/{id}` | Delete a scan |

**POST /api/scans body:**
```json
{
  "sourceType": "LocalFolder",
  "sourceInput": "C:\\path\\to\\project",
  "gitToken": null
}
```
`sourceType` values: `"LocalFolder"` | `"GitRepository"`

---

## Configuration

All settings are in `src/TechScanner.Api/appsettings.json`:

| Key | Default | Description |
|-----|---------|-------------|
| `ConnectionStrings.Default` | `Data Source=techscanner.db` | SQLite path |
| `DatabaseProvider` | `Sqlite` | `Sqlite` or `SqlServer` |
| `LlmSettings.Provider` | `GitHubModels` | `GitHubModels` or `OpenAI` |
| `LlmSettings.ApiKey` | _(empty)_ | GitHub PAT or OpenAI key |
| `LlmSettings.Model` | `gpt-4o-mini` | Model name |
