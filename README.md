# HomeOrOffice

HomeOrOffice is a small Windows desktop application for recording whether a working day was spent **at home**, **in the office**, or on a **business trip**.

It is a personal hobby project that I built for my own day-to-day use: a lightweight way to keep an overview of work locations without relying on a calendar or an external service.

## Features

- Record a work location for any date using a calendar
- Review, replace, or delete existing entries
- Optional automatic prompt when Windows starts
- Store data locally in an SQLite database
- Display yearly totals for home office, office, business trips, and unrecorded working days
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

- C# / .NET Framework 4.8.1
- WPF
- SQLite (`System.Data.SQLite.Core`)
- ClosedXML for Excel export
- Inno Setup for the Windows installer

## Getting started

### Install the application

The installer is available as `Setup/HomeOrOffice_Setup.exe`. Run it and choose whether a desktop shortcut should be created. The application can also create a Windows startup shortcut, so it can ask for the current work location when you log in.

### Build from source

1. Open `src/HomeOrOffice.sln` in Visual Studio.
2. Restore NuGet packages.
3. Build the `Release` configuration.

The project targets Windows and requires .NET Framework 4.8.1.

## Notes

All work-location data is stored locally on the computer. This project is intentionally small and focused on a practical personal workflow.
