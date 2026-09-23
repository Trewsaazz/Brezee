# FirebirdStudio Roadmap

The long-term goal: match and eventually beat  what other clients can do, with a modern UI and none of the 2004-era baggage. This list is essentially what a firebird client should do, broken into phases so the project has a sane build order instead of trying to do everything at once.

Nothing here is set in stone — phases can reorder, and some items may get cut if they turn out to be dead weight nobody actually uses.

## Technical decisions

- **Architecture:** native C++ core → thin C++/CLI bridge → C# WPF application
- **Minimum Firebird version:** 3.0 (uses the OO API; 2.5 and older are not supported)

## Phase 0 — Foundation

### Project groundwork

- [x] Solution scaffolding — native C++ core, C++/CLI bridge, WPF app
- [ ] Build setup and CI (build + tests on every push)
- [ ] Logging and centralized error handling (core → bridge → UI)
- [ ] Unit test projects for the core and the app

### Application shell

- [x] Main window shell with dockable panels (AvalonDock) from day one
- [ ] Central command system (menus, toolbars, and shortcuts all route through it)
- [ ] All UI strings in resource files from the start, so localization is possible later

### Connections

- [ ] Connect to a Firebird server (host/port/path/credentials)
- [ ] Embedded mode support — open a `.fdb` file directly, no running server needed
- [ ] Saved connections / database registration (alias list, like a bookmarks panel)
- [ ] Secure credential storage for saved passwords (Windows DPAPI, never plain text)
- [ ] Recent connections list
- [ ] Multiple simultaneous database connections (tabs or separate windows)

## Phase 1 — Core Browsing & Data Editing

- [ ] Database Explorer — object tree for tables, views, stored procedures, triggers, generators/sequences, domains, exceptions, UDFs, roles, indices
- [ ] Table data grid — browse, sort, filter rows
- [ ] Inline data editing (insert/update/delete rows) with commit/rollback
- [ ] Blob viewer/editor (text, image, binary preview)
- [ ] Visual editors for creating/altering objects without hand-writing DDL: tables, fields, views, indices, domains, exceptions, generators
- [ ] Dependencies viewer — what references a given object, and what it depends on
- [ ] Database properties viewer (ODS version, character set, size, etc.)

## Phase 2 — SQL & Development Tools

- [ ] SQL editor with syntax highlighting, multiple tabs and simple themes
- [ ] Query results grid with export
- [ ] Autocomplete / code completion for table, column, and procedure names
- [ ] Visual query builder (drag tables in, build joins visually)
- [ ] Script executive — run multi-statement DDL/DML scripts as a unit
- [ ] Stored procedure & trigger editor with syntax highlighting
- [ ] Stored procedure/trigger debugger — breakpoints, step-through, variable inspection
- [ ] Execution plan viewer/analyzer

## Phase 3 — Schema & Metadata Tools

- [ ] Extract metadata — generate a full DDL script for the database or a single object
- [ ] Search in metadata — find text across all database objects
- [ ] Copy object DDL to clipboard
- [ ] **Database comparer** — diff schema between two databases, generate a sync script
- [ ] Table data comparer — diff row-level data between two tables/databases
- [ ] HTML/PDF database documentation generator
- [ ] Database designer / ER diagram — visual schema modeling, reverse-engineer an existing DB into a diagram

## Phase 4 — Administration

- [ ] User manager (create/alter/drop database users)
- [ ] Grant/permissions manager — visual editor for object- and role-level grants
- [ ] Role editor
- [ ] Backup/restore — GUI wrapper around `gbak` with progress feedback
- [ ] Database validation & repair
- [ ] Database shutdown / bring online
- [ ] Secondary files manager (multi-file databases)
- [ ] Server properties/log viewer
- [ ] Firebird instance manager — handle multiple installed Firebird versions/services

## Phase 5 — Monitoring & Performance

- [ ] Active transaction monitor
- [ ] SQL monitor — see live queries hitting the database in real time
- [ ] Database statistics (page usage, fragmentation, record versions)
- [ ] Index selectivity recompute
- [ ] Index usage/analysis
- [ ] Stored procedure/trigger/view analyzer — flag unused or problematic code
- [ ] Trace/audit log viewer (via Firebird's trace API)

## Phase 6 — Import / Export / Reporting

- [ ] CSV export (per table or per query result)
- [ ] CSV/ODBC data import
- [ ] Report builder / custom report designer
- [ ] Test data generator — populate tables with realistic dummy data

## Phase 7 — Quality of Life

- [ ] Customizable keyboard shortcuts
- [ ] Customizable panel layout (save/restore docking layouts)
- [ ] To-do list panel for tracking database-related tasks
- [ ] UI localization support
- [ ] Code snippets/templates for SQL & PSQL
- [ ] Command-line companion tool — headless script execution (IBExpert's `IBEScript.exe` equivalent)

## Explicitly out of scope for now

- OLAP / cross-classified table tools
- Legacy InterBase-only quirks IBExpert still carries for backward compatibility but that don't apply to modern Firebird versions

---