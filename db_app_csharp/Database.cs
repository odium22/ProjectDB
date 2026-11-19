// Database.cs — слой БД: схема, соединение, права, CRUD, health checker, валидация
using System.Data;
using System.Data.SQLite;

namespace DbApp;

// роли и права (пункт 4: разграничение прав)
public static class Roles
{
    public static readonly Dictionary<string, Dictionary<string, bool>> Perms = new()
    {
        ["admin"]  = new() { ["create_table"] = true,  ["insert"] = true, ["update"] = true,  ["delete"] = true,  ["manage_users"] = true,  ["view"] = true },
        ["user"]   = new() { ["create_table"] = false, ["insert"] = true, ["update"] = true,  ["delete"] = false, ["manage_users"] = false, ["view"] = true },
        ["viewer"] = new() { ["create_table"] = false, ["insert"] = false, ["update"] = false, ["delete"] = false, ["manage_users"] = false, ["view"] = true },
    };
}

public class DbUser
{
    public int Id;
    public string Login = "";
    public string Role = "";
}

public static class Database
{
    const string DbFile = "app.db";

    static SQLiteConnection Connect()
    {
        var conn = new SQLiteConnection($"Data Source={DbFile};Version=3;");
        conn.Open();
        return conn;
    }

    // инициализация системных таблиц (пункт 3: модель данных)
    public static void Init()
    {
        using var conn = Connect();
        Exec(conn, @"CREATE TABLE IF NOT EXISTS users(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            login TEXT UNIQUE NOT NULL, password TEXT NOT NULL,
            role TEXT NOT NULL DEFAULT 'viewer', created_at TEXT DEFAULT CURRENT_TIMESTAMP)");
        Exec(conn, @"CREATE TABLE IF NOT EXISTS audit_log(
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            user TEXT, action TEXT, target TEXT, created_at TEXT DEFAULT CURRENT_TIMESTAMP)");
        if (Query(conn, "SELECT COUNT(*) FROM users").Rows[0][0].ToString() == "0")
        {
            var cmd = new SQLiteCommand("INSERT INTO users(login,password,role) VALUES('admin','admin','admin')", conn);
            cmd.ExecuteNonQuery();
        }
    }

    static void Exec(SQLiteConnection conn, string sql)
    {
        new SQLiteCommand(sql, conn).ExecuteNonQuery();
    }

    static DataTable Query(SQLiteConnection conn, string sql, params object[] args)
    {
        var cmd = new SQLiteCommand(sql, conn);
        for (int i = 0; i < args.Length; i++)
            cmd.Parameters.AddWithValue($"@p{i}", args[i]);
        // заменяем ? на именованные параметры
        for (int i = 0; i < args.Length; i++)
            sql = ReplaceFirst(sql, "?", $"@p{i}");
        cmd.CommandText = sql;
        var dt = new DataTable();
        new SQLiteDataAdapter(cmd).Fill(dt);
        return dt;
    }

    static string ReplaceFirst(string s, string old, string repl)
    {
        int i = s.IndexOf(old);
        return i < 0 ? s : s[..i] + repl + s[(i + old.Length)..];
    }

    public static DbUser? Login(string login, string password)
    {
        using var conn = Connect();
        var dt = Query(conn, "SELECT id, login, role FROM users WHERE login=? AND password=?", login, password);
        if (dt.Rows.Count == 0) return null;
        return new DbUser { Id = Convert.ToInt32(dt.Rows[0]["id"]), Login = dt.Rows[0]["login"].ToString()!, Role = dt.Rows[0]["role"].ToString()! };
    }

    public static void LogAction(string user, string action, string target = "")
    {
        using var conn = Connect();
        Query(conn, "INSERT INTO audit_log(user,action,target) VALUES(?,?,?)", user, action, target);
    }

    public static DataTable ListUsers()
    {
        using var conn = Connect();
        return Query(conn, "SELECT id, login, role, created_at FROM users");
    }

    public static void AddUser(string login, string password, string role)
    {
        using var conn = Connect();
        Query(conn, "INSERT INTO users(login,password,role) VALUES(?,?,?)", login, password, role);
    }

