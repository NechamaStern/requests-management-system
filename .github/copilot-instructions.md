# Requests Management System – Home Assignment (Full Stack, Level C)

This is a graded home assignment. ALWAYS align every piece of code/doc with the requirements below.
Work step by step as the user instructs. Prefer simple, correct, justified solutions (NO over-engineering).
User communicates in Hebrew; reply concisely in Hebrew. Code/identifiers in English.

## Goal
Request (פניות) management system for organizations/employers. Multi-user concurrent, large data volume.
Users search, filter, view, update status, view aggregates. Stack: .NET / .NET Core, REST API, Angular, DB of choice (SQL Server / MongoDB / other).

## Request model
Id, Title, OrganizationName, Status (New|InProgress|Waiting|Completed), Priority (Low|Medium|High),
AssignedTo, CreatedAt, UpdatedAt, Version/RowVersion (concurrency token).
Seed >= 100,000 records; seeding must be repeatable (re-runnable).

## 1. Querying (mandatory)
- REST API; NEVER send all records to client for filter/sort/paging.
- Server-side pagination; page size limit + parameter validation.
- Combined filters: Status, Priority, OrganizationName, AssignedTo, date range.
- Text search at least on Title and OrganizationName.
- Sorting by >= 3 fields, Asc/Desc.
- >= 2 relevant Aggregations (e.g. count by status, by priority).
- CancellationToken on all I/O. Return DTOs, not DB entities.
- Document how full in-memory load is avoided and how indexes support main search/filter scenarios.

## 2. Status update & concurrency (mandatory)
- Missing request -> proper HTTP code (404).
- Invalid status or disallowed transition -> clear validation error.
- Optimistic concurrency enforced server/DB side: only one concurrent update succeeds, other gets 409 Conflict.
- UpdatedAt and RowVersion/Version updated consistently. No lost updates.

## 3. Audit history (mandatory)
Each status change stored: RequestId, PreviousStatus, NewStatus, ChangedAt, ChangedBy. Endpoint to view a request's history.

## 4. Bulk update (mandatory)
Endpoint updating status for up to 100 requests in one call. Define + document behavior when some are missing or fail on concurrency.
Choose All-or-Nothing or Partial Success, justify choice, return clear per-item response.

## 5. Angular (mandatory)
Server-side search/filter/sort/paging; debounce on search; cancel/switch previous requests (switchMap);
Loading / Empty / Error states; status update with 409 handling; show aggregations; view change history;
separation of Components vs Services.

## 6. Performance (mandatory)
Pick >= 2 key API operations; document efficiency on 100K records: Execution/Query Plan (or Explain / equivalent metric),
indexes added and why, possible bottleneck + improvement. No full load before pagination/filtering when DB can do it.

## 7. Cache (mandatory, limited)
Cache >= 1 summary/aggregate (or other justified read). Define Expiration; Invalidation after updates (no stale data);
justify what is cached; describe required adaptation with multiple server instances (e.g. distributed cache / Redis).

## 8. Automated tests (mandatory)
>= 4 meaningful tests, at least one Integration test (API + DB or suitable substitute). Cover:
valid fetch/filter or update; validation/invalid input; concurrency conflict; integration.

## 9. Work planning (mandatory)
Show how spec became work plan: Features/User Stories/Tasks; each with what's needed + expected deliverable;
execution order & dependencies; effort estimate per task; clear mapping to assignment requirements;
include non-code tasks (DB design, tests, performance, docs, deployment/run readiness).

## 10. Code quality
Clear project structure/separation of concerns; readable code, consistent naming, no duplication; clear Models/DTOs/Mapping;
proper DI lifetimes; correct async; global error handling / ProblemDetails; consistent validation;
logging at meaningful points without sensitive data; avoid over-engineering.

## 11. Documentation (mandatory)
Clear run instructions; technologies + versions; solution structure; DB choice + indexes; Concurrency + Bulk explanation;
Cache + Invalidation; two significant technology decisions with alternatives considered; one known limitation + one thing to improve;
AI usage details (which parts, what was verified/changed manually).

## Submission
Must allow reviewers to fully run and inspect system, source, work plan, implementation, decisions.
NO ZIP / source-as-email-attachment (use e.g. a Git repository link). Submission quality is part of the grade.

## Grading weights
Backend .NET 17 | REST API 13 | Angular 15 | DB & performance 13 | Audit+Concurrency 11 | Work planning 8 |
Code quality 9 | Cache 4 | Tests 6 | Docs/submission 4.
Penalties: client-side filter/sort/paging; concurrency only in UI; cache without invalidation; shallow task breakdown; over-engineering.

## Working conventions & decisions (agreed with the user - follow by default)
Architecture / server (.NET 10)
- 3 layers: Api (Controllers, thin) -> Bll (services, Mapping, Cache) -> Dal (EF Core, Entities, Repositories, Models). Each layer registers its own DI (AddBusinessLayer -> AddDataLayer).
- Repositories live in Dal and are fully async with CancellationToken; read-only queries use AsNoTracking and let SQL do the work (GROUP BY etc.). Repositories return Dal models/projections (e.g. GroupCount<T>), never DTOs.
- DTOs live in Bll/Mapping/Dtos; AutoMapper Profile files in Bll/Mapping/Profiles. ALL entity/model -> DTO conversions go through AutoMapper (no manual ToDtos helpers). Keep DTOs even when thin (layer decoupling).
- Cache: IMemoryCache in the Bll service; explicit invalidation + short absolute expiration as safety net; document Redis/IDistributedCache for multiple instances.
- Assume every request has a Status and Priority (no NULL handling). Repositories return only existing groups; zero-fill is done on the client.
- Remove temporary/experimental code from ALL layers when asked (server + client).
- Don't leave NuGet vulnerability warnings; pin a patched version when needed.

Client (Angular, standalone, signals)
- Components vs Services separation; HTTP only in services (client/src/app/services); API-shaped types in models/.
- A child/presentational component used by a single screen lives INSIDE that screen's folder (e.g. components/dashboard/breakdown-card); move to shared/ only when reused.
- Pure helper functions go to a <screen>.utils.ts next to the component; display constants (labels/colors) in <screen>-display.ts.
- Always handle Loading / Error (with retry) / Empty states. Cancel subscriptions with takeUntilDestroyed(destroyRef) (or switchMap for search).
- Design: pleasant but simple and clear, RTL Hebrew, no gimmicks (graded submission).
- Dev proxy: client/proxy.conf.json forwards /api to http://localhost:5231. ng serve must be restarted if files are deleted/recreated.

Code & docs style
- Code/identifiers in English; UI text, docs and chat replies in Hebrew, concise. Comments only where they clarify something non-obvious.
- Each feature gets a short doc in /docs (data flow + how it meets the requirements) and README/server README are kept up to date.
- After changes: build (dotnet build server\RequestsManagement.sln, npx.cmd ng build in client) and verify against the real DB/API when possible. Stop the running API (Stop-Process by PID) before rebuilding the solution.
- In PowerShell use npx.cmd (npx.ps1 is blocked); long-running servers via detached async shell.
- When the user asks a design question, give a short honest recommendation with the trade-off, then follow their decision.
