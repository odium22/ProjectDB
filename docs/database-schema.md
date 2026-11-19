# Схема базы данных ProtectDB

Предметная область: учёт доступа и аудит действий над защищаемыми ресурсами.
Модель доступа — RBAC (пользователь получает роли, роль получает права).

## Таблицы

### Users
Учётные записи.

| Поле                | Тип      | Примечание                                   |
|---------------------|----------|----------------------------------------------|
| Id                  | int PK   | первичный ключ                               |
| UserName            | text     | логин, **уникальный**                        |
| DisplayName         | text     | отображаемое имя                             |
| Email               | text?    | почта                                        |
| PasswordHash        | text     | PBKDF2-хэш пароля                            |
| PasswordSalt        | text     | соль для хэша                                |
| IsActive            | bool     | активна ли учётная запись                    |
| IsLocked            | bool     | блокировка после неудачных входов            |
| FailedLoginAttempts | int      | счётчик неудачных входов                     |
| LastLoginAtUtc      | datetime?| время последнего входа                       |
| CreatedAtUtc        | datetime | время создания                               |

### Roles
Роли.

| Поле        | Тип     | Примечание                    |
|-------------|---------|-------------------------------|
| Id          | int PK  | первичный ключ                |
| Name        | text    | имя, **уникальное**           |
| Description | text?   | описание                      |
| IsSystem    | bool    | системную роль нельзя удалить |

### Permissions
Права (операции).

| Поле        | Тип     | Примечание                             |
|-------------|---------|----------------------------------------|
| Id          | int PK  | первичный ключ                         |
| Code        | text    | код вида `users.create`, **уникальный**|
| Name        | text    | отображаемое имя                       |
| Description | text?   | описание                               |
| ResourceId  | int? FK | ссылка на `Resources` (необязательна)  |

### Resources
Защищаемые ресурсы (разделы системы).

| Поле        | Тип     | Примечание                        |
|-------------|---------|-----------------------------------|
| Id          | int PK  | первичный ключ                    |
| Code        | text    | код (users, roles, ...), уникальный|
| Name        | text    | отображаемое имя                  |
| Description | text?   | описание                          |
| IsProtected | bool    | требует проверки прав             |

### UserRoles
Связь «пользователь — роль», составной ключ `(UserId, RoleId)`.

| Поле         | Тип      | Примечание          |
|--------------|----------|---------------------|
| UserId       | int FK   | → `Users` (CASCADE) |
| RoleId       | int FK   | → `Roles` (CASCADE) |
| AssignedAtUtc| datetime | когда выдана роль   |

### RolePermissions
Связь «роль — право», составной ключ `(RoleId, PermissionId)`.

| Поле         | Тип      | Примечание              |
|--------------|----------|-------------------------|
| RoleId       | int FK   | → `Roles` (CASCADE)     |
| PermissionId | int FK   | → `Permissions` (CASCADE)|
| GrantedAtUtc | datetime | когда выдано право      |

### Sessions
Сессии входа.

| Поле          | Тип       | Примечание                    |
|---------------|-----------|-------------------------------|
| Id            | int PK    | первичный ключ                |
| UserId        | int FK    | → `Users` (CASCADE)           |
| Token         | text      | токен сессии, **уникальный**  |
| IpAddress     | text?     | адрес входа                   |
| CreatedAtUtc  | datetime  | начало сессии                 |
| ExpiresAtUtc  | datetime  | плановое окончание            |
| RevokedAtUtc  | datetime? | досрочный отзыв               |

### AuditEntries
Журнал аудита.

| Поле         | Тип      | Примечание                                  |
|--------------|----------|---------------------------------------------|
| Id           | int PK   | первичный ключ                              |
| TimestampUtc | datetime | время события (индексируется)               |
| UserId       | int? FK  | → `Users` (SET NULL, история сохраняется)   |
| UserName     | text     | снимок логина на момент события             |
| Action       | text     | действие (`AuditAction` строкой)            |
| Result       | text     | результат (`AuditResult` строкой)           |
| EntityType   | text?    | имя сущности                                |
| EntityId     | text?    | идентификатор сущности                      |
| Details      | text?    | подробности                                 |
| IpAddress    | text?    | сетевой адрес                               |

## Связи

```
Users ──< UserRoles >── Roles ──< RolePermissions >── Permissions ──> Resources
  │                                              (ResourceId, SET NULL)
  └──< Sessions (CASCADE)
  └──< AuditEntries (SET NULL)
```

## Правила целостности

- `Users.UserName`, `Roles.Name`, `Permissions.Code`, `Resources.Code`,
  `Sessions.Token` — уникальные индексы;
- при удалении пользователя его сессии и назначения ролей удаляются (CASCADE);
- при удалении роли удаляются её связи (CASCADE);
- при удалении пользователя записи аудита сохраняются, ссылка на автора
  обнуляется (SET NULL);
- при удалении ресурса ссылка у прав обнуляется (SET NULL).
