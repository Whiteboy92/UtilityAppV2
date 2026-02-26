# UtilityAppV2

UtilityAppV2 is a Windows-only desktop toolbox built with WPF (.NET 10, C# 14) that streamlines common media and file‑management workflows such as playlist downloading, bulk filename fixing, file moves, and image conversions. It wraps popular command-line tooling like `yt-dlp` and `ffmpeg` in a cohesive, modern UI.

---

## Table of Contents
- [Architecture Overview](#architecture-overview)
- [Feature Highlights](#feature-highlights)
- [Tech Stack](#tech-stack)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Configuration & Settings](#configuration--settings)
- [Extensibility](#extensibility)
- [License](#license)

---

## Architecture Overview

UtilityAppV2 follows a clean MVVM layout with light dependency injection:

- **Views (`Views/`)**  
  XAML pages per feature—e.g., `DownloadPlaylistView`, `FixFileNamesView`, `MoveFilesView`, plus reusable UI components.

- **ViewModels (`ViewModels/`)**  
  Each view has a dedicated view model deriving from `BaseViewModel`, exposing observable state, validation, and `RelayCommand` actions. Async operations (e.g., playlist downloads) are orchestrated here with cancellation support and progress reporting.

- **Commands (`Commands/RelayCommand.cs`)**  
  A lightweight relay command enables binding arbitrary delegates to UI actions, ensuring command availability updates when view-model state changes.

- **Core (`Core/`)**  
  Shared infrastructure such as `BaseViewModel` (implements `INotifyPropertyChanged`) and utility constants (e.g., `FilePathConstants` for bundled tools).

- **Services (`Services/`)**  
  Interfaces plus implementation classes encapsulate domain logic:
    - `DownloadService`: wraps `yt-dlp`, `ffmpeg`, and `ffprobe`, translating user input into background downloads with progress callbacks.
    - `FileConversionService`, `FileFixerService`, `MoveFilesService`, etc.: handle PNG conversion, filename normalization (via `RegexFileNameCleaner`), directory reorganizations, and more.
    - `DialogService`: centralizes WPF dialog interactions (using `Ookii.Dialogs.Wpf` for folder/file pickers).
    - `SettingsService`: async persistence for `UserSettings`, keeping preferences (playlist URL, last downloaded track, etc.).

- **Models (`Models/`)**  
  Typed result objects like `DownloadResult`, `FileMoveResult`, `ConversionResult`, giving the UI structured feedback (counts, durations, failure info).

- **Resources & Tools (`Resources/`)**  
  Ship-with binaries (`yt-dlp.exe`, `ffmpeg.exe`, `ffprobe.exe`) are copied alongside the app at build time (`CopyToOutputDirectory=PreserveNewest`). Icons and theme assets live under `Resources/Icons` and `UI Components`.

Dependency injection (via `Microsoft.Extensions.DependencyInjection`) wires services and view models at startup (`App.xaml.cs`), keeping cross-feature coupling minimal and simplifying testing.

---

## Feature Highlights

- **Playlist Downloader**
    - Paste a playlist link, specify how many tracks to fetch, and let `yt-dlp` + `ffmpeg` grab high-quality MP3s.
    - Live progress bar, cancellation, and auto-saving of the “first downloaded song” for quick relaunches.

- **Filename Fixer**
    - Batch clean inconsistent or messy filenames using configurable regex rules (accents, spacing, numbering, etc.).
    - Preview results and log successes/errors per file.

- **File Mover**
    - Apply move rules (by extension, artist folder, custom patterns) to reorganize libraries in bulk.
    - Detailed summary via `FileMoveResult` keeps auditing easy.

- **Image Converter**
    - Convert arbitrary image formats to PNG (leveraging `Magick.NET-Q8-x64`) with optional resizing or compression presets.

- **Modern WPF Shell**
    - Custom title bar and theming (via the `WPF-UI` NuGet package) for a polished desktop feel.
    - Reusable dialogs and message boxes in `UI Components`.

---

## Tech Stack

| Layer            | Technology / Package                                 |
|------------------|-------------------------------------------------------|
| UI               | WPF (XAML), WPF-UI (4.1.0)                            |
| Language         | C# 14.0                                               |
| Runtime Target   | .NET 10.0 (Windows)                                   |
| DI & Hosting     | `Microsoft.Extensions.DependencyInjection`            |
| Media Tooling    | `yt-dlp`, `ffmpeg`, `ffprobe` (bundled)               |
| Image Processing | `Magick.NET-Q8-x64` 14.9.1                            |
| Dialogs          | `Ookii.Dialogs.Wpf` 5.0.1                             |
| Metadata         | `TagLibSharp` 2.3.0                                   |

---

## Getting Started

### Prerequisites

- **.NET 10 SDK** – Ensure it is installed on your Windows machine.
- **Git** – Required for cloning the repository.
- **Git LFS** – Used to manage large binaries (`ffmpeg.exe`, `ffprobe.exe`). Install it from [git-lfs.github.com](https://git-lfs.github.com/) and run:

```bash
git lfs install