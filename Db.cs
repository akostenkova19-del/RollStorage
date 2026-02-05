using Microsoft.EntityFrameworkCore;

namespace RollStorage;

public class Db(DbContextOptions<Db> options) : DbContext(options)
{
    public DbSet<Roll> Rolls { get; set; }
}