using System;
using System.Data.SQLite;
using System.IO;
using System.Globalization;
using System.Threading;
using System.Configuration;
using System.Text;

string? userInput;
bool isValidInput = false;
int menuChoice;
int[] menuNumbers = new int[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

string dbPath = @"C:\Users\Andrew\Documents\coding stuff\C#\The C# Academy\HabitLogger2\bin\data\mydatabase.db";

var connection = new SQLiteConnection($"Data Source={dbPath}");

Console.Title ="Habit Logger v2";

using (connection)
{
    connection.Open();

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
                FOREIGN KEY (HabitId) REFERENCES Habits(Id)
            );";

        command.ExecuteNonQuery();

        command.CommandText = "SELECT COUNT(*) FROM Habits";
        int count = Convert.ToInt32(command.ExecuteScalar());

        if (count == 0)
        {
            SeedData(connection);
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
                    PrintAllRecords(connection, "View All Records");
                    ReturnToMainMenu();
                    break;
                case 2:
                    AddRecord(connection);
                    ReturnToMainMenu();
                    break;
                case 3:
                    EditRecord(connection);
                    ReturnToMainMenu();
                    break;
                case 4:
                    DeleteRecord(connection);
                    ReturnToMainMenu();
                    break;
                case 5:
                    PrintHabits(connection, "View All Habits");
                    ReturnToMainMenu();
                    break;
                case 6:
                    AddHabit(connection);
                    ReturnToMainMenu();
                    break;
                case 7:
                    EditHabit(connection);
                    ReturnToMainMenu();
                    break;
                case 8:
                    DeleteHabit(connection);
                    ReturnToMainMenu();
                    break;
                case 9:
                    PrintReport(connection);
                    ReturnToMainMenu();
                    break;
                default:
                    Console.WriteLine("Invalid choice.");
                    break;
            }
        } while (menuChoice != 0);
    }
}
static void SeedData(SQLiteConnection connection)
{
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

static void PrintReport(SQLiteConnection connection)
    {
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

            var longestStreaks = CalculateStreak(connection);
            Console.WriteLine($"\nLongest Streaks:");
            foreach (var streak in longestStreaks)
            {
                Console.WriteLine($"{streak.Key} - {streak.Value} days in a row");
            }
        }
    }

static Dictionary<string, int> CalculateStreak(SQLiteConnection connection)
    {
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

static void AddHabit(SQLiteConnection connection)
{
    Console.Clear();
    PrintHabits(connection, "Add Habit");
 
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

static void PrintHabits(SQLiteConnection connection, string heading)
{
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

void EditHabit(SQLiteConnection connection)
{
    Console.Clear();
    PrintHabits(connection, "Edit Habit");

    int habitId = 0;
    do
    {
        Console.Write("\nEnter the ID of the habit you want to edit: ");
        habitId = GetHabitId(connection, habitId);
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
            PrintHabits(connection, "Edit Habit");
            Console.WriteLine("\nHabit updated successfully!");
        }
    }
}

void DeleteHabit(SQLiteConnection connection)

{
    Console.Clear();
    PrintHabits(connection, "Delete Habit");

    int habitId = 0;
    do
    {
        GetHabitId(connection, habitId);
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
            PrintHabits(connection, "Delete Habit");
            Console.WriteLine("\nHabit deleted successfully!");
        }
        else
        {
            Console.WriteLine("\nNo habit was deleted. Please try again.");
        }
    }
}

void PrintAllRecords(SQLiteConnection connection, string heading)
{
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

void AddRecord(SQLiteConnection connection)
{
    Console.Clear();
    PrintHabits(connection, "Add Record");

    int habitId = 0;
    do
    {
        Console.Write("\nEnter the ID of the habit for which you want to add a record: ");
        habitId = GetHabitId(connection, habitId);
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
            PrintAllRecords(connection, "Add Record");
            Console.WriteLine("\nRecord added successfully!");
        }
        else
        {
            Console.WriteLine("\nFailed to add record. Please try again.");
        }
    }
}

int GetHabitId(SQLiteConnection connection, int habitId)
{
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

int GetRecordId(SQLiteConnection connection, int recordId)
{
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

void EditRecord(SQLiteConnection connection)
{
    Console.Clear();
    PrintAllRecords(connection, "Edit Record");

    int recordId = 0;
    do
    {
        Console.Write("\nEnter the ID of the record you want to edit: ");
        recordId = GetRecordId(connection, recordId);
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
            PrintAllRecords(connection, "Edit Record");
            Console.WriteLine("\nRecord updated successfully!");
        }
        else
        {
            Console.WriteLine("\nFailed to update Record. Please try again.");
        }
    }
}

void DeleteRecord(SQLiteConnection connection)
{
    Console.Clear();
    PrintAllRecords(connection, "Delete Record");

    int recordId = 0;
    do
    {
        Console.Write("\nEnter the ID of the record you want to delete: ");
        recordId = GetRecordId(connection, recordId);
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
            PrintAllRecords(connection, "Delete Record");
            Console.WriteLine("\nRecord deleted successfully!");
        }
        else
        {
            Console.WriteLine("\nNo record found with that ID. Deletion failed.");
        }
    }
}