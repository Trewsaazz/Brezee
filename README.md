<div align="center">

# 🌬️ Brezee

### A modern, lightweight desktop client for Firebird SQL

*Work with your Firebird databases the way it should feel: fast, clear, and effortless, like a breeze.*

<br>

![Status](https://img.shields.io/badge/status-early%20development-orange?style=for-the-badge)
![License](https://img.shields.io/badge/license-MIT-blue?style=for-the-badge)
![Platform](https://img.shields.io/badge/platform-Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white)
<br>
![C++](https://img.shields.io/badge/C++-00599C?style=for-the-badge&logo=cplusplus&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![Firebird](https://img.shields.io/badge/Firebird%20SQL-F40F02?style=for-the-badge&logo=firebird&logoColor=white)
![Non-profit](https://img.shields.io/badge/non--profit-💚-2ea44f?style=for-the-badge)

<br>

[About](#-about) •
[Why Brezee](#-why-brezee) •
[Goals](#-goals) •
[Tech Stack](#%EF%B8%8F-tech-stack) •
[Building](#-building) •
[Status](#-project-status) •
[Contributing](#-contributing) •
[License](#-license)

</div>

---

## 📖 About

**Brezee** is a free, open-source desktop client for [Firebird SQL](https://firebirdsql.org/).

Firebird is a powerful, mature, and reliable relational database that has quietly powered
countless businesses and applications for decades. Its tooling, however, hasn't always kept
pace with what developers and database administrators expect today. Brezee aims to close that
gap with a clean, responsive, and approachable interface for everyday database work.

> [!NOTE]
> Brezee is a **completely non-profit** project. It exists for one reason: **to help people.**
> No paywalls, no premium tiers, no telemetry, just a useful tool, built in the open.

---

## 💡 Why Brezee?

<table>
<tr>
<td width="50%" valign="top">

### ⚡ Fast by design
A native C++ core keeps things snappy, even when working with large databases and result sets.

</td>
<td width="50%" valign="top">

### 🎨 Modern and clean
A polished WPF interface that feels at home on today's desktops. No clutter, no dated dialogs.

</td>
</tr>
<tr>
<td width="50%" valign="top">

### 🧭 Approachable
Friendly to newcomers learning Firebird, while still giving experienced DBAs the depth they need.

</td>
<td width="50%" valign="top">

### 💚 Free, forever
MIT-licensed and non-profit. Built by the community, for the community.

</td>
</tr>
</table>

---

## 🎯 Goals

Brezee is at the very beginning of its journey. These are the core ideas guiding it. The
detailed plan lives in the [roadmap](roadmap.md), and this section will grow as features land.

- 🔌 **Connections**: simple, reliable connection management for local and remote Firebird servers
- 🗂️ **Exploration**: browse databases, tables, views, procedures, and other objects at a glance
- ✍️ **Querying**: a comfortable SQL editor for writing and running queries
- 📊 **Results**: clear, fast presentation of query results
- 🛠️ **Administration**: everyday database tasks made simple

---

## 🛠️ Tech Stack

| Layer          | Technology                                              |
| -------------- | ------------------------------------------------------- |
| **Core**       | Native C++20: all database logic, no .NET dependencies  |
| **Bridge**     | C++/CLI: a thin managed wrapper around the core         |
| **UI**         | WPF on .NET 10                                          |
| **Database**   | Firebird SQL 3.0 and newer                              |
| **Build**      | Visual Studio 2026 / MSBuild, GitHub Actions CI         |

```
┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐
│   Brezee.App     │ ──▶ │  Brezee.Bridge   │ ──▶ │   Brezee.Core    │ ──▶  Firebird
│   WPF · C#       │     │  C++/CLI         │     │   native C++20   │
└──────────────────┘     └──────────────────┘     └──────────────────┘
```

---

## 🔨 Building

> [!IMPORTANT]
> WPF and C++/CLI are Windows-only, so Brezee builds and runs on **Windows 10/11 (x64)**.

**Requirements**

- [Visual Studio 2026](https://visualstudio.microsoft.com/) with these workloads:
  - *Desktop development with C++*, plus the **C++/CLI support** component
  - *.NET desktop development*
- .NET 10 SDK (included with Visual Studio)

**Build**

Open `Brezee.sln` in Visual Studio, select the `x64` platform, and press <kbd>F5</kbd>.

Or build from a *Developer PowerShell for VS 2026*:

```powershell
msbuild Brezee.sln -restore -p:Configuration=Release -p:Platform=x64
```

**Run the tests**

```powershell
./eng/run-tests.ps1 -Configuration Release
```

| Suite | Framework | Covers |
| ----- | --------- | ------ |
| `tests/Brezee.Core.Tests` | [doctest](https://github.com/doctest/doctest) | Native C++ core |
| `tests/Brezee.App.Tests` | [xUnit v3](https://xunit.net/) | View models and commands, plus the C++/CLI bridge end to end |

Both suites also show up in Visual Studio's Test Explorer.

Every push is built and tested by [GitHub Actions](.github/workflows/build.yml), which uploads a
ready-to-run build as an artifact.

---

## 🚧 Project Status

> [!WARNING]
> Brezee is in **early development** and is not ready for use yet. Things will change quickly,
> and nothing is stable.

Progress is tracked task by task in the [roadmap](roadmap.md). Screenshots and usage guides
will be added here as the project takes shape.

---

## 🤝 Contributing

Brezee is built to help people, and help building it is always welcome. Whether it's code,
ideas, bug reports, documentation, or simply sharing the project, every contribution counts.

Contribution guidelines are coming soon. In the meantime, feel free to open an issue to start
a conversation.

---

## 📄 License

Brezee is released under the [MIT License](LICENSE).

---

<div align="center">

Made with 💚 for the Firebird community

</div>
