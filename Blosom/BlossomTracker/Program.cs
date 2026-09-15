using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using Microsoft.Data.SqlClient;

namespace BlossomTracker
{
    // ════════════════════════════════════════════════════════════════════
    // CLASS 1: ConsoleHelper - UI utilities (colors, slow print)
    // ════════════════════════════════════════════════════════════════════
    static class ConsoleHelper
    {
        public static void PrintLine()
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine(new string('─', 70));
            Console.ResetColor();
        }

        public static void SlowPrint(string text, int delayMs = 15)
        {
            foreach (char c in text) { Console.Write(c); Thread.Sleep(delayMs); }
            Console.WriteLine();
        }

        public static void Clear() => Console.Clear();

        public static void WriteColor(string text, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        public static void PressEnter()
        {
            Console.Write("\nPress Enter to return to Main Menu...");
            Console.ReadLine();
        }

        public static void Pause(int ms = 1200) => Thread.Sleep(ms);
    }

    // ════════════════════════════════════════════════════════════════════
    // CLASS 2: DatabaseHelper - All SQL Server calls
    // ════════════════════════════════════════════════════════════════════
    static class DatabaseHelper
    {
        // 🔧 Connection string pointing to BlossomDB on PC "007"
        private const string ConnStr =
            "Server=007-\\SQLEXPRESS;Database=BlossomDB;" +
            "Trusted_Connection=True;TrustServerCertificate=True;";

        private static SqlConnection GetConn() => new SqlConnection(ConnStr);

        public static string HashPassword(string password)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            byte[] bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }

