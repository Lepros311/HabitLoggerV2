using System.Data.SQLite;
using System.Globalization;

string? userInput;
bool isValidInput = false;
int menuChoice;
int[] menuNumbers = [0, 1, 2, 3, 4, 5, 6, 7, 8, 9];

string dbPath = @"C:\Users\Andrew\Documents\coding stuff\C#\The C# Academy\HabitLogger2\bin\data\mydatabase.db";

Console.Title = "Habit Logger v2";

using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
{
    connection.Open();
    CreateTables(connection);

    if (GetHabitCount(connection) == 0)
    {
        SeedData();
    }
}

do
{
    PrintMainMenu();
    menuChoice = GetMenuChoice();

    switch (menuChoice)
    {
        case 0:
            Console.WriteLine("\nGoodbye!");
            Thread.Sleep(2000);
            Environment.Exit(0);
            break;
        case 1:
            PrintAllRecords("View All Records");
            ReturnToMainMenu();
            break;
        case 2:
            AddRecord();
            ReturnToMainMenu();
            break;
        case 3:
            EditRecord();
            ReturnToMainMenu();
            break;
        case 4:
            DeleteRecord();
            ReturnToMainMenu();
            break;
        case 5:
            PrintHabits("View All Habits");
            ReturnToMainMenu();
            break;
        case 6:
            AddHabit();
            ReturnToMainMenu();
            break;
        case 7:
            EditHabit();
            ReturnToMainMenu();
            break;
        case 8:
            DeleteHabit();
            ReturnToMainMenu();
            break;
        case 9:
            PrintReport();
            ReturnToMainMenu();
            break;
        default:
            Console.WriteLine("Invalid choice.");
            break;
    }
} while (menuChoice != 0);

static void CreateTables(SQLiteConnection connection)
{
    using (var command = connection.CreateCommand())
    {
        command.CommandText = @"
            CREATE TABLE IF NOT EXISTS Habits (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Habit TEXT NOT NULL,
                Unit TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS HabitInstances (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                HabitId INTEGER NOT NULL,
                Date TEXT NOT NULL,
                Quantity INTEGER NOT NULL,
                FOREIGN KEY (HabitId) REFERENCES Habits(Id) ON DELETE CASCADE
            );";

        command.ExecuteNonQuery();
    }
}

static int GetHabitCount(SQLiteConnection connection)
{
    using (var command = connection.CreateCommand())
    {
        command.CommandText = "SELECT COUNT(*) FROM Habits";
        return Convert.ToInt32(command.ExecuteScalar());
    }
}

void SeedData()
{
    using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
    {
        connection.Open();

        Random random = new Random();
        string[] habitNames = { "Exercise", "Read", "Drink Water" };

        var habitUnits = new Dictionary<string, string>
        {
            { "Exercise", "reps" },
            { "Read", "pages" },
            { "Drink Water", "glasses" }
        };

        foreach (var habit in habitNames)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "INSERT INTO Habits (Habit, Unit) VALUES (@habit, @unit)";
                command.Parameters.AddWithValue("@habit", habit);
                command.Parameters.AddWithValue("@unit", habitUnits[habit]);
                command.ExecuteNonQuery();
            }
        }

        for (int i = 0; i < 30; i++)
        {
            string habit = habitNames[random.Next(habitNames.Length)];

            int habitId;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Id FROM Habits WHERE Habit = @habit";
                command.Parameters.AddWithValue("@habit", habit);
                habitId = Convert.ToInt32(command.ExecuteScalar());
            }

            DateTime startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime today = DateTime.Now;

            int range = (today - startOfMonth).Days;
            DateTime randomDate = startOfMonth.AddDays(random.Next(range));

            string date = randomDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            int quantity = random.Next(1, 11);
            string unit = habitUnits[habit];

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "INSERT INTO HabitInstances (HabitId, Date, Quantity) VALUES (@habitId, @date, @quantity)";
                command.Parameters.AddWithValue("@habitId", habitId);
                command.Parameters.AddWithValue("@date", date);
                command.Parameters.AddWithValue("@quantity", quantity);
                command.ExecuteNonQuery();
            }
        }
    }
}

