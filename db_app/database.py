# database.py — слой БД: схема, соединение, health checker, валидация данных
import sqlite3
from datetime import datetime

DB_FILE = "app.db"

ROLES = {
    "admin":  {"create_table": True, "insert": True, "update": True, "delete": True, "manage_users": True, "view": True},
    "user":   {"create_table": False, "insert": True, "update": True, "delete": False, "manage_users": False, "view": True},
    "viewer": {"create_table": False, "insert": False, "update": False, "delete": False, "manage_users": False, "view": True},
}


def connect():
    conn = sqlite3.connect(DB_FILE)
    conn.row_factory = sqlite3.Row
    return conn


def init_db():
    # системные таблицы: users, roles, audit_log
    conn = connect()
    cur = conn.cursor()
    cur.execute("""CREATE TABLE IF NOT EXISTS users(
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        login TEXT UNIQUE NOT NULL,
        password TEXT NOT NULL,
        role TEXT NOT NULL DEFAULT 'viewer',
        created_at TEXT DEFAULT CURRENT_TIMESTAMP)""")
    cur.execute("""CREATE TABLE IF NOT EXISTS audit_log(
        id INTEGER PRIMARY KEY AUTOINCREMENT,
        user TEXT, action TEXT, target TEXT,
        created_at TEXT DEFAULT CURRENT_TIMESTAMP)""")
    if not cur.execute("SELECT 1 FROM users LIMIT 1").fetchone():
        cur.execute("INSERT INTO users(login,password,role) VALUES('admin','admin','admin')")
    conn.commit()
    conn.close()


def login(login, password):
    conn = connect()
    row = conn.execute("SELECT * FROM users WHERE login=? AND password=?", (login, password)).fetchone()
    conn.close()
    return dict(row) if row else None


def log_action(user, action, target=""):
    conn = connect()
    conn.execute("INSERT INTO audit_log(user,action,target) VALUES(?,?,?)", (user, action, target))
    conn.commit()
    conn.close()


def list_users():
    conn = connect()
    rows = [dict(r) for r in conn.execute("SELECT id,login,role,created_at FROM users")]
    conn.close()
    return rows


def add_user(login, password, role):
    conn = connect()
    conn.execute("INSERT INTO users(login,password,role) VALUES(?,?,?)", (login, password, role))
    conn.commit(); conn.close()


def delete_user(uid):
    conn = connect()
    conn.execute("DELETE FROM users WHERE id=?", (uid,))
    conn.commit(); conn.close()


# ---- операции с таблицами (с проверкой прав) ----

def get_tables():
    conn = connect()
    rows = [r["name"] for r in conn.execute(
        "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT IN ('users','audit_log')")]
    conn.close()
    return rows


def get_columns(table):
    conn = connect()
    cols = [dict(r) for r in conn.execute(f"PRAGMA table_info('{table}')")]
    conn.close()
    return cols


def create_table(name, columns):
    # columns: список строк "имя тип"
    conn = connect()
    defs = ", ".join(c.strip() for c in columns if c.strip())
    conn.execute(f"CREATE TABLE IF NOT EXISTS {name}({defs})")
    conn.commit(); conn.close()


def select_rows(table, limit=100):
    conn = connect()
    rows = [list(r) for r in conn.execute(f"SELECT * FROM {table} LIMIT ?", (limit,))]
    conn.close()
    return rows


def insert_row(table, values):
    conn = connect()
    cols = list(values.keys())
    ph = ",".join("?" * len(cols))
    conn.execute(f"INSERT INTO {table}({','.join(cols)}) VALUES({ph})", list(values.values()))
    conn.commit(); conn.close()


def update_row(table, pk_col, pk, values):
    conn = connect()
    set_part = ",".join(f"{k}=?" for k in values)
    conn.execute(f"UPDATE {table} SET {set_part} WHERE {pk_col}=?", (*values.values(), pk))
    conn.commit(); conn.close()


def delete_row(table, pk_col, pk):
    conn = connect()
    conn.execute(f"DELETE FROM {table} WHERE {pk_col}=?", (pk,))
    conn.commit(); conn.close()


def health_check():
    # health checker: доступность БД + базовая статистика
    result = []
    try:
        conn = connect()
        conn.execute("SELECT 1")
        result.append(("OK", "Соединение с БД"))
        for t in ["users", "audit_log"] + get_tables():
            try:
                conn.execute(f"SELECT COUNT(*) FROM {t}").fetchone()
                result.append(("OK", f"Таблица {t} читается"))
            except Exception as e:
                result.append(("FAIL", f"Таблица {t}: {e}"))
        conn.close()
    except Exception as e:
        result.append(("FAIL", f"БД недоступна: {e}"))
    return result


def validate_data():
    # проверка валидности данных: пустые логины, дубликаты, битые роли
    issues = []
    conn = connect()
    dupes = conn.execute("SELECT login,COUNT(*) c FROM users GROUP BY login HAVING c>1").fetchall()
    if dupes:
        issues.append(("FAIL", f"Дубликаты логинов: {[d['login'] for d in dupes]}"))
    bad_roles = conn.execute("SELECT login FROM users WHERE role NOT IN ('admin','user','viewer')").fetchall()
    if bad_roles:
        issues.append(("FAIL", f"Неизвестные роли: {[r['login'] for r in bad_roles]}"))
    empty = conn.execute("SELECT login FROM users WHERE TRIM(login)='' OR TRIM(password)=''").fetchall()
    if empty:
        issues.append(("FAIL", "Пустые логины/пароли"))
    if not issues:
        issues.append(("OK", "Валидация данных: нарушений не найдено"))
    conn.close()
    return issues
