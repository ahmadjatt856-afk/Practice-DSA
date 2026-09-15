namespace BlossomAPI.Models
{
    // ── Auth ─────────────────────────────────────────────────────────────
    public class RegisterRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class LoginRequest
    {
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class LoginResponse
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
    }

    // ── Cycle Log ────────────────────────────────────────────────────────
    public class CycleLogRequest
    {
        public int UserId { get; set; }
        public string LastPeriodDate { get; set; } = "";  // "YYYY-MM-DD"
        public int CycleLengthDays { get; set; }
        public int PeriodLengthDays { get; set; }
        public string CurrentPhase { get; set; } = "";
    }

    // ── Symptom ──────────────────────────────────────────────────────────
    public class SymptomRequest
    {
        public int UserId { get; set; }
        public string SymptomName { get; set; } = "";
        public int Severity { get; set; }
    }

    public class SymptomRecord
    {
        public string SymptomName { get; set; } = "";
        public int Severity { get; set; }
        public string DateLogged { get; set; } = "";
    }

    // ── Water Intake ─────────────────────────────────────────────────────
    public class WaterIntakeRequest
    {
        public int UserId { get; set; }
        public int GlassesConsumed { get; set; }
        public int Goal { get; set; }
    }

    public class WaterRecord
    {
        public int GlassesConsumed { get; set; }
        public int Goal { get; set; }
        public string DateLogged { get; set; } = "";
    }

    // ── Reminder ─────────────────────────────────────────────────────────
    public class ReminderRequest
    {
        public int UserId { get; set; }
        public string ReminderType { get; set; } = "";
        public string ReminderDate { get; set; } = "";  // "YYYY-MM-DD"
    }

    public class MarkDoneRequest
    {
        public int UserId { get; set; }
        public string ReminderType { get; set; } = "";
    }

    public class ReminderRecord
    {
        public int ReminderId { get; set; }
        public string ReminderType { get; set; } = "";
        public string ReminderDate { get; set; } = "";
        public string Status { get; set; } = "";
    }

    // ── Goodie Basket Order ───────────────────────────────────────────────
    public class OrderRequest
    {
        public int UserId { get; set; }
        public string FlowType { get; set; } = "";
        public string PadSelected { get; set; } = "";
        public string ChocolatesList { get; set; } = "";
        public string PainkillerName { get; set; } = "";
        public string FullName { get; set; } = "";
        public string PhoneNumber { get; set; } = "";
        public string HomeAddress { get; set; } = "";
        public string City { get; set; } = "";
        public string DeliveryMethod { get; set; } = "";
        public string PaymentMethod { get; set; } = "";
        public int DeliveryFeeRs { get; set; }
    }

    // ── Activity Log ─────────────────────────────────────────────────────
    public class ActivityRequest
    {
        public int UserId { get; set; }
        public string Action { get; set; } = "";
        public string Detail { get; set; } = "";
    }

    // ── Generic Response ─────────────────────────────────────────────────
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
    }
}