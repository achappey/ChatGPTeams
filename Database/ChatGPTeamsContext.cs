using System.Collections.Generic;
using System.Threading.Tasks;
using achappey.ChatGPTeams.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace achappey.ChatGPTeams.Database;

public class ChatGPTeamsContext : DbContext
{
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Assistant> Assistants { get; set; }
    public DbSet<Department> Departments { get; set; }
    public DbSet<Function> Functions { get; set; }
    public DbSet<Resource> Resources { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Prompt> Prompts { get; set; }

    public ChatGPTeamsContext(DbContextOptions<ChatGPTeamsContext> options)
        : base(options)
    {

    }

    //  protected override void OnConfiguring(DbContextOptionsBuilder options)
    //     => options.UseSqlServer($"{DbPath}");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Assistant>()
        .HasMany(a => a.Functions)
        .WithMany(f => f.Assistants);

        modelBuilder.Entity<Assistant>()
      .HasMany(a => a.Resources)
      .WithMany(f => f.Assistants);

        modelBuilder.Entity<Conversation>()
       .HasMany(a => a.Resources)
       .WithMany(f => f.Conversations);

        modelBuilder.Entity<Conversation>()
       .HasMany(a => a.Functions)
       .WithMany(f => f.Conversations);

        modelBuilder.Entity<Prompt>()
       .HasMany(a => a.Functions)
       .WithMany(f => f.Prompts);

        /*        modelBuilder.Entity<AssistantFunction>().HasNoKey();
                modelBuilder.Entity<AssistantResource>().HasNoKey();
                modelBuilder.Entity<ConversationFunction>().HasNoKey();
                modelBuilder.Entity<ConversationResource>().HasNoKey();
                modelBuilder.Entity<PromptFunction>().HasNoKey();*/
    }

    public async Task AddAsync<T>(T entity) where T : class
    {
        Set<T>().Add(entity);
        await SaveChangesAsync();

    }

    public async Task<T> GetByIdAsync<T>(string id) where T : class
    {
        return await Set<T>().FindAsync(id);
    }

    public async Task UpdateAsync<T>(T entity) where T : class
    {
        Set<T>().Update(entity);
        await SaveChangesAsync();
    }

    public async Task DeleteAsync<T>(T entity) where T : class
    {
        Set<T>().Remove(entity);
        await SaveChangesAsync();
    }

    public async Task<List<T>> GetAllAsync<T>() where T : class
    {
        return await Set<T>().ToListAsync();
    }
}