    // ---- таблицы (пункт 3) ----
    public static List<string> GetTables()
    {
        using var conn = Connect();
        var dt = Query(conn, "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT IN ('users','audit_log')");
        return dt.Rows.Cast<DataRow>().Select(r => r["name"].ToString()!).ToList();
    }

    public static List<(string Name, string Type)> GetColumns(string table)
    {
        using var conn = Connect();
        var dt = Query(conn, $"PRAGMA table_info('{table}')");
        return dt.Rows.Cast<DataRow>().Select(r => (r["name"].ToString()!, r["type"].ToString()!)).ToList();
    }

    public static void CreateTable(string name, string columnsSql)
    {
        using var conn = Connect();
        Exec(conn, $"CREATE TABLE IF NOT EXISTS {name}({columnsSql})");
    }

    // ---- CRUD (пункт 6) ----
    public static DataTable SelectRows(string table)
    {
        using var conn = Connect();
        return Query(conn, $"SELECT * FROM {table} LIMIT 100");
    }

    public static void InsertRow(string table, Dictionary<string, object> values)
    {
        using var conn = Connect();
        var cols = string.Join(",", values.Keys);
        var ph = string.Join(",", values.Keys.Select(_ => "?"));
        Query(conn, $"INSERT INTO {table}({cols}) VALUES({ph})", values.Values.ToArray());
    }

    public static void UpdateRow(string table, string pkCol, object pk, Dictionary<string, object> values)
    {
        using var conn = Connect();
        var set = string.Join(",", values.Keys.Select(k => $"{k}=?"));
        Query(conn, $"UPDATE {table} SET {set} WHERE {pkCol}=?", values.Values.Append(pk).ToArray());
    }

    public static void DeleteRow(string table, string pkCol, object pk)
    {
        using var conn = Connect();
        Query(conn, $"DELETE FROM {table} WHERE {pkCol}=?", pk);
    }

    // ---- health checker (пункт 5) ----
    public static List<(string, string)> HealthCheck()
    {
        var result = new List<(string, string)>();
        try
        {
            using var conn = Connect();
            Query(conn, "SELECT 1");
            result.Add(("OK", "Соединение с БД"));
            foreach (var t in new[] { "users", "audit_log" }.Concat(GetTables()))
            {
                try { Query(conn, $"SELECT COUNT(*) FROM {t}"); result.Add(("OK", $"Таблица {t} читается")); }
                catch (Exception ex) { result.Add(("FAIL", $"Таблица {t}: {ex.Message}")); }
            }
        }
        catch (Exception ex) { result.Add(("FAIL", $"БД недоступна: {ex.Message}")); }
        return result;
    }

    // ---- валидация данных (пункт 7) ----
    public static List<(string, string)> ValidateData()
    {
        var issues = new List<(string, string)>();
        using var conn = Connect();
        var dupes = Query(conn, "SELECT login FROM users GROUP BY login HAVING COUNT(*)>1");
        if (dupes.Rows.Count > 0)
            issues.Add(("FAIL", "Дубликаты логинов: " + string.Join(", ", dupes.Rows.Cast<DataRow>().Select(r => r["login"]))));
        var bad = Query(conn, "SELECT login FROM users WHERE role NOT IN ('admin','user','viewer')");
        if (bad.Rows.Count > 0)
            issues.Add(("FAIL", "Неизвестные роли: " + string.Join(", ", bad.Rows.Cast<DataRow>().Select(r => r["login"]))));
        var empty = Query(conn, "SELECT login FROM users WHERE TRIM(login)='' OR TRIM(password)=''");
        if (empty.Rows.Count > 0) issues.Add(("FAIL", "Пустые логины/пароли"));
        if (issues.Count == 0) issues.Add(("OK", "Валидация данных: нарушений не найдено"));
        return issues;
    }

    public static DataTable GetAuditLog()
    {
        using var conn = Connect();
        return Query(conn, "SELECT user, action, target, created_at FROM audit_log ORDER BY id DESC LIMIT 100");
    }
}
