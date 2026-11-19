// App.cs — GUI: серый минималистичный WinForms. Вход -> главное окно с разделами (пункты 3-7)
using System.Data;

namespace DbApp;

static class Style
{
    // серая палитра
    public static readonly Color Bg = Color.FromArgb(212, 212, 212);
    public static readonly Color Menu = Color.FromArgb(192, 192, 192);
    public static readonly Color Btn = Color.FromArgb(184, 184, 184);
    public static readonly Color Field = Color.FromArgb(232, 232, 232);
    public static readonly Color Fg = Color.FromArgb(43, 43, 43);

    public static Button MkButton(string text, EventHandler onClick, int w = 180)
    {
        var b = new Button { Text = text, BackColor = Btn, ForeColor = Fg, FlatStyle = FlatStyle.Flat,
            Width = w, Height = 30, Font = new Font("Segoe UI", 9.75f) };
        b.FlatAppearance.BorderSize = 1;
        b.Click += onClick;
        return b;
    }

    public static Label MkLabel(string text)
    {
        return new Label { Text = text, BackColor = Bg, ForeColor = Fg, Font = new Font("Segoe UI", 9.75f), AutoSize = true };
    }

    public static TextBox MkTextBox(int w = 180)
    {
        return new TextBox { BackColor = Field, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9.75f), Width = w };
    }

    public static DataGridView MkGrid()
    {
        var g = new DataGridView { BackgroundColor = Bg, ReadOnly = true, AllowUserToAddRows = false,
            AllowUserToDeleteRows = false, RowHeadersVisible = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, BorderStyle = BorderStyle.FixedSingle, Dock = DockStyle.Fill };
        g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(176, 176, 176);
        g.EnableHeadersVisualStyles = false;
        return g;
    }

    public static DataGridView FillGrid(DataGridView g, DataTable dt)
    {
        g.DataSource = dt;
        return g;
    }
}

class LoginForm : Form
{
    // окно входа
    readonly TextBox txtLogin = Style.MkTextBox();
    readonly TextBox txtPass = Style.MkTextBox();
    public DbUser? User;

    public LoginForm()
    {
        Text = "Вход в систему";
        Size = new Size(330, 220);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Style.Bg;
        StartPosition = FormStartPosition.CenterScreen;

        var lblL = Style.MkLabel("Логин"); lblL.SetBounds(20, 20, 100, 20);
        var lblP = Style.MkLabel("Пароль"); lblP.SetBounds(20, 60, 100, 20);
        txtLogin.SetBounds(20, 40, 260, 26);
        txtPass.SetBounds(20, 80, 260, 26); txtPass.PasswordChar = '*';
        var btn = Style.MkButton("Войти", (s, e) => DoLogin(), 262); btn.SetBounds(20, 120, 262, 32);
        Controls.AddRange(new Control[] { lblL, lblP, txtLogin, txtPass, btn });
    }