void PrintMainMenu()
{
    Console.Clear();
    Console.WriteLine("====================");
    Console.WriteLine("MAIN MENU");
    Console.WriteLine("====================\n");
    Console.WriteLine("----------");
    Console.WriteLine("Type 0 to Close Application.");
    Console.WriteLine("----------");
    Console.WriteLine("Type 1 to View All Records.");
    Console.WriteLine("Type 2 to Add Record.");
    Console.WriteLine("Type 3 to Edit Record.");
    Console.WriteLine("Type 4 to Delete Record.");
    Console.WriteLine("----------");
    Console.WriteLine("Type 5 to View All Habits.");
    Console.WriteLine("Type 6 to Add Habit.");
    Console.WriteLine("Type 7 to Edit Habit.");
    Console.WriteLine("Type 8 to Delete Habit.");
    Console.WriteLine("----------");
    Console.WriteLine("Type 9 to View Report.");
    Console.WriteLine("----------");
    Console.WriteLine();
}

int GetMenuChoice()
{
    do
    {
        isValidInput = false;
        Console.WriteLine("What would you like to do?");
        userInput = Console.ReadLine();
        if ((int.TryParse(userInput, out menuChoice)) && (Array.IndexOf(menuNumbers, Convert.ToInt32(userInput)) >= 0))
            isValidInput = true;
        else
            Console.WriteLine("Invalid input. Try again.");
    } while (isValidInput == false);
    return menuChoice;
}

void ReturnToMainMenu()
{
    Console.Write("\nPress any key to return to the Main Menu...");
    Console.ReadKey();
}

void PrintReport()
{
    using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
    {
        connection.Open();

        Console.Clear();

        string totalHabitsQuery = "SELECT Habit, Unit FROM Habits ORDER BY Habit";
        using (var command = new SQLiteCommand(totalHabitsQuery, connection))
        using (var reader = command.ExecuteReader())
        {
            var habits = new List<(string Habit, string Unit)>();

            while (reader.Read())
            {
                habits.Add((reader["Habit"].ToString(), reader["Unit"].ToString()));
            }

            int totalHabits = habits.Count;
            Console.WriteLine("====================");
            Console.WriteLine("REPORT");
            Console.WriteLine("====================\n");
            Console.WriteLine($"Total Habits Tracked: {totalHabits}");
            Console.WriteLine("List of Habits:");
            foreach (var habit in habits)
            {
                Console.WriteLine($"- {habit.Habit}");
            }
            Console.WriteLine();
        }

        string ytdQuery = @"
        SELECT h.Habit,
            COUNT(DISTINCT hi.Date) AS TotalDays,
            SUM(hi.Quantity) AS TotalUnits,
            h.Unit
        FROM HabitInstances hi
        JOIN Habits h ON hi.HabitId = h.Id
        WHERE hi.Date >= DATE('now', 'start of year')
        GROUP BY h.Habit, h.Unit";

        using (var command = new SQLiteCommand(ytdQuery, connection))
        using (var reader = command.ExecuteReader())
        {
            Console.WriteLine("YTD:");
            while (reader.Read())
            {
                string? habit = reader["Habit"].ToString();
                int totalDays = Convert.ToInt32(reader["TotalDays"]);
                int totalUnits = Convert.ToInt32(reader["TotalUnits"]);
                string? unit = reader["Unit"].ToString();

                Console.WriteLine($"{habit} - {totalDays} days, {totalUnits} {unit}");
            }

            var longestStreaks = CalculateStreak();
            Console.WriteLine($"\nLongest Streaks:");
            foreach (var streak in longestStreaks)
            {
                Console.WriteLine($"{streak.Key} - {streak.Value} days in a row");
            }
        }
    }
}

Dictionary<string, int> CalculateStreak()
{
    using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
    {
        connection.Open();

        var longestStreaks = new Dictionary<string, int>();

        string query = @"
        SELECT h.Habit, hi.Date
        FROM HabitInstances hi
        JOIN Habits h ON hi.HabitId = h.Id
        ORDER BY h.Habit, hi.Date";

        using (var command = new SQLiteCommand(query, connection))
        using (var reader = command.ExecuteReader())
        {
            string? currentHabit = null;
            int currentStreak = 0;
            int longestStreak = 0;
            DateTime? lastDate = null;

            while (reader.Read())
            {
                string? habit = reader["Habit"].ToString();
                DateTime date = DateTime.Parse(reader["Date"].ToString(), CultureInfo.InvariantCulture);

                if (currentHabit != habit)
                {
                    if (currentHabit != null)
                    {
                        longestStreaks[currentHabit] = Math.Max(longestStreaks.GetValueOrDefault(currentHabit, 0), longestStreak);
                    }

                    currentHabit = habit;
                    currentStreak = 1;
                    longestStreak = 1;
                    lastDate = date;
                }
                else
                {
                    if (lastDate.HasValue && (date - lastDate.Value).Days == 1)
                    {
                        currentStreak++;
                    }
                    else
                    {
                        currentStreak = 1;
                    }

                    longestStreak = Math.Max(longestStreak, currentStreak);
                }

                lastDate = date;
            }

            if (currentHabit != null)
            {
                longestStreaks[currentHabit] = Math.Max(longestStreaks.GetValueOrDefault(currentHabit, 0), longestStreak);
            }
        }

        return longestStreaks;
    }
}