        // ── User Authentication ──────────────────────────────────────────
        public static bool RegisterUser(string username, string password)
        {
            try
            {
                using SqlConnection conn = GetConn(); conn.Open();
                using SqlCommand cmd = new SqlCommand("sp_RegisterUser", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@PasswordHash", HashPassword(password));
                object result = cmd.ExecuteScalar();
                return result != null && Convert.ToInt32(result) > 0;
            }
            catch (Exception ex) { Console.WriteLine("DB Error: " + ex.Message); return false; }
        }

        public static int LoginUser(string username, string password)
        {
            try
            {
                using SqlConnection conn = GetConn(); conn.Open();
                using SqlCommand cmd = new SqlCommand("sp_LoginUser", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@PasswordHash", HashPassword(password));
                object result = cmd.ExecuteScalar();
                return result != null ? Convert.ToInt32(result) : -1;
            }
            catch (Exception ex) { Console.WriteLine("DB Error: " + ex.Message); return -1; }
        }

        // ── Cycle Log ────────────────────────────────────────────────────
        public static void SaveCycleLog(int userId, DateTime lastPeriod, int cycleLen, int periodLen, string phase,
            DateTime nextPeriod, DateTime ovulation, DateTime fertileStart, DateTime fertileEnd)
        {
            try
            {
                using SqlConnection conn = GetConn(); conn.Open();
                using SqlCommand cmd = new SqlCommand("sp_SaveCycleLog", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@LastPeriodDate", lastPeriod);
                cmd.Parameters.AddWithValue("@CycleLengthDays", cycleLen);
                cmd.Parameters.AddWithValue("@PeriodLengthDays", periodLen);
                cmd.Parameters.AddWithValue("@CurrentPhase", phase);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { Console.WriteLine("DB Error: " + ex.Message); }
        }

        // ── Activity Log ─────────────────────────────────────────────────
        public static void LogActivity(int userId, string action, string detail = "")
        {
            try
            {
                using SqlConnection conn = GetConn(); conn.Open();
                using SqlCommand cmd = new SqlCommand("sp_LogActivity", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@Detail", detail ?? "");
                cmd.ExecuteNonQuery();
            }
            catch { }
        }

        // ── Goodie Basket Order ─────────────────────────────────────────
        public static void PlaceOrder(int userId, string flowType, string pad, string chocolates,
            string painkiller, string delivery, string payment, decimal fee, string name,
            string phone, string address, string city)
        {
            try
            {
                using SqlConnection conn = GetConn(); conn.Open();
                using SqlCommand cmd = new SqlCommand("sp_PlaceOrder", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@FlowType", flowType);
                cmd.Parameters.AddWithValue("@PadSelected", pad);
                cmd.Parameters.AddWithValue("@ChocolatesList", chocolates);
                cmd.Parameters.AddWithValue("@PainkillerName", painkiller);
                cmd.Parameters.AddWithValue("@FullName", name);
                cmd.Parameters.AddWithValue("@PhoneNumber", phone);
                cmd.Parameters.AddWithValue("@HomeAddress", address);
                cmd.Parameters.AddWithValue("@City", city);
                cmd.Parameters.AddWithValue("@DeliveryMethod", delivery);
                cmd.Parameters.AddWithValue("@PaymentMethod", payment);
                cmd.Parameters.AddWithValue("@DeliveryFeeRs", fee);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { Console.WriteLine("DB Error: " + ex.Message); }
        }

        // ── Symptom Tracker ─────────────────────────────────────────────
        public static void SaveSymptom(int userId, string symptomName, int severity)
        {
            try
            {
                using SqlConnection conn = GetConn(); conn.Open();
                using SqlCommand cmd = new SqlCommand("sp_SaveSymptom", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@SymptomName", symptomName);
                cmd.Parameters.AddWithValue("@Severity", severity);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { Console.WriteLine("DB Error: " + ex.Message); }
        }

        public static void ShowSymptomHistory(int userId)
        {
            try
            {
                using SqlConnection conn = GetConn(); conn.Open();
                using SqlCommand cmd = new SqlCommand("sp_GetSymptomHistory", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                using SqlDataReader r = cmd.ExecuteReader();
                Console.WriteLine($"\n  {"Date",-14} {"Symptom",-18} {"Severity",-10}");
                Console.WriteLine("  " + new string('─', 42));
                bool any = false;
                while (r.Read())
                {
                    any = true;
                    string date = Convert.ToDateTime(r["DateLogged"]).ToString("yyyy-MM-dd");
                    string symptom = r["SymptomName"].ToString() ?? "";
                    int sev = Convert.ToInt32(r["Severity"]);
                    string bar = new string('█', sev) + new string('░', 5 - sev);
                    Console.WriteLine($"  {date,-14} {symptom,-18} {sev}/5  {bar}");
                }
                if (!any) Console.WriteLine("  No symptom history found.");
            }
            catch (Exception ex) { Console.WriteLine("DB Error: " + ex.Message); }
        }

        // ── Water Intake Tracker ────────────────────────────────────────
        public static void SaveWaterIntake(int userId, int glasses, int goal)
        {
            try
            {
                using SqlConnection conn = GetConn(); conn.Open();
                using SqlCommand cmd = new SqlCommand("sp_SaveWaterIntake", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@GlassesConsumed", glasses);
                cmd.Parameters.AddWithValue("@Goal", goal);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { Console.WriteLine("DB Error: " + ex.Message); }
        }

        public static void ShowWaterHistory(int userId)
        {
            try
            {
                using SqlConnection conn = GetConn(); conn.Open();
                using SqlCommand cmd = new SqlCommand("sp_GetWaterHistory", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                using SqlDataReader r = cmd.ExecuteReader();
                Console.WriteLine($"\n  {"Date",-14} {"Glasses",-10} {"Goal",-8} {"Status",-12}");
                Console.WriteLine("  " + new string('─', 44));
                bool any = false;
                while (r.Read())
                {
                    any = true;
                    string date = Convert.ToDateTime(r["DateLogged"]).ToString("yyyy-MM-dd");
                    int glasses = Convert.ToInt32(r["GlassesConsumed"]);
                    int goal = Convert.ToInt32(r["Goal"]);
                    string status = glasses >= goal ? "✔ Goal met" : "✗ Below goal";
                    Console.WriteLine($"  {date,-14} {glasses,-10} {goal,-8} {status}");
                }
                if (!any) Console.WriteLine("  No water intake history found.");
            }
            catch (Exception ex) { Console.WriteLine("DB Error: " + ex.Message); }
        }

        // ── Reminders ───────────────────────────────────────────────────
        public static void SaveReminder(int userId, string reminderType, DateTime reminderDate)
        {
            try
            {
                using SqlConnection conn = GetConn(); conn.Open();
                using SqlCommand cmd = new SqlCommand("sp_SaveReminder", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ReminderType", reminderType);
                cmd.Parameters.AddWithValue("@ReminderDate", reminderDate);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { Console.WriteLine("DB Error: " + ex.Message); }
        }

        public static void ShowUpcomingReminders(int userId)
        {
            try
            {
                using SqlConnection conn = GetConn(); conn.Open();
                using SqlCommand cmd = new SqlCommand("sp_GetUpcomingReminders", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                using SqlDataReader r = cmd.ExecuteReader();
                bool any = false;
                while (r.Read())
                {
                    any = true;
                    string type = r["ReminderType"].ToString() ?? "";
                    string date = Convert.ToDateTime(r["ReminderDate"]).ToString("yyyy-MM-dd");
                    string status = r["Status"].ToString() ?? "";
                    int days = (Convert.ToDateTime(r["ReminderDate"]) - DateTime.Today).Days;
                    string when = days == 0 ? "TODAY" : days == 1 ? "Tomorrow" : $"In {days} days";
                    ConsoleColor col = days <= 1 ? ConsoleColor.Yellow : ConsoleColor.Cyan;
                    ConsoleHelper.WriteColor($"  🔔 {type,-22} {date}   {when,-12} [{status}]", col);
                }
                if (!any) Console.WriteLine("  No upcoming reminders.");
            }
            catch (Exception ex) { Console.WriteLine("DB Error: " + ex.Message); }
        }

        public static void MarkReminderDone(int userId, string reminderType)
        {
            try
            {
                using SqlConnection conn = GetConn(); conn.Open();
                using SqlCommand cmd = new SqlCommand("sp_MarkReminderDone", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@ReminderType", reminderType);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex) { Console.WriteLine("DB Error: " + ex.Message); }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // CLASS 3: AuthService - Login & Signup screens
    // ════════════════════════════════════════════════════════════════════
    class AuthService
    {
        public (int UserId, string Username) Login()
        {
            ConsoleHelper.Clear();
            ConsoleHelper.PrintLine();
            ConsoleHelper.SlowPrint("  LOGIN TO YOUR ACCOUNT");
            ConsoleHelper.PrintLine();
            while (true)
            {
                Console.Write("Username : ");
                string username = Console.ReadLine()?.Trim() ?? "";
                Console.Write("Password : ");
                string password = ReadMaskedPassword();
                int userId = DatabaseHelper.LoginUser(username, password);
                if (userId > 0)
                {
                    ConsoleHelper.WriteColor("Login successful!", ConsoleColor.Green);
                    DatabaseHelper.LogActivity(userId, "LOGIN");
                    ConsoleHelper.Pause();
                    return (userId, username);
                }
                ConsoleHelper.WriteColor("Invalid username or password! Try again.", ConsoleColor.Red);
            }
        }

        public void Signup()
        {
            ConsoleHelper.Clear();
            ConsoleHelper.PrintLine();
            ConsoleHelper.SlowPrint("  CREATE NEW ACCOUNT");
            ConsoleHelper.PrintLine();
            while (true)
            {
                Console.Write("Enter new username : ");
                string username = Console.ReadLine()?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(username))
                { ConsoleHelper.WriteColor("Username cannot be empty!", ConsoleColor.Red); continue; }
                Console.Write("Enter new password : ");
                string password = ReadMaskedPassword();
                if (DatabaseHelper.RegisterUser(username, password))
                { ConsoleHelper.WriteColor("Account created successfully!", ConsoleColor.Green); ConsoleHelper.Pause(); break; }
                ConsoleHelper.WriteColor("Username already exists! Try another.", ConsoleColor.Red);
            }
        }

        private string ReadMaskedPassword()
        {
            string password = "";
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(intercept: true);
                if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
                { password += key.KeyChar; Console.Write("*"); }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                { password = password[..^1]; Console.Write("\b \b"); }
            } while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            return password;
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // CLASS 4: CycleTracker - Period & ovulation calculator
    // ════════════════════════════════════════════════════════════════════
    class CycleTracker
    {
        public DateTime? LastPeriodDate { get; private set; }
        public int CycleLength { get; private set; }

        public void RunCycleTracker(int userId)
        {
            ConsoleHelper.Clear();
            ConsoleHelper.PrintLine();
            ConsoleHelper.SlowPrint("  PERIOD & OVULATION CALCULATOR");
            ConsoleHelper.PrintLine();

            DateTime lastPeriod = ReadDate("Enter last period start date (YYYY-MM-DD): ");
            int cycleLen = ReadInt("Enter average cycle length (days): ", 20, 45);
            int periodLen = ReadInt("Enter average period length (days): ", 1, 10);

            LastPeriodDate = lastPeriod;
            CycleLength = cycleLen;

            DateTime today = DateTime.Today;
            DateTime nextPeriod = lastPeriod.AddDays(cycleLen);
            DateTime ovulation = nextPeriod.AddDays(-14);
            DateTime fertileStart = ovulation.AddDays(-5);
            DateTime fertileEnd = ovulation.AddDays(1);
            int currentDay = (int)((today - lastPeriod).TotalDays % cycleLen) + 1;

            string phase;
            if (currentDay <= periodLen) phase = "Menstrual";
            else if (currentDay <= (ovulation - lastPeriod).Days) phase = "Follicular";
            else if (Math.Abs((today - ovulation).Days) <= 1) phase = "Ovulation";
            else phase = "Luteal";

            Console.WriteLine("\nRESULTS:");
            Console.WriteLine($"  Next period    : {nextPeriod:yyyy-MM-dd}");
            Console.WriteLine($"  Ovulation day  : {ovulation:yyyy-MM-dd}");
            Console.WriteLine($"  Fertile window : {fertileStart:yyyy-MM-dd} → {fertileEnd:yyyy-MM-dd}");
            Console.WriteLine($"  Today is day {currentDay} → {phase} phase");

            DatabaseHelper.SaveCycleLog(userId, lastPeriod, cycleLen, periodLen, phase, nextPeriod, ovulation, fertileStart, fertileEnd);
            DatabaseHelper.LogActivity(userId, "CYCLE_TRACKER", $"Phase={phase}");

            DatabaseHelper.SaveReminder(userId, "Next Period", nextPeriod);
            DatabaseHelper.SaveReminder(userId, "Ovulation Day", ovulation);
            ConsoleHelper.WriteColor("✔ Cycle data saved. Reminders set automatically.", ConsoleColor.Green);
            ConsoleHelper.PressEnter();
        }

        public void CheckReminder()
        {
            if (LastPeriodDate == null || CycleLength == 0) return;
            int daysLeft = (LastPeriodDate.Value.AddDays(CycleLength) - DateTime.Today).Days;
            if (daysLeft <= 2 && daysLeft >= 0)
                ConsoleHelper.WriteColor($"⚠ Reminder: Next period in {daysLeft} day(s)!", ConsoleColor.Yellow);
        }

        public void PregnancyTestCalculator(int userId)
        {
            ConsoleHelper.Clear();
            ConsoleHelper.PrintLine();
            ConsoleHelper.SlowPrint("  PREGNANCY TEST DATE CALCULATOR");
            ConsoleHelper.PrintLine();
            DateTime lastPeriod = ReadDate("Enter last period start date (YYYY-MM-DD): ");
            int cycleLen = ReadInt("Enter average cycle length (days): ", 20, 45);
            DateTime ovulation = lastPeriod.AddDays(cycleLen - 14);
            DateTime bestTestDate = ovulation.AddDays(14);
            Console.WriteLine($"\n  Best day to test : {bestTestDate:yyyy-MM-dd}");
            DatabaseHelper.LogActivity(userId, "PREGNANCY_TEST_CALC", $"BestDate={bestTestDate:yyyy-MM-dd}");
            ConsoleHelper.PressEnter();
        }

        public void PregnancyWeeksToMonths(int userId)
        {
            ConsoleHelper.Clear();
            ConsoleHelper.PrintLine();
            ConsoleHelper.SlowPrint("  PREGNANCY WEEKS → MONTHS CALCULATOR");
            ConsoleHelper.PrintLine();
            Console.Write("Enter number of pregnancy weeks: ");
            if (!double.TryParse(Console.ReadLine(), out double weeks) || weeks < 0)
            { ConsoleHelper.WriteColor("Invalid input.", ConsoleColor.Red); ConsoleHelper.PressEnter(); return; }
            double months = weeks / 4.345;
            string trimester = weeks <= 13 ? "First Trimester" : weeks <= 26 ? "Second Trimester" : "Third Trimester";
            Console.WriteLine($"\n  {weeks} weeks = {months:F2} months → {trimester}");
            DatabaseHelper.LogActivity(userId, "PREGNANCY_WEEKS", $"Weeks={weeks}");
            ConsoleHelper.PressEnter();
        }

        private DateTime ReadDate(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                if (DateTime.TryParseExact(Console.ReadLine()?.Trim(), "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime d)) return d;
                ConsoleHelper.WriteColor("Invalid format! Use YYYY-MM-DD.", ConsoleColor.Red);
            }
        }

        private int ReadInt(string prompt, int min, int max)
        {
            while (true)
            {
                Console.Write(prompt);
                if (int.TryParse(Console.ReadLine(), out int v) && v >= min && v <= max) return v;
                ConsoleHelper.WriteColor($"Enter a number between {min} and {max}.", ConsoleColor.Red);
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // CLASS 5: ExerciseAndMood - Phase-based workout & food guide
    // ════════════════════════════════════════════════════════════════════
    class ExerciseAndMood
    {
        private readonly Dictionary<string, (string Title, string Body, ConsoleColor Color)> _phases = new()
        {
            ["1"] = ("Menstrual Phase", "🩸 Rest and Recover\n• Gentle yoga, stretching, walking\n• Iron-rich foods: spinach, lentils, red meat\n• Stay warm with herbal tea", ConsoleColor.Red),
            ["2"] = ("Follicular Phase", "⚡ High Energy!\n• Cardio, strength training, HIIT\n• Eat protein: eggs, chicken, beans\n• Great time for new workouts", ConsoleColor.Green),
            ["3"] = ("Ovulation Phase", "💪 Peak Performance\n• Maintain strength training\n• Stay hydrated\n• Focus on fiber and antioxidants", ConsoleColor.Yellow),
            ["4"] = ("Luteal Phase", "😌 Slow Down\n• Moderate workouts, Pilates, swimming\n• Complex carbs and healthy fats\n• Practice mindfulness", ConsoleColor.Blue),
            ["5"] = ("Lifestyle Tips", "🌿 Daily Wellness\n• Drink 8+ glasses of water\n• Reduce caffeine and sugar\n• Sleep 7-9 hours\n• Track your symptoms", ConsoleColor.Magenta),
        };

        public void Run(int userId)
        {
            while (true)
            {
                ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
                ConsoleHelper.SlowPrint("  EXERCISE & MOOD SUPPORT"); ConsoleHelper.PrintLine();
                Console.WriteLine("1. Menstrual Phase\n2. Follicular Phase\n3. Ovulation Phase");
                Console.WriteLine("4. Luteal Phase\n5. Lifestyle Tips\n6. Back to Main Menu");
                ConsoleHelper.PrintLine(); Console.Write("Enter choice: ");
                string choice = Console.ReadLine()?.Trim() ?? "";
                if (choice == "6") break;
                if (_phases.TryGetValue(choice, out var phase))
                {
                    ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
                    ConsoleHelper.WriteColor(phase.Title, phase.Color); ConsoleHelper.PrintLine();
                    Console.WriteLine(phase.Body); ConsoleHelper.PrintLine();
                    DatabaseHelper.LogActivity(userId, "EXERCISE_MOOD", phase.Title);
                    Console.Write("Press Enter to continue..."); Console.ReadLine();
                }
                else { ConsoleHelper.WriteColor("Invalid choice!", ConsoleColor.Red); ConsoleHelper.Pause(800); }
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // CLASS 6: PcosGuide - PCOS information centre
    // ════════════════════════════════════════════════════════════════════
    class PcosGuide
    {
        public void Run(int userId)
        {
            while (true)
            {
                ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
                ConsoleHelper.SlowPrint("  PCOS COMPLETE GUIDE"); ConsoleHelper.PrintLine();
                Console.WriteLine("1. What is PCOS?\n2. Symptoms\n3. Diet Recommendations");
                Console.WriteLine("4. Exercise & Lifestyle\n5. Supplement Advice\n6. Back to Main Menu");
                Console.Write("\nEnter choice: ");
                string choice = Console.ReadLine()?.Trim() ?? "";
                switch (choice)
                {
                    case "1": Show("WHAT IS PCOS?", "PCOS is a hormonal imbalance affecting women of reproductive age. It causes irregular periods, acne, weight gain, and mood swings. Manageable with proper diet and exercise.", userId); break;
                    case "2": Show("COMMON SYMPTOMS", "• Irregular or missed periods\n• Weight gain (belly fat)\n• Acne or oily skin\n• Excess facial hair\n• Hair thinning\n• Mood swings and fatigue", userId); break;
                    case "3": Show("RECOMMENDED DIET", "• High-fiber vegetables (Spinach, Broccoli, Kale)\n• Lean proteins (Fish, Chicken)\n• Whole grains (Brown rice, Oats, Quinoa)\n• AVOID → Sugar, white bread, junk food", userId); break;
                    case "4": Show("EXERCISE PLAN", "• Follicular → Cardio + Strength training\n• Ovulation → Flexibility workouts\n• Luteal → Yoga, Pilates, Light walking\n• Menstrual → Stretching, Rest", userId); break;
                    case "5": Show("HELPFUL SUPPLEMENTS", "• Inositol (supports insulin balance)\n• Omega-3 (reduces inflammation)\n• Vitamin D (hormonal support)\n• Magnesium (anxiety + sleep)", userId); break;
                    case "6": return;
                    default: ConsoleHelper.WriteColor("Invalid input!", ConsoleColor.Red); ConsoleHelper.Pause(800); break;
                }
            }
        }

        private void Show(string title, string body, int userId)
        {
            ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
            ConsoleHelper.WriteColor(title, ConsoleColor.Cyan); ConsoleHelper.PrintLine();
            Console.WriteLine(body); ConsoleHelper.PrintLine();
            DatabaseHelper.LogActivity(userId, "PCOS_GUIDE", title);
            Console.Write("Press Enter to return..."); Console.ReadLine();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // CLASS 7: GoodieBasket - Period care ordering system
    // ════════════════════════════════════════════════════════════════════
    class GoodieBasket
    {
        private record BasketOption(string FlowType, List<string> Chocolates, List<string> Painkillers, List<string> Pads);

        private readonly Dictionary<string, BasketOption> _options = new()
        {
            ["1"] = new BasketOption("Heavy Flow",
                new List<string> { "Dark Chocolate", "Snickers", "KitKat" },
                new List<string> { "Ibuprofen", "Mefenamic Acid" },
                new List<string> { "Maxi Thick Pads", "Overnight Pads", "Extra Long Wings Pads" }),
            ["2"] = new BasketOption("Medium Flow",
                new List<string> { "Dairy Milk", "Galaxy", "Ferrero Rocher" },
                new List<string> { "Ibuprofen (low dose)", "Brufen" },
                new List<string> { "Regular Wings Pads", "Cotton Soft Pads" }),
            ["3"] = new BasketOption("Light Flow",
                new List<string> { "Milky Bar", "Cookies n Cream", "Kinder Bueno" },
                new List<string> { "No painkiller needed", "Panadol" },
                new List<string> { "Panty Liners", "Ultra Thin Pads" }),
        };

        public void Run(int userId)
        {
            ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
            ConsoleHelper.SlowPrint("  🌸 PERIOD GOODIE BASKET"); ConsoleHelper.PrintLine();
            Console.WriteLine("Select your flow level:\n1. Heavy Flow\n2. Medium Flow\n3. Light Flow\n4. Back\n");
            Console.Write("Enter choice: ");
            string flowChoice = Console.ReadLine()?.Trim() ?? "";
            if (flowChoice == "4") return;
            if (!_options.TryGetValue(flowChoice, out BasketOption? selected))
            { ConsoleHelper.WriteColor("Invalid choice!", ConsoleColor.Red); ConsoleHelper.Pause(); return; }

            ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
            ConsoleHelper.WriteColor($"  Your Basket for {selected.FlowType}", ConsoleColor.Cyan);
            ConsoleHelper.PrintLine();
            Console.WriteLine("Chocolates:"); foreach (string c in selected.Chocolates) Console.WriteLine("  • " + c);
            Console.WriteLine("\nPainkillers:"); foreach (string p in selected.Painkillers) Console.WriteLine("  • " + p);
            Console.WriteLine("\nPad Options:");
            for (int i = 0; i < selected.Pads.Count; i++) Console.WriteLine($"  {i + 1}. {selected.Pads[i]}");
            Console.Write("\nSelect pad number: ");
            string padInput = Console.ReadLine()?.Trim() ?? "";
            string chosenPad = (int.TryParse(padInput, out int idx) && idx >= 1 && idx <= selected.Pads.Count)
                ? selected.Pads[idx - 1] : selected.Pads[0];

            ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
            ConsoleHelper.WriteColor("  🧺 Basket Summary", ConsoleColor.Magenta); ConsoleHelper.PrintLine();
            Console.WriteLine($"  Flow : {selected.FlowType}\n  Pad  : {chosenPad}");
            Console.WriteLine("\n1. Confirm & Proceed\n2. Cancel\n");
            Console.Write("Enter choice: ");
            string confirm = Console.ReadLine()?.Trim() ?? "";
            if (confirm == "2") { ConsoleHelper.WriteColor("Cancelled.", ConsoleColor.Red); ConsoleHelper.Pause(); return; }

            ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
            ConsoleHelper.WriteColor("  🚚 DELIVERY DETAILS", ConsoleColor.Magenta); ConsoleHelper.PrintLine();
            Console.Write("Full Name    : "); string name = Console.ReadLine() ?? "";
            Console.Write("Phone Number : "); string phone = Console.ReadLine() ?? "";
            Console.Write("Home Address : "); string address = Console.ReadLine() ?? "";
            Console.Write("City         : "); string city = Console.ReadLine() ?? "";
            Console.WriteLine("\n1. Standard Delivery (Rs 150)\n2. Express Delivery (Rs 250)");
            Console.Write("Enter choice: ");
            string dChoice = Console.ReadLine()?.Trim() ?? "";
            string deliveryType = dChoice == "2" ? "Express Delivery" : "Standard Delivery";
            decimal fee = dChoice == "2" ? 250 : 150;
            Console.WriteLine("\n1. Cash on Delivery\n2. EasyPaisa\n3. JazzCash");
            Console.Write("Enter choice: ");
            string pChoice = Console.ReadLine()?.Trim() ?? "";
            string payment = pChoice == "2" ? "EasyPaisa" : pChoice == "3" ? "JazzCash" : "Cash on Delivery";

            DatabaseHelper.PlaceOrder(userId, selected.FlowType, chosenPad,
                string.Join(", ", selected.Chocolates), selected.Painkillers[0],
                deliveryType, payment, fee, name, phone, address, city);
            DatabaseHelper.LogActivity(userId, "GOODIE_BASKET_ORDER", $"Flow={selected.FlowType}");
            ConsoleHelper.WriteColor("\n✔ Order placed successfully! 🌸", ConsoleColor.Green);
            ConsoleHelper.PressEnter();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // CLASS 8: SymptomTracker - Log & view daily symptoms
    // ════════════════════════════════════════════════════════════════════
    class SymptomTracker
    {
        private readonly string[] _symptoms = { "Cramps", "Headache", "Acne", "Mood Swings", "Back Pain", "Bloating", "Fatigue", "Nausea", "Breast Tenderness" };

        public void Run(int userId)
        {
            while (true)
            {
                ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
                ConsoleHelper.SlowPrint("  🩺 SYMPTOM TRACKER"); ConsoleHelper.PrintLine();
                Console.WriteLine("  1. Log a new symptom today");
                Console.WriteLine("  2. View my symptom history");
                Console.WriteLine("  3. Back to Main Menu");
                ConsoleHelper.PrintLine(); Console.Write("  Enter choice: ");
                string choice = Console.ReadLine()?.Trim() ?? "";
                switch (choice)
                {
                    case "1": LogSymptom(userId); break;
                    case "2": DatabaseHelper.ShowSymptomHistory(userId); ConsoleHelper.PressEnter(); break;
                    case "3": return;
                    default: ConsoleHelper.WriteColor("Invalid choice!", ConsoleColor.Red); ConsoleHelper.Pause(800); break;
                }
            }
        }

        private void LogSymptom(int userId)
        {
            ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
            ConsoleHelper.WriteColor("  LOG SYMPTOM", ConsoleColor.Cyan); ConsoleHelper.PrintLine();
            for (int i = 0; i < _symptoms.Length; i++) Console.WriteLine($"  {i + 1,2}. {_symptoms[i]}");
            Console.Write("\n  Select symptom number: ");
            if (!int.TryParse(Console.ReadLine(), out int sIdx) || sIdx < 1 || sIdx > _symptoms.Length)
            { ConsoleHelper.WriteColor("Invalid selection!", ConsoleColor.Red); ConsoleHelper.Pause(800); return; }
            string symptomName = _symptoms[sIdx - 1];
            Console.WriteLine("\n  Severity: 1=Mild, 3=Moderate, 5=Severe");
            Console.Write("  Enter severity (1-5): ");
            if (!int.TryParse(Console.ReadLine(), out int severity) || severity < 1 || severity > 5)
            { ConsoleHelper.WriteColor("Please enter 1 to 5.", ConsoleColor.Red); ConsoleHelper.Pause(800); return; }
            DatabaseHelper.SaveSymptom(userId, symptomName, severity);
            DatabaseHelper.LogActivity(userId, "LOG_SYMPTOM", $"{symptomName} Severity={severity}");
            ConsoleHelper.WriteColor($"\n  ✔ Logged: {symptomName} (Severity: {severity}/5)", ConsoleColor.Green);
            ConsoleHelper.PressEnter();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // CLASS 9: WaterTracker - Daily water intake logger
    // ════════════════════════════════════════════════════════════════════
    class WaterTracker
    {
        public void Run(int userId)
        {
            while (true)
            {
                ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
                ConsoleHelper.SlowPrint("  💧 WATER INTAKE TRACKER"); ConsoleHelper.PrintLine();
                Console.WriteLine("  1. Log today's water intake");
                Console.WriteLine("  2. View water intake history");
                Console.WriteLine("  3. Back to Main Menu");
                ConsoleHelper.PrintLine(); Console.Write("  Enter choice: ");
                string choice = Console.ReadLine()?.Trim() ?? "";
                switch (choice)
                {
                    case "1": LogIntake(userId); break;
                    case "2": DatabaseHelper.ShowWaterHistory(userId); ConsoleHelper.PressEnter(); break;
                    case "3": return;
                    default: ConsoleHelper.WriteColor("Invalid choice!", ConsoleColor.Red); ConsoleHelper.Pause(800); break;
                }
            }
        }

        private void LogIntake(int userId)
        {
            ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
            ConsoleHelper.WriteColor("  LOG WATER INTAKE", ConsoleColor.Cyan); ConsoleHelper.PrintLine();
            Console.WriteLine("  Recommended daily goal: 8 glasses (2 litres)");
            Console.Write("\n  How many glasses did you drink today? ");
            if (!int.TryParse(Console.ReadLine(), out int glasses) || glasses < 0)
            { ConsoleHelper.WriteColor("Invalid number!", ConsoleColor.Red); ConsoleHelper.Pause(800); return; }
            Console.Write("  What is your daily goal (glasses)? [default 8]: ");
            string goalInput = Console.ReadLine()?.Trim() ?? "";
            int goal = string.IsNullOrEmpty(goalInput) ? 8 : int.TryParse(goalInput, out int g) ? g : 8;
            DatabaseHelper.SaveWaterIntake(userId, glasses, goal);
            DatabaseHelper.LogActivity(userId, "LOG_WATER", $"Glasses={glasses} Goal={goal}");
            if (glasses >= goal)
                ConsoleHelper.WriteColor($"\n  ✔ Goal reached! ({glasses}/{goal} glasses) 💧", ConsoleColor.Green);
            else
                ConsoleHelper.WriteColor($"\n  {glasses}/{goal} glasses — drink {goal - glasses} more!", ConsoleColor.Yellow);
            if (glasses < goal) DatabaseHelper.SaveReminder(userId, "Drink Water", DateTime.Today.AddDays(1));
            ConsoleHelper.PressEnter();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // CLASS 10: ReminderManager - Set & view reminders
    // ════════════════════════════════════════════════════════════════════
    class ReminderManager
    {
        private readonly Dictionary<string, string> _types = new()
        {
            ["1"] = "Next Period",
            ["2"] = "Ovulation Day",
            ["3"] = "Take Medicine",
            ["4"] = "Drink Water",
            ["5"] = "Doctor Appointment",
        };

        public void Run(int userId)
        {
            while (true)
            {
                ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
                ConsoleHelper.SlowPrint("  🔔 REMINDERS & NOTIFICATIONS"); ConsoleHelper.PrintLine();
                Console.WriteLine("  1. View upcoming reminders");
                Console.WriteLine("  2. Set a new reminder");
                Console.WriteLine("  3. Mark a reminder as done");
                Console.WriteLine("  4. Back to Main Menu");
                ConsoleHelper.PrintLine(); Console.Write("  Enter choice: ");
                string choice = Console.ReadLine()?.Trim() ?? "";
                switch (choice)
                {
                    case "1": DatabaseHelper.ShowUpcomingReminders(userId); ConsoleHelper.PressEnter(); break;
                    case "2": SetReminder(userId); break;
                    case "3": MarkDone(userId); break;
                    case "4": return;
                    default: ConsoleHelper.WriteColor("Invalid choice!", ConsoleColor.Red); ConsoleHelper.Pause(800); break;
                }
            }
        }

        private void SetReminder(int userId)
        {
            ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
            ConsoleHelper.WriteColor("  SET NEW REMINDER", ConsoleColor.Cyan); ConsoleHelper.PrintLine();
            foreach (var kv in _types) Console.WriteLine($"  {kv.Key}. {kv.Value}");
            Console.Write("\n  Select reminder type: ");
            string tChoice = Console.ReadLine()?.Trim() ?? "";
            if (!_types.TryGetValue(tChoice, out string? reminderType))
            { ConsoleHelper.WriteColor("Invalid selection!", ConsoleColor.Red); ConsoleHelper.Pause(800); return; }
            Console.Write("  Enter reminder date (YYYY-MM-DD): ");
            if (!DateTime.TryParseExact(Console.ReadLine()?.Trim(), "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime rDate))
            { ConsoleHelper.WriteColor("Invalid date! Use YYYY-MM-DD.", ConsoleColor.Red); ConsoleHelper.Pause(800); return; }
            DatabaseHelper.SaveReminder(userId, reminderType, rDate);
            DatabaseHelper.LogActivity(userId, "SET_REMINDER", $"{reminderType} on {rDate:yyyy-MM-dd}");
            ConsoleHelper.WriteColor($"\n  ✔ Reminder set: {reminderType} on {rDate:yyyy-MM-dd}", ConsoleColor.Green);
            ConsoleHelper.PressEnter();
        }

        private void MarkDone(int userId)
        {
            ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
            ConsoleHelper.WriteColor("  MARK REMINDER AS DONE", ConsoleColor.Cyan); ConsoleHelper.PrintLine();
            foreach (var kv in _types) Console.WriteLine($"  {kv.Key}. {kv.Value}");
            Console.Write("\n  Select reminder to mark done: ");
            string tChoice = Console.ReadLine()?.Trim() ?? "";
            if (!_types.TryGetValue(tChoice, out string? reminderType))
            { ConsoleHelper.WriteColor("Invalid selection!", ConsoleColor.Red); ConsoleHelper.Pause(800); return; }
            DatabaseHelper.MarkReminderDone(userId, reminderType);
            DatabaseHelper.LogActivity(userId, "MARK_REMINDER_DONE", reminderType);
            ConsoleHelper.WriteColor($"  ✔ {reminderType} marked as done.", ConsoleColor.Green);
            ConsoleHelper.PressEnter();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // CLASS 11: App - Startup screen & main menu
    // ════════════════════════════════════════════════════════════════════
    class App
    {
        private readonly AuthService _auth = new();
        private readonly CycleTracker _cycle = new();
        private readonly ExerciseAndMood _exMood = new();
        private readonly PcosGuide _pcos = new();
        private readonly GoodieBasket _basket = new();
        private readonly SymptomTracker _symptoms = new();
        private readonly WaterTracker _water = new();
        private readonly ReminderManager _reminders = new();

        public void Start()
        {
            while (true)
            {
                ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
                ConsoleHelper.SlowPrint("  🌙 BLOSSOM MENSTRUAL HEALTH TRACKER");
                ConsoleHelper.PrintLine();
                Console.WriteLine("  1. Login\n  2. Signup\n  3. Exit\n");
                Console.Write("Choose an option: ");
                string option = Console.ReadLine()?.Trim() ?? "";
                switch (option)
                {
                    case "1": var (userId, username) = _auth.Login(); MainMenu(userId, username); break;
                    case "2": _auth.Signup(); break;
                    case "3": ConsoleHelper.WriteColor("Thank you for visiting Blossom! 🌸", ConsoleColor.Magenta); return;
                    default: ConsoleHelper.WriteColor("Invalid option!", ConsoleColor.Red); ConsoleHelper.Pause(800); break;
                }
            }
        }

        private void MainMenu(int userId, string username)
        {
            while (true)
            {
                ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
                ConsoleHelper.WriteColor($"  Welcome, {username}!", ConsoleColor.Cyan);
                ConsoleHelper.PrintLine();
                DatabaseHelper.ShowUpcomingReminders(userId);
                Console.WriteLine("\n  1.  Home Page");
                Console.WriteLine("  2.  Cycle Tracker");
                Console.WriteLine("  3.  Pregnancy Test Calculator");
                Console.WriteLine("  4.  Pregnancy Weeks to Months");
                Console.WriteLine("  5.  Exercise & Mood");
                Console.WriteLine("  6.  PCOS Guide");
                Console.WriteLine("  7.  Goodie Basket");
                Console.WriteLine("  8.  Symptom Tracker");
                Console.WriteLine("  9.  Water Intake Tracker");
                Console.WriteLine("  10. Reminders");
                Console.WriteLine("  11. About Us");
                Console.WriteLine("  12. Logout");
                ConsoleHelper.PrintLine();
                Console.Write("  Enter your choice: ");
                string choice = Console.ReadLine()?.Trim() ?? "";

                switch (choice)
                {
                    case "1": ShowHomePage(userId); break;
                    case "2": _cycle.RunCycleTracker(userId); break;
                    case "3": _cycle.PregnancyTestCalculator(userId); break;
                    case "4": _cycle.PregnancyWeeksToMonths(userId); break;
                    case "5": _exMood.Run(userId); break;
                    case "6": _pcos.Run(userId); break;
                    case "7": _basket.Run(userId); break;
                    case "8": _symptoms.Run(userId); break;
                    case "9": _water.Run(userId); break;
                    case "10": _reminders.Run(userId); break;
                    case "11": ShowAboutUs(userId); break;
                    case "12": ConsoleHelper.WriteColor("Logged out!", ConsoleColor.Green); DatabaseHelper.LogActivity(userId, "LOGOUT"); ConsoleHelper.Pause(); return;
                    default: ConsoleHelper.WriteColor("Invalid choice!", ConsoleColor.Red); ConsoleHelper.Pause(800); break;
                }
            }
        }

        private void ShowHomePage(int userId)
        {
            ConsoleHelper.Clear();
            ConsoleHelper.SlowPrint("  🌙 BLOSSOM HEALTH TRACKER", 8);
            Console.WriteLine("  Track your health • Understand your body • Stay empowered\n");
            string[] features = { "Period Tracking", "Symptom Tracker", "Water Tracker", "Reminders", "Exercise & Mood", "PCOS Support" };
            foreach (string f in features) Console.WriteLine($"  • {f}");
            ConsoleHelper.PrintLine();
            DatabaseHelper.LogActivity(userId, "VIEW_HOME");
            ConsoleHelper.PressEnter();
        }

        private void ShowAboutUs(int userId)
        {
            ConsoleHelper.Clear(); ConsoleHelper.PrintLine();
            ConsoleHelper.SlowPrint("  ABOUT BLOSSOM HEALTH TRACKER", 8); ConsoleHelper.PrintLine();
            Console.WriteLine("\n  OUR MISSION: Help women understand their bodies through science-backed predictions.\n");
            Console.WriteLine("  FEATURES: Cycle tracking, Symptom logging, Water intake, Reminders, PCOS guide, Goodie basket\n");
            Console.WriteLine("  DEVELOPERS: Khadija Sohail & Bisma Burhan\n");
            ConsoleHelper.WriteColor("  Stay healthy and keep blooming 🌸", ConsoleColor.Magenta);
            ConsoleHelper.PrintLine();
            DatabaseHelper.LogActivity(userId, "VIEW_ABOUT");
            ConsoleHelper.PressEnter();
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // ENTRY POINT
    // ════════════════════════════════════════════════════════════════════
    class Program
    {
        static void Main(string[] args)
        {
            App app = new App();
            app.Start();
        }
    }
}