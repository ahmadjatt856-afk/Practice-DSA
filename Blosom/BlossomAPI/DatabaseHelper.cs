using System.Data;
using System.Security.Cryptography;
using System.Text;
using BlossomAPI.Models;
using Microsoft.Data.SqlClient;

namespace BlossomAPI
{
    public class DatabaseHelper
    {
        private readonly string _connStr;

        public DatabaseHelper(string connStr)
        {
            _connStr = connStr;
        }

        private SqlConnection GetConn() => new SqlConnection(_connStr);

        public static string HashPassword(string password)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes);
        }

        // ── Auth ─────────────────────────────────────────────────────────

        public bool RegisterUser(string username, string password)
        {
            using SqlConnection conn = GetConn(); conn.Open();
            using SqlCommand cmd = new("sp_RegisterUser", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@PasswordHash", HashPassword(password));
            object? result = cmd.ExecuteScalar();
            return result != null && Convert.ToInt32(result) > 0;
        }

        public int LoginUser(string username, string password)
        {
            using SqlConnection conn = GetConn(); conn.Open();
            using SqlCommand cmd = new("sp_LoginUser", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@Username", username);
            cmd.Parameters.AddWithValue("@PasswordHash", HashPassword(password));
            object? result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : -1;
        }

        // ── Cycle Log ────────────────────────────────────────────────────

        public void SaveCycleLog(CycleLogRequest req)
        {
            using SqlConnection conn = GetConn(); conn.Open();
            using SqlCommand cmd = new("sp_SaveCycleLog", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", req.UserId);
            cmd.Parameters.AddWithValue("@LastPeriodDate", DateOnly.Parse(req.LastPeriodDate));
            cmd.Parameters.AddWithValue("@CycleLengthDays", req.CycleLengthDays);
            cmd.Parameters.AddWithValue("@PeriodLengthDays", req.PeriodLengthDays);
            cmd.Parameters.AddWithValue("@CurrentPhase", req.CurrentPhase);
            cmd.ExecuteNonQuery();
        }

        // ── Activity Log ─────────────────────────────────────────────────

        public void LogActivity(int userId, string action, string detail = "")
        {
            try
            {
                using SqlConnection conn = GetConn(); conn.Open();
                using SqlCommand cmd = new("sp_LogActivity", conn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@Action", action);
                cmd.Parameters.AddWithValue("@Detail", detail);
                cmd.ExecuteNonQuery();
            }
            catch { /* non-blocking */ }
        }

        // ── Symptom ──────────────────────────────────────────────────────

        public void SaveSymptom(SymptomRequest req)
        {
            using SqlConnection conn = GetConn(); conn.Open();
            using SqlCommand cmd = new("sp_SaveSymptom", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", req.UserId);
            cmd.Parameters.AddWithValue("@SymptomName", req.SymptomName);
            cmd.Parameters.AddWithValue("@Severity", req.Severity);
            cmd.ExecuteNonQuery();
        }

        public List<SymptomRecord> GetSymptomHistory(int userId)
        {
            var list = new List<SymptomRecord>();
            using SqlConnection conn = GetConn(); conn.Open();
            using SqlCommand cmd = new("sp_GetSymptomHistory", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);
            using SqlDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new SymptomRecord
                {
                    SymptomName = r["SymptomName"].ToString() ?? "",
                    Severity = Convert.ToInt32(r["Severity"]),
                    DateLogged = Convert.ToDateTime(r["DateLogged"]).ToString("yyyy-MM-dd")
                });
            }
            return list;
        }

        // ── Water Intake ─────────────────────────────────────────────────

        public void SaveWaterIntake(WaterIntakeRequest req)
        {
            using SqlConnection conn = GetConn(); conn.Open();
            using SqlCommand cmd = new("sp_SaveWaterIntake", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", req.UserId);
            cmd.Parameters.AddWithValue("@GlassesConsumed", req.GlassesConsumed);
            cmd.Parameters.AddWithValue("@Goal", req.Goal);
            cmd.ExecuteNonQuery();
        }

        public List<WaterRecord> GetWaterHistory(int userId)
        {
            var list = new List<WaterRecord>();
            using SqlConnection conn = GetConn(); conn.Open();
            using SqlCommand cmd = new("sp_GetWaterHistory", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);
            using SqlDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new WaterRecord
                {
                    GlassesConsumed = Convert.ToInt32(r["GlassesConsumed"]),
                    Goal = Convert.ToInt32(r["Goal"]),
                    DateLogged = Convert.ToDateTime(r["DateLogged"]).ToString("yyyy-MM-dd")
                });
            }
            return list;
        }

        // ── Reminders ────────────────────────────────────────────────────

        public void SaveReminder(ReminderRequest req)
        {
            using SqlConnection conn = GetConn(); conn.Open();
            using SqlCommand cmd = new("sp_SaveReminder", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", req.UserId);
            cmd.Parameters.AddWithValue("@ReminderType", req.ReminderType);
            cmd.Parameters.AddWithValue("@ReminderDate", DateOnly.Parse(req.ReminderDate));
            cmd.ExecuteNonQuery();
        }

        public List<ReminderRecord> GetUpcomingReminders(int userId)
        {
            var list = new List<ReminderRecord>();
            using SqlConnection conn = GetConn(); conn.Open();
            using SqlCommand cmd = new("sp_GetUpcomingReminders", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", userId);
            using SqlDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new ReminderRecord
                {
                    ReminderId = Convert.ToInt32(r["ReminderId"]),
                    ReminderType = r["ReminderType"].ToString() ?? "",
                    ReminderDate = Convert.ToDateTime(r["ReminderDate"]).ToString("yyyy-MM-dd"),
                    Status = r["Status"].ToString() ?? ""
                });
            }
            return list;
        }

        public void MarkReminderDone(MarkDoneRequest req)
        {
            using SqlConnection conn = GetConn(); conn.Open();
            using SqlCommand cmd = new("sp_MarkReminderDone", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", req.UserId);
            cmd.Parameters.AddWithValue("@ReminderType", req.ReminderType);
            cmd.ExecuteNonQuery();
        }

        // ── Goodie Basket ─────────────────────────────────────────────────

        public void PlaceOrder(OrderRequest req)
        {
            using SqlConnection conn = GetConn(); conn.Open();
            using SqlCommand cmd = new("sp_PlaceOrder", conn);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.AddWithValue("@UserId", req.UserId);
            cmd.Parameters.AddWithValue("@FlowType", req.FlowType);
            cmd.Parameters.AddWithValue("@PadSelected", req.PadSelected);
            cmd.Parameters.AddWithValue("@ChocolatesList", req.ChocolatesList);
            cmd.Parameters.AddWithValue("@PainkillerName", req.PainkillerName);
            cmd.Parameters.AddWithValue("@FullName", req.FullName);
            cmd.Parameters.AddWithValue("@PhoneNumber", req.PhoneNumber);
            cmd.Parameters.AddWithValue("@HomeAddress", req.HomeAddress);
            cmd.Parameters.AddWithValue("@City", req.City);
            cmd.Parameters.AddWithValue("@DeliveryMethod", req.DeliveryMethod);
            cmd.Parameters.AddWithValue("@PaymentMethod", req.PaymentMethod);
            cmd.Parameters.AddWithValue("@DeliveryFeeRs", req.DeliveryFeeRs);
            cmd.ExecuteNonQuery();
        }
    }
}