void AddHabit()
{
    using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
    {
        connection.Open();

        Console.Clear();
        PrintHabits("Add Habit");

        Console.WriteLine("\nEnter the new habit name:");
        string? habitName = Console.ReadLine();
        Console.WriteLine("\nEnter the unit of measurement (e.g., miles, pages, minutes, etc.)");
        string? unit = Console.ReadLine();

        using (var command = connection.CreateCommand())
        {
            command.CommandText = "INSERT INTO Habits (Habit, Unit) VALUES (@habit, @unit)";
            command.Parameters.AddWithValue("@habit", habitName);
            command.Parameters.AddWithValue("@unit", unit);
            command.ExecuteNonQuery();
        }

        Console.WriteLine("\nHabit added successfully!");
    }
}

void PrintHabits(string heading)
{
    using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
    {
        connection.Open();

        Console.Clear();
        Console.WriteLine("====================");
        Console.WriteLine(heading);
        Console.WriteLine("====================\n");

        string query = "SELECT Id, Habit, Unit FROM Habits ORDER BY Id";

        using (var command = new SQLiteCommand(query, connection))
        using (var reader = command.ExecuteReader())
        {
            if (!reader.HasRows)
            {
                Console.WriteLine("No habits found.");
                return;
            }

            Console.WriteLine("{0, -5} {1, -20} {2, -10}", "ID", "Habit", "Unit");
            Console.WriteLine(new string('-', 40));

            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string habit = reader.GetString(1);
                string unit = reader.GetString(2);

                Console.WriteLine("{0, -5} {1, -20} {2, -10}", id, habit, unit);
            }
        }
    }
}

void EditHabit()
{
    using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
    {
        connection.Open();

        Console.Clear();
        PrintHabits("Edit Habit");

        int habitId = 0;
        do
        {
            Console.Write("\nEnter the ID of the habit you want to edit: ");
            habitId = GetHabitId(habitId);
        } while (isValidInput == false);

        string checkHabitQuery = "SELECT Habit, Unit FROM Habits WHERE Id = @habitId";
        string? currentHabit = null;
        string? currentUnit = null;

        using (var command = connection.CreateCommand())
        {
            command.CommandText = checkHabitQuery;
            command.Parameters.AddWithValue("@habitId", habitId);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    currentHabit = reader["Habit"].ToString();
                    currentUnit = reader["Unit"].ToString();
                }
            }
        }

        Console.WriteLine($"\nHabit: {currentHabit}");
        Console.WriteLine($"Unit: {currentUnit}");

        Console.Write("\nEnter new habit name (leave blank to keep current): ");
        string? newHabitName = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(newHabitName))
        {
            newHabitName = currentHabit;
        }

        Console.Write("\nEnter a new unit (leave blank to keep current): ");
        string? newUnit = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(newUnit))
        {
            newUnit = currentUnit;
        }

        if (newHabitName == currentHabit && newUnit == currentUnit)
        {
            Console.WriteLine("\nNo changes were made.");
            return;
        }

        string updateHabitQuery = "UPDATE Habits SET Habit = @newHabit, Unit = @newUnit WHERE Id = @habitId";

        using (var command = connection.CreateCommand())
        {
            command.CommandText = updateHabitQuery;
            command.Parameters.AddWithValue("@newHabit", newHabitName);
            command.Parameters.AddWithValue("@newUnit", newUnit);
            command.Parameters.AddWithValue("@habitId", habitId);

            int rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                PrintHabits("Edit Habit");
                Console.WriteLine("\nHabit updated successfully!");
            }
        }
    }
}

void DeleteHabit()
{
    using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
    {
        connection.Open();

        Console.Clear();
        PrintHabits("Delete Habit");

        int habitId = 0;
        do
        {
            Console.Write("\nEnter the ID of the habit you want to delete: ");
            habitId = GetHabitId(habitId);
        } while (isValidInput == false);

        string? confirmation;
        do
        {
            Console.Write($"Are you sure you want to delete the habit with ID {habitId}? (y/n): ");
            confirmation = Console.ReadLine();
            if (confirmation?.ToLower() == "n")
            {
                Console.WriteLine("Deletion canceled.");
                return;
            }
            else if (confirmation?.ToLower() != "y")
            {
                Console.WriteLine("Invalid response.");
            }
        } while ((confirmation != "y") && (confirmation != "n"));

        string deleteHabitQuery = "DELETE FROM Habits WHERE Id = @habitId";

        using (var command = connection.CreateCommand())
        {
            command.CommandText = deleteHabitQuery;
            command.Parameters.AddWithValue("@habitId", habitId);
            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected > 0)
            {
                PrintHabits("Delete Habit");
                Console.WriteLine("\nHabit deleted successfully!");
            }
            else
            {
                Console.WriteLine("\nNo habit was deleted. Please try again.");
            }
        }
    }
}

