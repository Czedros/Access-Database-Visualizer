using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;



namespace WPF_Visualizer_Temp
{
    public partial class MainWindow : Window
    {
        private string _accessFilePath = string.Empty;
        private const string BookmarkFile = "bookmarks.json";
        private ObservableCollection<Bookmark> _bookmarks = new();
        private bool showingBookmarks = true;

        public MainWindow()
        {
            InitializeComponent();
            LoadBookmarks();
            RefreshBookmarkView();
        }

        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            showingBookmarks = !showingBookmarks;

            TablesScroll.Visibility = showingBookmarks ? Visibility.Collapsed : Visibility.Visible;
            BookmarksScroll.Visibility = showingBookmarks ? Visibility.Visible : Visibility.Collapsed;
            SidebarTitle.Text = showingBookmarks ? "Bookmarks" : "Tables";
        }


        private void OpenFile_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new OpenFileDialog
            {
                Filter = "Access Database Files (*.accdb;*.mdb)|*.accdb;*.mdb",
                Title = "Select an Access Database File"
            };

            if (fileDialog.ShowDialog() == true)
            {
                _accessFilePath = fileDialog.FileName;
                FilePathText.Text = _accessFilePath;

                try
                {
                    var tableNames = GetAccessTableNames(_accessFilePath);
                    TablesListBox.ItemsSource = tableNames;

                    // Always switch to tables view
                    showingBookmarks = false;
                    TablesScroll.Visibility = Visibility.Visible;
                    BookmarksScroll.Visibility = Visibility.Collapsed;
                    SidebarTitle.Text = "Tables";

                    // Clear previous selection
                    TablesListBox.SelectedItem = null;
                    DataGridDisplay.ItemsSource = null;
                    RefreshBookmarkView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading database:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string[] GetAccessTableNames(string dbPath)
        {
            var connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";
            using var connection = new OleDbConnection(connectionString);
            connection.Open();

            // Get schema for user tables only
            var schema = connection.GetSchema("Tables");
            var tableNames = new System.Collections.Generic.List<string>();

            foreach (DataRow row in schema.Rows)
            {
                var tableType = row["TABLE_TYPE"]?.ToString();
                var tableName = row["TABLE_NAME"]?.ToString();

                if (tableType == "TABLE" && tableName != null)
                {
                    tableNames.Add(tableName);
                }
            }

            return tableNames.ToArray();
        }


        private void TablesListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (TablesListBox.SelectedItem is string tableName && !string.IsNullOrEmpty(_accessFilePath))
            {
                try
                {
                    var dataTable = LoadTableData(_accessFilePath, tableName);
                    DataGridDisplay.ItemsSource = dataTable.DefaultView;
                    
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load data from table '{tableName}':\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private DataTable LoadTableData(string dbPath, string tableName)
        {
            var connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";
            using var connection = new OleDbConnection(connectionString);
            using var command = new OleDbCommand($"SELECT * FROM [{tableName}]", connection);
            using var adapter = new OleDbDataAdapter(command);

            var dataTable = new DataTable();
            connection.Open();
            adapter.FillSchema(dataTable, SchemaType.Source);
            adapter.Fill(dataTable);
            return dataTable;
        }

        private void NewEntry_Click(object sender, RoutedEventArgs e)
        {
            if (TablesListBox.SelectedItem is not string tableName) return;

            try
            {
                var schema = GetTableSchema(_accessFilePath!, tableName);
                var entryWindow = new NewEntryPage(schema, tableName);
                if (entryWindow.ShowDialog() == true)
                {
                    InsertNewRow(_accessFilePath!, tableName, entryWindow.EntryValues);
                    var updatedTable = LoadTableData(_accessFilePath!, tableName);
                    DataGridDisplay.ItemsSource = updatedTable.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error preparing entry form:\n{ex.Message}");
            }
        }

        private DataTable GetTableSchema(string dbPath, string tableName)
        {
            var connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";
            using var connection = new OleDbConnection(connectionString);
            using var command = new OleDbCommand($"SELECT * FROM [{tableName}] WHERE 1=0", connection); // no data, just schema
            using var adapter = new OleDbDataAdapter(command);

            var table = new DataTable();
            connection.Open();
            adapter.FillSchema(table, SchemaType.Source);

            return table;
        }

        private void InsertNewRow(string dbPath, string tableName, Dictionary<string, object> values)
        {
            var columnNames = string.Join(",", values.Keys.Select(k => $"[{k}]"));
            var paramNames = string.Join(",", values.Keys.Select(k => $"@{k}"));

            var connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";
            using var connection = new OleDbConnection(connectionString);
            using var command = new OleDbCommand($"INSERT INTO [{tableName}] ({columnNames}) VALUES ({paramNames})", connection);

            foreach (var pair in values)
            {
                command.Parameters.AddWithValue($"@{pair.Key}", pair.Value ?? DBNull.Value);
            }

            connection.Open();
            command.ExecuteNonQuery();
        }


        private void SaveBookmarks()
        {
            var json = JsonSerializer.Serialize(_bookmarks, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(BookmarkFile, json);
        }

        private void BookmarkTableFromList_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true; // Prevent click from reaching ListBoxItem

            if (sender is Button btn && btn.Tag is string tableName && !string.IsNullOrEmpty(_accessFilePath))
            {
                if (_bookmarks.Any(b => b.DatabasePath == _accessFilePath && b.TableName == tableName))
                {
                    MessageBox.Show("This table is already bookmarked.");
                    return;
                }

                var bookmark = new Bookmark
                {
                    DatabasePath = _accessFilePath,
                    TableName = tableName
                };

                _bookmarks.Add(bookmark);
                SaveBookmarks();
                RefreshBookmarkView();
                MessageBox.Show($"Bookmarked: {System.IO.Path.GetFileName(_accessFilePath)} → {tableName}", "Bookmark Added");
            }
        }


        private void BookmarksListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BookmarksListBox.SelectedItem is Bookmark bookmark)
            {
                LoadBookmark(bookmark);
            }
        }

        private void LoadBookmarks()
        {
            if (File.Exists(BookmarkFile))
            {
                try
                {
                    var json = File.ReadAllText(BookmarkFile);
                    var loaded = JsonSerializer.Deserialize<List<Bookmark>>(json) ?? new List<Bookmark>();
                    _bookmarks = new ObservableCollection<Bookmark>(loaded);
                    // Show Bookmarks view by default
                    TablesScroll.Visibility = Visibility.Collapsed;
                    BookmarksScroll.Visibility = Visibility.Visible;
                    SidebarTitle.Text = "Bookmarks";
                    showingBookmarks = true;

                    BookmarksListBox.SelectedItem = null;
                    RefreshBookmarkView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load bookmarks:\n{ex.Message}", "Error");
                }
            }
        }
        private void LoadBookmark(Bookmark bookmark)
        {
            try
            {
                _accessFilePath = bookmark.DatabasePath;
                FilePathText.Text = _accessFilePath;

                var tableNames = GetAccessTableNames(_accessFilePath);
                TablesListBox.ItemsSource = tableNames;

                // Set and load the bookmarked table
                TablesListBox.SelectedItem = bookmark.TableName;
                var dataTable = LoadTableData(_accessFilePath, bookmark.TableName);
                DataGridDisplay.ItemsSource = dataTable.DefaultView;
                RefreshBookmarkView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load bookmark:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteBookmark_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            if (sender is Button btn && btn.Tag is Bookmark bookmark)
            {
                var result = MessageBox.Show($"Delete bookmark for '{bookmark.TableName}'?",
                                             "Confirm Delete",
                                             MessageBoxButton.YesNo,
                                             MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _bookmarks.Remove(bookmark);
                    SaveBookmarks();

                    BookmarksListBox.SelectedItem = null;
                    DataGridDisplay.ItemsSource = null;
                    FilePathText.Text = string.Empty;
                }
                RefreshBookmarkView();
            }
        }
        private void RefreshBookmarkView()
        {
            var cvs = (CollectionViewSource)FindResource("BookmarkView");
            cvs.Source = _bookmarks;

            cvs.GroupDescriptions.Clear();
            var converter = (IValueConverter)Application.Current.Resources["FileNameConverter"];
            cvs.GroupDescriptions.Add(new PropertyGroupDescription("DatabasePath", converter));

            cvs.View?.Refresh();
        }

        private void DataGridDisplay_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Actions")
            {
                e.Cancel = true;
            }
        }

        private void EditRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DataRowView rowView && TablesListBox.SelectedItem is string tableName)
            {
                try
                {
                    // Use schema only for form, not for the row!
                    var schema = GetTableSchema(_accessFilePath, tableName);
                    var originalValues = rowView.Row.Table.Columns
                        .Cast<DataColumn>()
                        .ToDictionary(c => c.ColumnName, c => rowView.Row[c]);

                    var editWindow = new NewEntryPage(schema, tableName, rowView.Row);

                    if (editWindow.ShowDialog() == true)
                    {
                        // Use the original row from the DataGrid's DataTable
                        UpdateRow(_accessFilePath, tableName, originalValues, editWindow.EntryValues);
                        var updatedTable = LoadTableData(_accessFilePath, tableName);
                        DataGridDisplay.ItemsSource = updatedTable.DefaultView;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to edit row:\n{ex.Message}");
                }
            }
        }

        private void DeleteRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is DataRowView rowView && TablesListBox.SelectedItem is string tableName)
            {
                var result = MessageBox.Show("Are you sure you want to delete this row?", "Confirm Delete", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes) return;

                try
                {
                    DeleteRow(_accessFilePath, tableName, rowView.Row);
                    var updatedTable = LoadTableData(_accessFilePath, tableName);
                    DataGridDisplay.ItemsSource = updatedTable.DefaultView;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to delete row:\n{ex.Message}");
                }
            }
        }

