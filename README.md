# HomeOrOffice

HomeOrOffice is a small Windows desktop application for recording whether a working day was spent **at home**, **in the office**, or on a **business trip**.

It is a personal hobby project that I built for my own day-to-day use: a lightweight way to keep an overview of work locations without relying on a calendar or an external service.

## Features

- Record a work location for any date using a calendar
- Review, replace, or delete existing entries
- Automatic prompt when you log in to Windows, shown only if today has no entry yet
- Store data locally in an SQLite database
- Display yearly totals for home office, office, business trips, and unrecorded working days (Monday to Friday up to today; public holidays are not taken into account)
- Export a complete year to an Excel (`.xlsx`) file
- Switch the UI between English and German
- Install the application with an Inno Setup installer

## Screenshots

### Calendar and daily entry

The calendar shows the selected day and the status of an existing entry. Work locations are displayed with different colours.

![Home office entry](docs/images/Screenshot%202026-09-09%20084621.png)

![Office entry](docs/images/Screenshot%202026-09-09%20092319.png)

### Safeguards for existing and future entries

The application asks for confirmation before overwriting an entry or saving an entry for a future date.

![Overwrite confirmation](docs/images/Screenshot%202026-09-09%20092446.png)

![Future-date confirmation](docs/images/Screenshot%202026-09-09%20092520.png)

### Year overview and Excel export

The export view provides yearly totals and creates an Excel file containing every day of the selected year.

![Yearly overview](docs/images/Screenshot%202026-09-09%20092553.png)

## Technology

- C# / .NET 10 (`net10.0-windows`)
- WPF
- SQLite (`System.Data.SQLite.Core`)
- ClosedXML for Excel export
- Inno Setup for the Windows installer

## Getting started

### Requirements

- Windows
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0) — the installer does not include it, so install it first if it is not already on the computer

### Install the application

The installer is available as `Setup/HomeOrOffice_Setup.exe`. Run it and choose whether a desktop shortcut should be created.

The installer also adds a shortcut to the Windows startup folder. It starts HomeOrOffice with the `auto` parameter when you log in: the window only appears if no work location has been recorded for today yet. To turn this off, delete the HomeOrOffice shortcut from the startup folder (`shell:common startup`).

### Build from source

You need the .NET 10 SDK (included in Visual Studio 2026).

```powershell
dotnet build src/HomeOrOffice.sln -c Release
```

Alternatively, open `src/HomeOrOffice.sln` in Visual Studio and build the `Release` configuration. The output is written to `src/HomeOrOffice/bin/Release/net10.0-windows/`.

### Build the installer

Build the `Release` configuration first, then compile `Setup/Setup.iss` with Inno Setup. The script takes its files from `src/HomeOrOffice/bin/Release/net10.0-windows/` and writes `HomeOrOffice_Setup.exe` to the `Setup` folder.

## Data storage

All work-location data is stored locally in an SQLite database:

```
C:\ProgramData\HomeOrOffice\Datenbank\HomeOrOfficeDB.db
```

The database contains one table per year. If the folder does not exist, the application creates it on startup. The data is kept when the application is uninstalled.

## Notes

This project is intentionally small and focused on a practical personal workflow.