void PrintAllRecords(string heading)
{
    using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
    {
        connection.Open();

        Console.Clear();
        Console.WriteLine("====================");
        Console.WriteLine(heading);
        Console.WriteLine("====================\n");

        string query = @"
        SELECT hi.Id, h.Habit, hi.Date, hi.Quantity, h.Unit
        FROM HabitInstances hi
        JOIN Habits h ON hi.HabitId = h.Id
        ORDER BY hi.Date DESC";

        using (var command = new SQLiteCommand(query, connection))
        using (var reader = command.ExecuteReader())
        {
            if (!reader.HasRows)
            {
                Console.WriteLine("No habits found.");
                return;
            }

            Console.WriteLine("{0, -5} {1, -20} {2, -15} {3, -10} {4, -10}", "ID", "Habit", "Date ▼", "Quantity", "Unit");
            Console.WriteLine(new string('-', 70));

            while (reader.Read())
            {
                int id = reader.GetInt32(0);
                string habit = reader.GetString(1);
                DateTime date = reader.GetDateTime(2);
                int quantity = reader.GetInt32(3);
                string unit = reader.GetString(4);

                Console.WriteLine("{0, -5} {1, -20} {2, -15:yyyy-MM-dd} {3, -10} {4, -10}", id, habit, date, quantity, unit);
            }
        }
    }
}

void AddRecord()
{
    using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
    {
        connection.Open();

        Console.Clear();
        PrintHabits("Add Record");

        int habitId = 0;
        do
        {
            Console.Write("\nEnter the ID of the habit for which you want to add a record: ");
            habitId = GetHabitId(habitId);
        } while (isValidInput == false);

        string checkHabitIdQuery = "SELECT Unit FROM Habits WHERE Id = @habitId";
        string? unit = null;

        using (var command = connection.CreateCommand())
        {
            command.CommandText = checkHabitIdQuery;
            command.Parameters.AddWithValue("@habitId", habitId);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    unit = reader.GetString(0);
                }
            }
        }

        DateTime date;
        do
        {
            Console.Write("\nEnter the Date (yyyy-MM-dd): ");
            if (!DateTime.TryParse(Console.ReadLine(), out date))
            {
                isValidInput = false;
                Console.WriteLine("Invalid date format. Please enter a date in the format yyyy-MM-dd.");
            }
            else
            {
                isValidInput = true;
            }
        } while (isValidInput == false);

        int quantity;
        do
        {
            Console.Write("\nEnter the quantity: ");
            if (!int.TryParse(Console.ReadLine(), out quantity))
            {
                isValidInput = false;
                Console.WriteLine("Invalid quantity. Please enter a numeric value.");
            }
            else
            {
                isValidInput = true;
            }
        } while (isValidInput == false);

        string insertQuery = @"
        INSERT INTO HabitInstances (HabitId, Date, Quantity)
        VALUES (@habitId, @date, @quantity)";

        using (var command = connection.CreateCommand())
        {
            command.CommandText = insertQuery;
            command.Parameters.AddWithValue("@habitId", habitId);
            command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@quantity", quantity);

            int rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                PrintAllRecords("Add Record");
                Console.WriteLine("\nRecord added successfully!");
            }
            else
            {
                Console.WriteLine("\nFailed to add record. Please try again.");
            }
        }
    }
}

int GetHabitId(int habitId)
{
    using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
    {
        connection.Open();

        if (!int.TryParse(Console.ReadLine(), out habitId))
        {
            isValidInput = false;
            Console.WriteLine("Invalid ID. Please enter a numeric value.");
        }
        else
        {
            string checkHabitIdQuery = "SELECT COUNT(*) FROM Habits WHERE Id = @habitId";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = checkHabitIdQuery;
                command.Parameters.AddWithValue("@habitId", habitId);

                int count = Convert.ToInt32(command.ExecuteScalar());
                if (count > 0)
                {
                    isValidInput = true;
                }
                else
                {
                    isValidInput = false;
                    Console.WriteLine("Habit not found. Please enter a valid habit ID.");
                }
            }
        }
        return habitId;
    }
}

