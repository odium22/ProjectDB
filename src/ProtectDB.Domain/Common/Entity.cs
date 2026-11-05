namespace ProtectDB.Domain.Common;

// Базовая сущность: первичный ключ, общий для всех таблиц.
public abstract class Entity
{
    public int Id { get; set; }
}
