# Access Database Visualizer

A lightweight WPF application for viewing and interacting with Microsoft Access databases (.accdb / .mdb) in a clean, user-friendly interface.
Built in C# for speed and responsiveness, the tool provides a fast way to inspect, search, and manage Access database content without requiring Microsoft Access. 

## Features
Basic CRUD cycle is available

Features
1. Table Browsing
  - Open any Access database file and view its tables.
  - Paginated DataGrid for responsive browsing of large tables.
  - Automatically lists all available tables in the connected database.
2. Column Filtering
- Click column headers to filter records (similar to Excel / Access filter UI).
- Supports multiple filter criteria on different columns.
3. Record Editing
- Edit existing entries directly in the DataGrid.
- Add new records through a dedicated “New Entry” page.
- Changes are saved back to the database.
4. Bookmarking
- Bookmark frequently accessed tables for quick access.
- Bookmarks are persisted across sessions.
5. Search
- Search across the current table’s data.
- Case-insensitive match for quick lookups.


## Pre-reqs for this project:
- Windows OS
- .NET Framework (version matching project settings)
- Microsoft Access Database Engine (ACE) OLEDB provider installed


## Quick Start Guide
1. Clone the repository: 
```bash
git clone https://github.com/yourusername/Access-Database-Visualizer.git
```
2. Open the Solution in Visual Studio
3. Build the project to restore dependencies
4. Run the application

## Note
If you are looking to run this as a desktop app, please publish it via visual studio.

## Future Enhancements
- Export selected data to CSV/Excel.
- Support SQL query execution.
- Add inline data type conversion and validation.
= Introduce sorting and filtering persistence per table.