int GetRecordId(int recordId)
{
    using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
    {
        connection.Open();

        if (!int.TryParse(Console.ReadLine(), out recordId))
        {
            isValidInput = false;
            Console.WriteLine("Invalid ID. Please enter a numeric value.");
        }
        else
        {
            string checkHabitInstanceIdQuery = "SELECT COUNT(*) FROM HabitInstances WHERE Id = @recordId";
            using (var command = connection.CreateCommand())
            {
                command.CommandText = checkHabitInstanceIdQuery;
                command.Parameters.AddWithValue("@recordId", recordId);

                int count = Convert.ToInt32(command.ExecuteScalar());
                if (count > 0)
                {
                    isValidInput = true;
                }
                else
                {
                    isValidInput = false;
                    Console.WriteLine("Record not found. Please enter a valid record ID.");
                }
            }
        }
        return recordId;
    }
}

void EditRecord()
{
    using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
    {
        connection.Open();

        Console.Clear();
        PrintAllRecords("Edit Record");

        int recordId = 0;
        do
        {
            Console.Write("\nEnter the ID of the record you want to edit: ");
            recordId = GetRecordId(recordId);
        } while (isValidInput == false);

        string selectQuery = @"
        SELECT h.Habit, h.Unit, hi.Date, hi.Quantity
        FROM HabitInstances hi
        JOIN Habits h ON hi.HabitId = h.Id
        WHERE hi.Id = @recordId";

        string? habitName = null; ;
        string? unit = null;
        DateTime? date = null;
        int? quantity = null;

        using (var command = connection.CreateCommand())
        {
            command.CommandText = selectQuery;
            command.Parameters.AddWithValue("@recordId", recordId);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    habitName = reader.GetString(0);
                    unit = reader.GetString(1);
                    date = reader.GetDateTime(2);
                    quantity = reader.GetInt32(3);
                }
            }
        }

        Console.WriteLine($"Selected record ID: {recordId}");
        Console.WriteLine($"Habit: {habitName}");
        Console.WriteLine($"Date: {date:yyyy-MM-dd}");
        Console.WriteLine($"Quantity: {quantity}");
        Console.WriteLine($"Unit: {unit}");

        Console.Write("\nEnter new date (yyyy-MM-dd) (leave blank to keep current): ");
        string? dateInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(dateInput) && DateTime.TryParse(dateInput, out DateTime newDate))
        {
            date = newDate;
        }

        Console.Write("\nEnter new quantity (leave blank to keep current): ");
        string? quantityInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(quantityInput) && int.TryParse(quantityInput, out int newQuantity))
        {
            quantity = newQuantity;
        }

        string updateQuery = @"
        UPDATE HabitInstances
        SET Date = @date, Quantity = @quantity
        WHERE Id = @recordId";

        using (var command = connection.CreateCommand())
        {
            command.CommandText = updateQuery;
            command.Parameters.AddWithValue("@date", date?.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@quantity", quantity);
            command.Parameters.AddWithValue("@recordId", recordId);

            int rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                PrintAllRecords("Edit Record");
                Console.WriteLine("\nRecord updated successfully!");
            }
            else
            {
                Console.WriteLine("\nFailed to update Record. Please try again.");
            }
        }
    }
}

void DeleteRecord()
{
    using (var connection = new SQLiteConnection($"Data Source={dbPath}"))
    {
        connection.Open();

        Console.Clear();
        PrintAllRecords("Delete Record");

        int recordId = 0;
        do
        {
            Console.Write("\nEnter the ID of the record you want to delete: ");
            recordId = GetRecordId(recordId);
        } while (isValidInput == false);

        string? confirmation;
        do
        {
            Console.Write($"Are you sure you want to delete the record with ID {recordId}? (y/n): ");
            confirmation = Console.ReadLine();
            if (confirmation?.ToLower() == "n")
            {
                Console.WriteLine("Deletion canceled.");
                return;
            }
            else if (confirmation?.ToLower() != "y")
            {
                Console.WriteLine("Invalid response.");
            }
        } while ((confirmation != "y") && (confirmation != "n"));

        string deleteQuery = @"
        DELETE FROM HabitInstances
        WHERE Id = @recordId";

        using (var command = connection.CreateCommand())
        {
            command.CommandText = deleteQuery;
            command.Parameters.AddWithValue("@recordId", recordId);

            int rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                PrintAllRecords("Delete Record");
                Console.WriteLine("\nRecord deleted successfully!");
            }
            else
            {
                Console.WriteLine("\nNo record found with that ID. Deletion failed.");
            }
        }
    }
}