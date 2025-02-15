using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace Data.Context
{
    public class CoreAPIContext : IdentityDbContext<IdentityUser>
    {
        public CoreAPIContext(DbContextOptions<CoreAPIContext> options) : base(options)
        {
        }

        // public DbSet<Model> Model { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // here you can set the relationships between the entities with more accuracy
            // modelBuilder.Entity<Model>()
            //     .HasOne(c => c.OtherModel)
            //     .WithMany(b => b.ModelItem)
            //     .HasForeignKey(c => c.OtherModelId)
            //     .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
