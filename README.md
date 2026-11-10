# BC_Progress

A lightweight Windows desktop app to track your **The Battle Cats** roster and campaign progress (the later is still in progress).

## ✨ Features

- **Cat overview as tiles** with rarity grouping
- Fast level editing:
  - base level
  - plus level
  - manual input + quick +/- buttons
- **Drag & drop ordering** of owned cats (category-safe)
- **Grid lock toggle** to prevent accidental reordering
- **Image support** with local cache and refresh action
- Per-cat context actions:
  - **Evolution** (next form)
  - **Wiki** link
- **Campaign Progress** tab (Empire of Cats, ITF, COTC, Story of Legends)
- **File menu**
  - Import `Progress.json`
  - Export (Save As dialog, default file name `Progress.json`)

## 🧱 Tech Stack

- **.NET 10** (`net10.0-windows`)
- **WinForms**
- `System.Text.Json`
- `HttpClient` for wiki/image metadata

## 📁 Project Structure

```text
BC_Progress/
├─ BC_Progress.csproj
├─ Progress.json
├─ Sourcecode/
│  ├─ Program.cs
│  ├─ Form1.cs
│  ├─ Form1.Designer.cs
│  ├─ Controls/
│  │  └─ BufferedFlowLayoutPanel.cs
│  ├─ Models/
│  │  ├─ ProgressDocument.cs
│  │  ├─ CampaignProgressRow.cs
│  │  ├─ CatTileBinding.cs
│  │  └─ EvolutionInfo.cs
│  └─ Services/
│     └─ ProgressDocumentService.cs
└─ README.md
```

## 🚀 Getting Started

### Prerequisites

- Windows
- .NET 10 SDK
- Visual Studio 2026 (or newer) with WinForms support

### Run

```powershell
dotnet build
dotnet run
```

Or open `BC_Progress.slnx` in Visual Studio and start debugging.

## 💾 Data Handling

The app uses `Progress.json` as the main data source.

- On startup, it searches for `Progress.json` from the app directory upwards.
- If no file exists, a default document is created automatically.
- Changes are persisted to JSON.
- You can also import/export via **File** menu.

## 🖼️ Images & Cache

Cat icons are cached locally in:

```text
cache/cat-icons
```

Use **Options → Refresh Images** to clear cached icon files and reload them.

## ⚙️ Notes

- The UI includes anti-flicker improvements (double-buffered tile panel + composited form rendering).
- Network-dependent actions (wiki/image lookup) may be slower on first load and improve after caching.

## 🗺️ Roadmap (optional)

- Additional export formats (e.g., CSV)
- Further UI virtualization for very large rosters
- Automated tests for JSON import/export and persistence
- Include and Search new cats.

## 📄 License

This repository is **source-available** under:

- **PolyForm Noncommercial 1.0.0**
- **Additional Terms** in this repository, including:
  - no commercial use
  - no AI/ML use

See [LICENSE](LICENSE) for the full terms.
