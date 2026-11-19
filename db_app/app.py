# app.py — GUI: серый минималистичный интерфейс. Вход -> главное окно с 7 разделами (пункты 3-7 задания)
import tkinter as tk
from tkinter import ttk, messagebox
import database as db

BG, FG, BTN, SEL = "#d4d4d4", "#2b2b2b", "#b8b8b8", "#a0a0a0"
FONT = ("Segoe UI", 10)


def style_root(root, title, w, h):
    root.title(title)
    root.geometry(f"{w}x{h}")
    root.configure(bg=BG)
    root.resizable(True, True)


def mk_button(parent, text, cmd, width=18):
    b = tk.Button(parent, text=text, command=cmd, bg=BTN, fg=FG, font=FONT,
                  relief="flat", bd=1, width=width, activebackground=SEL)
    return b


class LoginWindow:
    # окно входа пользователя
    def __init__(self):
        self.root = tk.Tk()
        style_root(self.root, "Вход в систему", 320, 200)
        self.user = None
        frm = tk.Frame(self.root, bg=BG); frm.pack(expand=True)
        tk.Label(frm, text="Логин", bg=BG, fg=FG, font=FONT).pack(pady=(0, 2))
        self.e_login = tk.Entry(frm, font=FONT, relief="flat", bg="#e8e8e8", width=22); self.e_login.pack(pady=2)
        tk.Label(frm, text="Пароль", bg=BG, fg=FG, font=FONT).pack()
        self.e_pass = tk.Entry(frm, show="*", font=FONT, relief="flat", bg="#e8e8e8", width=22); self.e_pass.pack(pady=2)
        mk_button(frm, "Войти", self.do_login).pack(pady=12)
        self.e_login.focus()

    def do_login(self):
        u = db.login(self.e_login.get(), self.e_pass.get())
        if not u:
            messagebox.showerror("Ошибка", "Неверный логин или пароль")
            return
        self.user = u
        self.root.destroy()


