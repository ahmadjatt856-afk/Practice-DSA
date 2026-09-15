using BlossomAPI;
using BlossomAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Connection String ─────────────────────────────────────────────────────────
// Change "007-\\SQLEXPRESS" to your actual server name if needed
var connStr = builder.Configuration.GetConnectionString("BlossomDB")
    ?? "Server=007-\\SQLEXPRESS;Database=BlossomDB;Trusted_Connection=True;TrustServerCertificate=True;";

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton(new DatabaseHelper(connStr));

// Allow your HTML frontend to call this API
// In production replace "*" with your actual domain e.g. "http://127.0.0.1:5500"
builder.Services.AddCors(options =>
{
    options.AddPolicy("BlossomFrontend", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();
app.UseCors("BlossomFrontend");

// ════════════════════════════════════════════════════════════════════════════
// HEALTH CHECK
// GET /api/health  →  { "status": "ok" }
// ════════════════════════════════════════════════════════════════════════════
app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

// ════════════════════════════════════════════════════════════════════════════
// AUTH ENDPOINTS
// ════════════════════════════════════════════════════════════════════════════

// POST /api/auth/register
// Body: { "username": "...", "password": "..." }
// Returns: { "success": true } or { "success": false, "message": "Username already taken" }
app.MapPost("/api/auth/register", (RegisterRequest req, DatabaseHelper db) =>
{
    if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new ApiResponse { Success = false, Message = "Username and password required." });

    if (req.Password.Length < 4)
        return Results.BadRequest(new ApiResponse { Success = false, Message = "Password must be at least 4 characters." });

    bool ok = db.RegisterUser(req.Username.Trim(), req.Password);
    if (!ok)
        return Results.Conflict(new ApiResponse { Success = false, Message = "Username already taken." });

    return Results.Ok(new ApiResponse { Success = true, Message = "Account created successfully!" });
});

// POST /api/auth/login
// Body: { "username": "...", "password": "..." }
// Returns: { "userId": 3, "username": "sara" }  or 401
app.MapPost("/api/auth/login", (LoginRequest req, DatabaseHelper db) =>
{
    if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password))
        return Results.BadRequest(new ApiResponse { Success = false, Message = "Username and password required." });

    int userId = db.LoginUser(req.Username.Trim(), req.Password);
    if (userId <= 0)
        return Results.Unauthorized();

    db.LogActivity(userId, "LOGIN");
    return Results.Ok(new LoginResponse { UserId = userId, Username = req.Username.Trim() });
});

// ════════════════════════════════════════════════════════════════════════════
// CYCLE LOG
// ════════════════════════════════════════════════════════════════════════════

// POST /api/cycle
// Body: { "userId":3, "lastPeriodDate":"2024-05-01", "cycleLengthDays":28, "periodLengthDays":5, "currentPhase":"Menstrual" }
app.MapPost("/api/cycle", (CycleLogRequest req, DatabaseHelper db) =>
{
    db.SaveCycleLog(req);
    db.LogActivity(req.UserId, "CYCLE_LOG", $"Phase:{req.CurrentPhase}");
    return Results.Ok(new ApiResponse { Success = true, Message = "Cycle log saved." });
});

// ════════════════════════════════════════════════════════════════════════════
// SYMPTOMS
// ════════════════════════════════════════════════════════════════════════════

// POST /api/symptoms
// Body: { "userId":3, "symptomName":"Cramps", "severity":4 }
app.MapPost("/api/symptoms", (SymptomRequest req, DatabaseHelper db) =>
{
    db.SaveSymptom(req);
    db.LogActivity(req.UserId, "LOG_SYMPTOM", $"{req.SymptomName}:{req.Severity}");
    return Results.Ok(new ApiResponse { Success = true, Message = "Symptom saved." });
});

// GET /api/symptoms/{userId}
// Returns: [ { "symptomName":"Cramps", "severity":4, "dateLogged":"2024-05-10" }, ... ]
app.MapGet("/api/symptoms/{userId:int}", (int userId, DatabaseHelper db) =>
{
    var history = db.GetSymptomHistory(userId);
    return Results.Ok(history);
});

// ════════════════════════════════════════════════════════════════════════════
// WATER INTAKE
// ════════════════════════════════════════════════════════════════════════════

// POST /api/water
// Body: { "userId":3, "glassesConsumed":6, "goal":8 }
app.MapPost("/api/water", (WaterIntakeRequest req, DatabaseHelper db) =>
{
    db.SaveWaterIntake(req);
    db.LogActivity(req.UserId, "WATER_INTAKE", $"{req.GlassesConsumed}/{req.Goal}");
    return Results.Ok(new ApiResponse { Success = true, Message = "Water intake saved." });
});

// GET /api/water/{userId}
// Returns: [ { "glassesConsumed":6, "goal":8, "dateLogged":"2024-05-10" }, ... ]
app.MapGet("/api/water/{userId:int}", (int userId, DatabaseHelper db) =>
{
    var history = db.GetWaterHistory(userId);
    return Results.Ok(history);
});

// ════════════════════════════════════════════════════════════════════════════
// REMINDERS
// ════════════════════════════════════════════════════════════════════════════

// POST /api/reminders
// Body: { "userId":3, "reminderType":"Period", "reminderDate":"2024-06-01" }
app.MapPost("/api/reminders", (ReminderRequest req, DatabaseHelper db) =>
{
    db.SaveReminder(req);
    db.LogActivity(req.UserId, "SET_REMINDER", $"{req.ReminderType} on {req.ReminderDate}");
    return Results.Ok(new ApiResponse { Success = true, Message = "Reminder set." });
});

// GET /api/reminders/{userId}
// Returns: [ { "reminderId":1, "reminderType":"Period", "reminderDate":"2024-06-01", "status":"Pending" }, ... ]
app.MapGet("/api/reminders/{userId:int}", (int userId, DatabaseHelper db) =>
{
    var reminders = db.GetUpcomingReminders(userId);
    return Results.Ok(reminders);
});

// PUT /api/reminders/done
// Body: { "userId":3, "reminderType":"Period" }
app.MapPut("/api/reminders/done", (MarkDoneRequest req, DatabaseHelper db) =>
{
    db.MarkReminderDone(req);
    db.LogActivity(req.UserId, "MARK_REMINDER_DONE", req.ReminderType);
    return Results.Ok(new ApiResponse { Success = true, Message = "Reminder marked as done." });
});

// ════════════════════════════════════════════════════════════════════════════
// GOODIE BASKET ORDER
// ════════════════════════════════════════════════════════════════════════════

// POST /api/orders
// Body: full OrderRequest object
app.MapPost("/api/orders", (OrderRequest req, DatabaseHelper db) =>
{
    db.PlaceOrder(req);
    db.LogActivity(req.UserId, "PLACE_ORDER", $"{req.FlowType} - {req.City}");
    return Results.Ok(new ApiResponse { Success = true, Message = "Order placed successfully!" });
});

// ════════════════════════════════════════════════════════════════════════════
// ACTIVITY LOG
// ════════════════════════════════════════════════════════════════════════════

// POST /api/activity
// Body: { "userId":3, "action":"VIEW_HOME", "detail":"" }
app.MapPost("/api/activity", (ActivityRequest req, DatabaseHelper db) =>
{
    db.LogActivity(req.UserId, req.Action, req.Detail ?? "");
    return Results.Ok(new ApiResponse { Success = true });
});

app.Run();