    void DoLogin()
    {
        User = Database.Login(txtLogin.Text, txtPass.Text);
        if (User == null) { MessageBox.Show("Неверный логин или пароль", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
        Database.LogAction(User.Login, "login");
        DialogResult = DialogResult.OK;
        Close();
    }
}

class MainForm : Form
{
    // главное окно: меню разделов + контент-зона
    readonly DbUser user;
    readonly Dictionary<string, bool> perms;
    readonly Panel content = new() { Dock = DockStyle.Fill, BackColor = Style.Bg };
    readonly TextBox txtTable = Style.MkTextBox();
    readonly TextBox txtCols = Style.MkTextBox(400);
    ComboBox cmbView = new(), cmbCrud = new(), cmbRole = new();
    TextBox txtULogin = Style.MkTextBox(), txtUPass = Style.MkTextBox();
    DataGridView gridHealth = Style.MkGrid(), gridData = Style.MkGrid(), gridCrud = Style.MkGrid();
    FlowLayoutPanel crudBtns = new();

    public MainForm(DbUser u)
    {
        user = u;
        perms = Roles.Perms[u.Role];
        Text = $"Управление БД — {u.Login} ({u.Role})";
        Size = new Size(950, 620);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Style.Bg;

        var menu = new Panel { Dock = DockStyle.Left, Width = 230, BackColor = Style.Menu };
        var title = Style.MkLabel("  РАЗДЕЛЫ"); title.SetBounds(10, 10, 200, 20);
        menu.Controls.Add(title);

        (string, EventHandler)[] items =
        {
            ("Проектирование БД", (s,e) => PageDesign()),
            ("Схема и связи данных", (s,e) => PageSchema()),
            ("Разграничение прав", (s,e) => PageRoles()),
            ("Health checker / просмотр", (s,e) => PageHealth()),
            ("Контроль: CRUD данных", (s,e) => PageCrud()),
            ("Система тестирования", (s,e) => PageTest()),
            ("Журнал действий", (s,e) => PageAudit()),
        };
        int y = 35;
        foreach (var (text, fn) in items)
        {
            var b = Style.MkButton(text, fn, 214);
            b.SetBounds(8, y, 214, 30);
            menu.Controls.Add(b);
            y += 36;
        }
        Controls.Add(content);
        Controls.Add(menu);
        PageDesign();
    }

    void Clear() { content.Controls.Clear(); }

    Label Header(string text)
    {
        var l = Style.MkLabel(text); l.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        return l;
    }

    // пункт 3: проектирование БД — создание таблиц
    void PageDesign()
    {
        Clear();
        var header = Header("Проектирование БД — создание таблиц"); header.SetBounds(10, 10, 600, 24);
        var model = Style.MkLabel("Системная модель: users(id, login, password, role) | audit_log(id, user, action, target)");
        model.ForeColor = Color.FromArgb(85, 85, 85); model.SetBounds(10, 38, 700, 20);

        content.Controls.Add(header); content.Controls.Add(model);

        if (!perms["create_table"])
        {
            var den = Style.MkLabel("Недоступно для вашей роли"); den.ForeColor = Color.FromArgb(119, 119, 119);
            den.SetBounds(10, 70, 300, 20); content.Controls.Add(den);
            return;
        }

        var l1 = Style.MkLabel("Имя таблицы"); l1.SetBounds(10, 70, 100, 20);
        txtTable.SetBounds(120, 68, 200, 26);
        var l2 = Style.MkLabel("Колонки (по одной на строку: имя тип)"); l2.SetBounds(10, 105, 260, 20);
        txtCols.Multiline = true; txtCols.Height = 90; txtCols.SetBounds(10, 125, 400, 90);
        txtCols.Text = "id INTEGER PRIMARY KEY AUTOINCREMENT\r\n";
        var btn = Style.MkButton("Создать", (s, e) => DoCreateTable(), 100); btn.SetBounds(10, 225, 100, 30);

        var lt = Style.MkLabel("Таблицы БД:"); lt.SetBounds(10, 265, 100, 20);
        var grid = Style.MkGrid();
        var holder = new Panel { Dock = DockStyle.Bottom, Height = 250 };
        holder.Controls.Add(grid);
        grid.Dock = DockStyle.Fill;

        var tables = new DataTable(); tables.Columns.Add("Таблицы");
        foreach (var t in Database.GetTables()) tables.Rows.Add(t);
        Style.FillGrid(grid, tables);

        content.Controls.AddRange(new Control[] { l1, txtTable, l2, txtCols, btn, lt, holder });
    }

    void DoCreateTable()
    {
        var name = txtTable.Text.Trim();
        if (name == "" || txtCols.Text.Trim() == "") { MessageBox.Show("Заполните имя и колонки"); return; }
        try
        {
            var cols = string.Join(", ", txtCols.Text.Split("\n", StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
            Database.CreateTable(name, cols);
            Database.LogAction(user.Login, "create_table", name);
            MessageBox.Show($"Таблица {name} создана", "OK");
            PageDesign();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    // пункт 3(б): схема данных
    void PageSchema()
    {
        Clear();
        var header = Header("Схема и связи данных"); header.SetBounds(10, 10, 400, 24);
        content.Controls.Add(header);

        var dt = new DataTable();
        dt.Columns.Add("Таблица"); dt.Columns.Add("Колонки"); dt.Columns.Add("Описание");
        dt.Rows.Add("users", "id, login, password, role, created_at", "системная — хранит пользователей и роли");
        dt.Rows.Add("audit_log", "id, user, action, target, created_at", "журнал; user -> users.login");
        foreach (var t in Database.GetTables())
            dt.Rows.Add(t, string.Join(", ", Database.GetColumns(t).Select(c => c.Name)), "пользовательская");

        var grid = Style.MkGrid();
        var holder = new Panel { Dock = DockStyle.Fill };
        holder.Controls.Add(grid);
        content.Controls.Add(holder);
        Style.FillGrid(grid, dt);
        grid.Dock = DockStyle.Fill;
    }

    // пункт 4: разграничение прав
    void PageRoles()
    {
        Clear();
        var header = Header("Разграничение прав"); header.SetBounds(10, 10, 400, 24);
        content.Controls.Add(header);

        var dt = new DataTable();
        dt.Columns.Add("Роль"); dt.Columns.Add("Возможности");
        foreach (var (role, p) in Roles.Perms)
        {
            var allowed = string.Join(", ", p.Where(kv => kv.Value).Select(kv => kv.Key));
            dt.Rows.Add(role, allowed == "" ? "только просмотр" : allowed);
        }

        var grid = Style.MkGrid();
        var holder = new Panel { Dock = DockStyle.Top, Height = 100 };
        holder.Controls.Add(grid); grid.Dock = DockStyle.Fill;
        content.Controls.Add(holder);
        Style.FillGrid(grid, dt);

        var mine = Style.MkLabel($"Ваша роль: {user.Role}"); mine.ForeColor = Color.FromArgb(85, 85, 85);
        mine.SetBounds(10, 115, 200, 20); content.Controls.Add(mine);

        if (!perms["manage_users"]) return;

        txtULogin.SetBounds(10, 145, 150, 26); txtULogin.Text = "";
        txtUPass.SetBounds(170, 145, 150, 26); txtUPass.Text = "";
        cmbRole = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100, Font = new Font("Segoe UI", 9.75f) };
        cmbRole.Items.AddRange(new object[] { "admin", "user", "viewer" }); cmbRole.SelectedIndex = 2;
        cmbRole.SetBounds(330, 145, 100, 26);
        var btn = Style.MkButton("Добавить пользователя", (s, e) => DoAddUser(), 180); btn.SetBounds(440, 143, 180, 30);
        content.Controls.AddRange(new Control[] { txtULogin, txtUPass, cmbRole, btn });

        var grid2 = Style.MkGrid();
        var holder2 = new Panel { Dock = DockStyle.Fill };
        holder2.Controls.Add(grid2); grid2.Dock = DockStyle.Fill;
        content.Controls.Add(holder2);
        Style.FillGrid(grid2, Database.ListUsers());
    }

    void DoAddUser()
    {
        try
        {
            Database.AddUser(txtULogin.Text.Trim(), txtUPass.Text, cmbRole.Text);
            Database.LogAction(user.Login, "add_user", txtULogin.Text);
            PageRoles();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    // пункт 5: health checker + просмотр БД
    void PageHealth()
    {
        Clear();
        var header = Header("Health checker и просмотр БД"); header.SetBounds(10, 10, 500, 24);
        content.Controls.Add(header);

        var btn = Style.MkButton("Запустить проверку", (s, e) => DoHealth(), 180); btn.SetBounds(10, 40, 180, 30);
        content.Controls.Add(btn);

        gridHealth = Style.MkGrid();
        var holder = new Panel { Dock = DockStyle.Top, Height = 180 };
        holder.Controls.Add(gridHealth); gridHealth.Dock = DockStyle.Fill;
        content.Controls.Add(holder);

        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, BackColor = Style.Bg, WrapContents = false };
        var lbl = Style.MkLabel("Просмотр таблицы:");
        cmbView = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Font = new Font("Segoe UI", 9.75f) };
        cmbView.Items.AddRange(new[] { "users" }.Concat(Database.GetTables()).ToArray());
        if (cmbView.Items.Count > 0) cmbView.SelectedIndex = 0;
        var btnView = Style.MkButton("Показать", (s, e) => DoView(), 80);
        panel.Controls.AddRange(new Control[] { lbl, cmbView, btnView });
        panel.SetFlowBreak(lbl, false);
        foreach (Control c in panel.Controls) c.Margin = new Padding(4, 8, 4, 4);

        gridData = Style.MkGrid();
        var holder2 = new Panel { Dock = DockStyle.Fill };
        holder2.Controls.Add(gridData); gridData.Dock = DockStyle.Fill;
        content.Controls.Add(panel);
        content.Controls.Add(holder2);
    }

    void DoHealth()
    {
        var dt = new DataTable();
        dt.Columns.Add("Статус"); dt.Columns.Add("Проверка");
        foreach (var (s, m) in Database.HealthCheck()) dt.Rows.Add(s, m);
        Style.FillGrid(gridHealth, dt);
        Database.LogAction(user.Login, "health_check");
    }

    void DoView()
    {
        Style.FillGrid(gridData, Database.SelectRows(cmbView.Text));
    }

    // пункт 6: контроль данных
    void PageCrud()
    {
        Clear();
        var header = Header("Контроль данных (CRUD)"); header.SetBounds(10, 10, 400, 24);
        content.Controls.Add(header);

        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 40, BackColor = Style.Bg, WrapContents = false };
        var lbl = Style.MkLabel("Таблица:");
        cmbCrud = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Font = new Font("Segoe UI", 9.75f) };
        cmbCrud.Items.AddRange(Database.GetTables().ToArray());
        if (cmbCrud.Items.Count > 0) cmbCrud.SelectedIndex = 0;
        var btn = Style.MkButton("Открыть", (s, e) => ShowCrudTable(cmbCrud.Text), 80);
        panel.Controls.AddRange(new Control[] { lbl, cmbCrud, btn });
        foreach (Control c in panel.Controls) c.Margin = new Padding(4, 6, 4, 4);

        crudBtns = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45, BackColor = Style.Bg };
        gridCrud = Style.MkGrid();
        var holder = new Panel { Dock = DockStyle.Fill };
        holder.Controls.Add(gridCrud); gridCrud.Dock = DockStyle.Fill;

        content.Controls.Add(panel);
        content.Controls.Add(crudBtns);
        content.Controls.Add(holder);
        if (cmbCrud.Items.Count > 0) ShowCrudTable(cmbCrud.Text);
    }

    void ShowCrudTable(string table)
    {
        crudBtns.Controls.Clear();
        var cols = Database.GetColumns(table);
        string pk = cols[0].Name;

        // поля ввода для каждой колонки
        crudBtns.Controls.Add(Style.MkLabel("Колонки (заполните значения):"));
        foreach (var c in cols)
        {
            var tb = Style.MkTextBox(100); tb.Name = "col_" + c.Name;
            var lbl = Style.MkLabel(c.Name);
            crudBtns.Controls.Add(lbl); crudBtns.Controls.Add(tb);
        }
        foreach (Control c in crudBtns.Controls) c.Margin = new Padding(3, 8, 3, 8);

        if (perms["insert"])
        {
            var b = Style.MkButton("Добавить", (s, e) => CrudOp("insert", table, pk), 110);
            crudBtns.Controls.Add(b);
        }
        if (perms["update"])
        {
            var b = Style.MkButton("Изменить (по 1-й колонке)", (s, e) => CrudOp("update", table, pk), 170);
            crudBtns.Controls.Add(b);
        }
        if (perms["delete"])
        {
            var b = Style.MkButton("Удалить (по 1-й колонке)", (s, e) => CrudOp("delete", table, pk), 170);
            crudBtns.Controls.Add(b);
        }
        if (!perms["insert"] && !perms["update"] && !perms["delete"])
            crudBtns.Controls.Add(Style.MkLabel("Роль viewer: только просмотр"));
        Style.FillGrid(gridCrud, Database.SelectRows(table));
    }

    void CrudOp(string op, string table, string pk)
    {
        var values = new Dictionary<string, object>();
        foreach (Control c in crudBtns.Controls)
            if (c is TextBox tb && tb.Name.StartsWith("col_") && tb.Text != "")
                values[tb.Name[4..]] = tb.Text;
        if (values.Count == 0) { MessageBox.Show("Заполните хотя бы одно поле"); return; }
        try
        {
            if (op == "insert") Database.InsertRow(table, values);
            else if (op == "update" && values.ContainsKey(pk)) Database.UpdateRow(table, pk, values[pk], values);
            else if (op == "delete" && values.ContainsKey(pk)) Database.DeleteRow(table, pk, values[pk]);
            else { MessageBox.Show("Для изменения/удаления заполните 1-ю колонку (ID)"); return; }
            Database.LogAction(user.Login, op, table);
            ShowCrudTable(table);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    // пункт 7: система тестирования
    void PageTest()
    {
        Clear();
        var header = Header("Система тестирования БД"); header.SetBounds(10, 10, 400, 24);
        content.Controls.Add(header);

        var b1 = Style.MkButton("1. Проверить запуск системы", (s, e) => RunTest(true), 230); b1.SetBounds(10, 40, 230, 30);
        var b2 = Style.MkButton("2. Проверить валидность данных", (s, e) => RunTest(false), 230); b2.SetBounds(250, 40, 230, 30);
        content.Controls.AddRange(new Control[] { b1, b2 });

        var grid = Style.MkGrid();
        var holder = new Panel { Dock = DockStyle.Fill };
        holder.Controls.Add(grid); grid.Dock = DockStyle.Fill;
        content.Controls.Add(holder);
        gridTest = grid;
    }

    DataGridView? gridTest;

    void RunTest(bool startup)
    {
        var results = startup ? Database.HealthCheck() : Database.ValidateData();
        var dt = new DataTable();
        dt.Columns.Add("Результат"); dt.Columns.Add("Описание");
        foreach (var (s, m) in results) dt.Rows.Add(s == "OK" ? "ПРОЙДЕНО" : "ПРОВАЛ", m);
        Style.FillGrid(gridTest!, dt);
        Database.LogAction(user.Login, startup ? "test_startup" : "test_validate");
    }

    // журнал действий
    void PageAudit()
    {
        Clear();
        var header = Header("Журнал действий"); header.SetBounds(10, 10, 400, 24);
        content.Controls.Add(header);
        var grid = Style.MkGrid();
        var holder = new Panel { Dock = DockStyle.Fill };
        holder.Controls.Add(grid); grid.Dock = DockStyle.Fill;
        content.Controls.Add(holder);
        Style.FillGrid(grid, Database.GetAuditLog());
    }
}

static class Program
{
    [STAThread]
    static void Main()
    {
        Database.Init();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        var login = new LoginForm();
        if (login.ShowDialog() != DialogResult.OK || login.User == null) return;
        Application.Run(new MainForm(login.User));
    }
}
