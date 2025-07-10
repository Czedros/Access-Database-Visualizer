using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;



namespace WPF_Visualizer_Temp
{
    public partial class MainWindow : Window
    {
        private string _accessFilePath = string.Empty;
        private const string BookmarkFile = "bookmarks.json";
        private List<Bookmark> _bookmarks = new();
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
                    BookmarksListBox.Visibility = Visibility.Collapsed;
                    TablesListBox.Visibility = Visibility.Visible;
                    SidebarTitle.Text = "Tables";
                    showingBookmarks = false;

                    // Clear previous selection
                    TablesListBox.SelectedItem = null;
                    DataGridDisplay.ItemsSource = null;
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
                    _bookmarks = JsonSerializer.Deserialize<List<Bookmark>>(json) ?? new List<Bookmark>();
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
            e.Handled = true; // Prevent selection change

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
                    var schema = GetTableSchema(_accessFilePath, tableName);
                    var editWindow = new NewEntryPage(schema, tableName, rowView.Row);

                    if (editWindow.ShowDialog() == true)
                    {
                        UpdateRow(_accessFilePath, tableName, rowView.Row, editWindow.EntryValues);
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

        private void UpdateRow(string dbPath, string tableName, DataRow row, Dictionary<string, object> newValues)
        {
            var keyColumn = row.Table.PrimaryKey.FirstOrDefault();
            if (keyColumn == null)
                throw new Exception("No primary key found for table.");

            var keyValue = row[keyColumn];
            var updates = string.Join(",", newValues.Select(kv => $"[{kv.Key}] = @{kv.Key}"));

            var connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";
            using var connection = new OleDbConnection(connectionString);
            using var command = new OleDbCommand(
                $"UPDATE [{tableName}] SET {updates} WHERE [{keyColumn.ColumnName}] = @key", connection);

            foreach (var kv in newValues)
                command.Parameters.AddWithValue($"@{kv.Key}", kv.Value ?? DBNull.Value);

            command.Parameters.AddWithValue("@key", keyValue);
            connection.Open();
            command.ExecuteNonQuery();
        }

        private void DeleteRow(string dbPath, string tableName, DataRow row)
        {
            var keyColumn = row.Table.PrimaryKey.FirstOrDefault();
            if (keyColumn == null)
                throw new Exception("No primary key found for table.");

            var keyValue = row[keyColumn];
            var connectionString = $"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";
            using var connection = new OleDbConnection(connectionString);
            using var command = new OleDbCommand(
                $"DELETE FROM [{tableName}] WHERE [{keyColumn.ColumnName}] = @key", connection);

            command.Parameters.AddWithValue("@key", keyValue);
            connection.Open();
            command.ExecuteNonQuery();
        }
    }
}