        private void UpdateRow(string dbPath, string tableName, Dictionary<string, object> originalValues, Dictionary<string, object> newValues)
        {
            var setClause = string.Join(", ", newValues.Keys.Select(k => $"[{k}] = @{k}"));
            var whereClause = string.Join(" AND ", originalValues.Keys.Select(k => $"[{k}] = @old_{k}"));

            var connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";
            using var connection = new OleDbConnection(connectionString);
            using var command = new OleDbCommand(
                $"UPDATE [{tableName}] SET {setClause} WHERE {whereClause}", connection);

            // Add new values as parameters
            foreach (var (key, value) in newValues)
                command.Parameters.AddWithValue($"@{key}", value ?? DBNull.Value);

            // Add original values for WHERE clause
            foreach (var (key, value) in originalValues)
                command.Parameters.AddWithValue($"@old_{key}", value ?? DBNull.Value);

            connection.Open();
            int affected = command.ExecuteNonQuery();

            if (affected == 0)
            {
                MessageBox.Show("Update failed. The original row may not exist anymore or values may have changed.", "No Match", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeleteRow(string dbPath, string tableName, DataRow row)
        {
            var connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";
            using var connection = new OleDbConnection(connectionString);
            var command = new OleDbCommand();
            command.Connection = connection;

            string whereClause;
            if (row.Table.PrimaryKey.Length > 0)
            {
                var keyColumn = row.Table.PrimaryKey[0];
                var keyValue = row[keyColumn];
                whereClause = $"[{keyColumn.ColumnName}] = @key";
                command.Parameters.AddWithValue("@key", keyValue);
            }
            else
            {
                MessageBox.Show(
                    "Warning: This table has no primary key. The deletion will match all column values to identify the row, which may affect multiple rows.",
                    "No Primary Key",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                whereClause = string.Join(" AND ", row.Table.Columns
                    .Cast<DataColumn>()
                    .Where(c => !c.AutoIncrement)
                    .Select(c =>
                    {
                        var param = $"@old_{c.ColumnName}";
                        command.Parameters.AddWithValue(param, row[c] ?? DBNull.Value);
                        return $"[{c.ColumnName}] = {param}";
                    }));
            }
            command.CommandText = $"DELETE FROM [{tableName}] WHERE {whereClause}";
            connection.Open();
            command.ExecuteNonQuery();
        }

        private Popup? _popup;
        private ColumnFilterPopup? _filterPopup;
        private string? _activeColumn;

        private void HeaderFilter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string columnName && DataGridDisplay.ItemsSource is DataView view)
            {
                // Close previous popup
                if (_popup != null)
                {
                    _popup.IsOpen = false;
                    _popup = null;
                }

                _activeColumn = columnName;

                // Collect unique values from the DataView
                var valuesTable = view.ToTable(true, columnName);
                var values = valuesTable
                    .AsEnumerable()
                    .Select(row => row[columnName]?.ToString() ?? "")
                    .OrderBy(v => v)
                    .ToList();

                // Create and configure popup
                _filterPopup = new ColumnFilterPopup();
                _filterPopup.LoadValues(values.ToList());

                _filterPopup.FilterApplied += ApplyFilter;
                _filterPopup.SortAscending += () => SortColumn(columnName, ascending: true);
                _filterPopup.SortDescending += () => SortColumn(columnName, ascending: false);
                _filterPopup.ResetFilter += ResetColumnFilter;

                _popup = new Popup
                {
                    PlacementTarget = btn,
                    Placement = PlacementMode.Bottom,
                    StaysOpen = false,
                    Child = _filterPopup,
                    AllowsTransparency = true,
                    PopupAnimation = PopupAnimation.Fade,
                    MinWidth = 200
                };

                _popup.IsOpen = true;
            }
        }
        private void ApplyFilter(List<string> selectedValues)
        {
            if (_activeColumn != null && DataGridDisplay.ItemsSource is DataView view)
            {
                var escaped = selectedValues
                    .Select(v => $"'{v.Replace("'", "''")}'");

                view.RowFilter = $"{_activeColumn} IN ({string.Join(",", escaped)})";
            }

            if (_popup != null)
                _popup.IsOpen = false;
        }

        private void SortColumn(string columnName, bool ascending)
        {
            if (DataGridDisplay.ItemsSource is DataView view)
            {
                view.Sort = $"{columnName} {(ascending ? "ASC" : "DESC")}";
            }

            if (_popup != null)
                _popup.IsOpen = false;
        }

        private void ResetColumnFilter()
        {
            if (DataGridDisplay.ItemsSource is DataView view)
            {
                view.RowFilter = "";
                view.Sort = "";
            }

            if (_popup != null)
                _popup.IsOpen = false;
        }
    }
}