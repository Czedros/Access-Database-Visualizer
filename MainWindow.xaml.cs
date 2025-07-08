using System.Data;
using System.Data.OleDb;
using System.Windows;
using Microsoft.Win32;

namespace WPF_Visualizer_Temp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string _accessFilePath = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
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


    }
}