class MainWindow:
    # главное окно: меню разделов + контент-зона
    def __init__(self, user):
        self.user = user
        self.perms = db.ROLES[user["role"]]
        self.root = tk.Tk()
        style_root(self.root, f"Управление БД — {user['login']} ({user['role']})", 900, 600)
        menu = tk.Frame(self.root, bg="#c0c0c0", width=220)
        menu.pack(side="left", fill="y"); menu.pack_propagate(False)
        self.content = tk.Frame(self.root, bg=BG)
        self.content.pack(side="right", fill="both", expand=True, padx=10, pady=10)

        items = [
            ("Проектирование БД", self.page_design),
            ("Схема и связи данных", self.page_schema),
            ("Разграничение прав", self.page_roles),
            ("Health checker / просмотр БД", self.page_health),
            ("Контроль: CRUD данных", self.page_crud),
            ("Система тестирования", self.page_test),
            ("Журнал действий", self.page_audit),
        ]
        tk.Label(menu, text="  РАЗДЕЛЫ", bg="#c0c0c0", fg="#555", font=("Segoe UI", 9, "bold")).pack(pady=(10, 5))
        for text, fn in items:
            mk_button(menu, text, fn, 24).pack(fill="x", padx=6, pady=3)

    def run(self):
        self.root.mainloop()

    def clear(self):
        for w in self.content.winfo_children():
            w.destroy()

    def page_design(self):
        # пункт 3: проектирование модели данных + создание таблиц
        self.clear()
        tk.Label(self.content, text="Проектирование БД — создание таблиц", bg=BG, fg=FG,
                 font=("Segoe UI", 12, "bold")).pack(anchor="w")
        tk.Label(self.content, text="Системная модель: users(id, login, password, role) | audit_log(id, user, action, target)",
                 bg=BG, fg="#555", font=FONT).pack(anchor="w", pady=4)
        if not self.perms["create_table"]:
            tk.Label(self.content, text="Недоступно для вашей роли", bg=BG, fg="#777", font=FONT).pack(pady=20)
            return
        frm = tk.Frame(self.content, bg=BG); frm.pack(anchor="w", pady=10)
        tk.Label(frm, text="Имя таблицы", bg=BG, fg=FG, font=FONT).grid(row=0, column=0, sticky="w")
        self.t_name = tk.Entry(frm, font=FONT, relief="flat", bg="#e8e8e8"); self.t_name.grid(row=0, column=1, padx=6)
        tk.Label(frm, text="Колонки (по одной: имя тип)", bg=BG, fg=FG, font=FONT).grid(row=1, column=0, sticky="nw")
        self.t_cols = tk.Text(frm, font=FONT, relief="flat", bg="#e8e8e8", width=40, height=6)
        self.t_cols.insert("1.0", "id INTEGER PRIMARY KEY AUTOINCREMENT\n")
        self.t_cols.grid(row=1, column=1, padx=6)
        mk_button(frm, "Создать", self.do_create_table).grid(row=2, column=1, pady=8, sticky="w")
        tk.Label(self.content, text="Таблицы БД:", bg=BG, fg=FG, font=FONT).pack(anchor="w", pady=(10, 2))
        tree = self.mk_tree(self.content, ["Таблицы"], [(t,) for t in db.get_tables()])
        tree.pack(fill="x")

    def do_create_table(self):
        name = self.t_name.get().strip()
        cols = self.t_cols.get("1.0", "end").strip().splitlines()
        if not name or not cols:
            messagebox.showerror("Ошибка", "Заполните имя и колонки")
            return
        try:
            db.create_table(name, cols)
            db.log_action(self.user["login"], "create_table", name)
            messagebox.showinfo("OK", f"Таблица {name} создана")
        except Exception as e:
            messagebox.showerror("Ошибка", str(e))

    def page_schema(self):
        # пункт 3(б): схема данных — структура таблиц и связи
        self.clear()
        tk.Label(self.content, text="Схема и связи данных", bg=BG, fg=FG,
                 font=("Segoe UI", 12, "bold")).pack(anchor="w")
        rows = []
        rows.append(("users", "id, login, password, role, created_at", "системная — хранит пользователей и роли"))
        rows.append(("audit_log", "id, user, action, target, created_at", "журнал; user -> users.login"))
        for t in db.get_tables():
            cols = ", ".join(c["name"] for c in db.get_columns(t))
            rows.append((t, cols, "пользовательская"))
        self.mk_tree(self.content, ["Таблица", "Колонки", "Описание"], rows).pack(fill="x", pady=8)

    def page_roles(self):
        # пункт 4: разграничение прав
        self.clear()
        tk.Label(self.content, text="Разграничение прав", bg=BG, fg=FG, font=("Segoe UI", 12, "bold")).pack(anchor="w")
        rows = []
        for role, perms in db.ROLES.items():
            allowed = [k for k, v in perms.items() if v]
            rows.append((role, ", ".join(allowed) or "только просмотр"))
        self.mk_tree(self.content, ["Роль", "Возможности"], rows).pack(fill="x", pady=8)
        tk.Label(self.content, text=f"Ваша роль: {self.user['role']}", bg=BG, fg="#555", font=FONT).pack(anchor="w")
        if self.perms["manage_users"]:
            frm = tk.Frame(self.content, bg=BG); frm.pack(anchor="w", pady=10)
            tk.Label(frm, text="Логин", bg=BG, fg=FG, font=FONT).grid(row=0, column=0)
            self.u_login = tk.Entry(frm, font=FONT, relief="flat", bg="#e8e8e8"); self.u_login.grid(row=0, column=1, padx=4)
            tk.Label(frm, text="Пароль", bg=BG, fg=FG, font=FONT).grid(row=0, column=2)
            self.u_pass = tk.Entry(frm, font=FONT, relief="flat", bg="#e8e8e8"); self.u_pass.grid(row=0, column=3, padx=4)
            tk.Label(frm, text="Роль", bg=BG, fg=FG, font=FONT).grid(row=0, column=4)
            self.u_role = ttk.Combobox(frm, values=["admin", "user", "viewer"], state="readonly", width=8)
            self.u_role.current(2); self.u_role.grid(row=0, column=5, padx=4)
            mk_button(frm, "Добавить пользователя", self.do_add_user).grid(row=0, column=6, padx=6)
            self.mk_tree(self.content, ["ID", "Логин", "Роль", "Создан"],
                         [(u["id"], u["login"], u["role"], u["created_at"]) for u in db.list_users()]).pack(fill="x", pady=8)

    def do_add_user(self):
        try:
            db.add_user(self.u_login.get().strip(), self.u_pass.get(), self.u_role.get())
            db.log_action(self.user["login"], "add_user", self.u_login.get())
            self.page_roles()
        except Exception as e:
            messagebox.showerror("Ошибка", str(e))

    def page_health(self):
        # пункт 5: health checker + просмотр БД
        self.clear()
        tk.Label(self.content, text="Health checker и просмотр БД", bg=BG, fg=FG,
                 font=("Segoe UI", 12, "bold")).pack(anchor="w")
        mk_button(self.content, "Запустить проверку", self.do_health).pack(anchor="w", pady=6)
        self.health_tree = self.mk_tree(self.content, ["Статус", "Проверка"], [])
        self.health_tree.pack(fill="x", pady=4)
        frm = tk.Frame(self.content, bg=BG); frm.pack(fill="both", expand=True)
        tk.Label(frm, text="Просмотр таблицы:", bg=BG, fg=FG, font=FONT).pack(side="left", padx=4)
        self.view_combo = ttk.Combobox(frm, state="readonly", values=["users"] + db.get_tables())
        self.view_combo.current(0); self.view_combo.pack(side="left")
        mk_button(frm, "Показать", self.do_view, 10).pack(side="left", padx=6)
        self.data_tree = self.mk_tree(frm, [], [])
        self.data_tree.pack(fill="both", expand=True, pady=6)

    def do_health(self):
        for i in self.health_tree.get_children():
            self.health_tree.delete(i)
        for status, msg in db.health_check():
            self.health_tree.insert("", "end", values=(status, msg))
        db.log_action(self.user["login"], "health_check")

    def do_view(self):
        table = self.view_combo.get()
        for i in self.data_tree.get_children():
            self.data_tree.delete(i)
        self.data_tree["columns"] = []
        rows = db.select_rows(table)
        cols = [c["name"] for c in db.get_columns(table)]
        self.data_tree["columns"] = cols
        for c in cols:
            self.data_tree.heading(c, text=c); self.data_tree.column(c, width=120)
        for r in rows:
            self.data_tree.insert("", "end", values=r)

    def page_crud(self):
        # пункт 6: контроль данных (вставка/изменение/удаление с учётом прав)
        self.clear()
        tk.Label(self.content, text="Контроль данных (CRUD)", bg=BG, fg=FG,
                 font=("Segoe UI", 12, "bold")).pack(anchor="w")
        if not self.perms["view"]:
            return
        frm = tk.Frame(self.content, bg=BG); frm.pack(anchor="w", pady=6)
        tk.Label(frm, text="Таблица:", bg=BG, fg=FG, font=FONT).pack(side="left", padx=4)
        self.crud_combo = ttk.Combobox(frm, state="readonly", values=db.get_tables())
        if self.crud_combo["values"]:
            self.crud_combo.current(0)
        self.crud_combo.pack(side="left")
        mk_button(frm, "Открыть", lambda: self.show_crud_table(self.crud_combo.get()), 10).pack(side="left", padx=6)
        self.crud_zone = tk.Frame(self.content, bg=BG)
        self.crud_zone.pack(fill="both", expand=True)

    def show_crud_table(self, table):
        for w in self.crud_zone.winfo_children():
            w.destroy()
        cols = db.get_columns(table)
        pk_col = cols[0]["name"]
        rows = db.select_rows(table)

        tree = self.mk_tree(self.crud_zone, [c["name"] for c in cols], [tuple(r) for r in rows])
        tree.pack(fill="both", expand=True)

        entrys = {}
        form = tk.Frame(self.crud_zone, bg=BG); form.pack(fill="x", pady=6)
        tk.Label(form, text="Колонка=значение:", bg=BG, fg=FG, font=FONT).pack(side="left", padx=4)
        for c in cols:
            e = tk.Entry(form, font=FONT, relief="flat", bg="#e8e8e8", width=14)
            e.insert(0, c["name"]); entrys[c["name"]] = e
            e.pack(side="left", padx=2)
        btns = tk.Frame(self.crud_zone, bg=BG); btns.pack(anchor="w")
        if self.perms["insert"]:
            mk_button(btns, "Добавить", lambda: self.crud_insert(table, cols, entrys), 12).pack(side="left", padx=3)
        if self.perms["update"]:
            mk_button(btns, "Изменить (по ID)", lambda: self.crud_update(table, cols, pk_col, entrys), 16).pack(side="left", padx=3)
        if self.perms["delete"]:
            mk_button(btns, "Удалить (по ID)", lambda: self.crud_delete(table, pk_col, entrys), 16).pack(side="left", padx=3)
        if not any([self.perms["insert"], self.perms["update"], self.perms["delete"]]):
            tk.Label(btns, text="Роль viewer: только просмотр", bg=BG, fg="#777", font=FONT).pack(side="left")

    def crud_insert(self, table, cols, entrys):
        values = {c["name"]: entrys[c["name"]].get() for c in cols if entrys[c["name"]].get()}
        try:
            db.insert_row(table, values)
            db.log_action(self.user["login"], "insert", table)
            self.show_crud_table(table)
        except Exception as e:
            messagebox.showerror("Ошибка", str(e))

    def crud_update(self, table, cols, pk_col, entrys):
        pk = entrys[pk_col].get()
        values = {c["name"]: entrys[c["name"]].get() for c in cols[1:] if entrys[c["name"]].get()}
        try:
            db.update_row(table, pk_col, pk, values)
            db.log_action(self.user["login"], "update", table)
            self.show_crud_table(table)
        except Exception as e:
            messagebox.showerror("Ошибка", str(e))

    def crud_delete(self, table, pk_col, entrys):
        pk = entrys[pk_col].get()
        try:
            db.delete_row(table, pk_col, pk)
            db.log_action(self.user["login"], "delete", table)
            self.show_crud_table(table)
        except Exception as e:
            messagebox.showerror("Ошибка", str(e))

    def page_test(self):
        # пункт 7: система тестирования БД (проверка запуска и валидности)
        self.clear()
        tk.Label(self.content, text="Система тестирования БД", bg=BG, fg=FG,
                 font=("Segoe UI", 12, "bold")).pack(anchor="w")
        btns = tk.Frame(self.content, bg=BG); btns.pack(anchor="w", pady=6)
        mk_button(btns, "1. Проверить запуск системы", self.test_startup, 26).pack(side="left", padx=3)
        mk_button(btns, "2. Проверить валидность данных", self.test_validate, 26).pack(side="left", padx=3)
        self.test_tree = self.mk_tree(self.content, ["Результат", "Описание"], [])
        self.test_tree.pack(fill="x", pady=6)

    def test_startup(self):
        for i in self.test_tree.get_children():
            self.test_tree.delete(i)
        for status, msg in db.health_check():
            self.test_tree.insert("", "end", values=(("ПРОЙДЕНО" if status == "OK" else "ПРОВАЛ"), msg))
        db.log_action(self.user["login"], "test_startup")

    def test_validate(self):
        for i in self.test_tree.get_children():
            self.test_tree.delete(i)
        for status, msg in db.validate_data():
            self.test_tree.insert("", "end", values=(("ПРОЙДЕНО" if status == "OK" else "ПРОВАЛ"), msg))
        db.log_action(self.user["login"], "test_validate")

    def page_audit(self):
        # журнал действий пользователей
        self.clear()
        tk.Label(self.content, text="Журнал действий", bg=BG, fg=FG, font=("Segoe UI", 12, "bold")).pack(anchor="w")
        conn = db.connect()
        rows = [tuple(r) for r in conn.execute("SELECT user, action, target, created_at FROM audit_log ORDER BY id DESC LIMIT 100")]
        conn.close()
        self.mk_tree(self.content, ["Пользователь", "Действие", "Объект", "Время"], rows).pack(fill="both", expand=True, pady=6)

    def mk_tree(self, parent, cols, rows):
        tree = ttk.Treeview(parent, columns=cols, show="headings", height=8)
        for c in cols:
            tree.heading(c, text=c); tree.column(c, width=130)
        style = ttk.Style()
        style.configure("Treeview", background="#e0e0e0", fieldbackground="#e8e8e8",
                        foreground=FG, font=FONT, rowheight=24)
        style.configure("Treeview.Heading", background="#b0b0b0", font=("Segoe UI", 10, "bold"))
        for r in rows:
            tree.insert("", "end", values=r)
        return tree


def main():
    db.init_db()
    login = LoginWindow()
    login.root.mainloop()
    if not login.user:
        return
    MainWindow(login.user).run()


if __name__ == "__main__":
    main()
