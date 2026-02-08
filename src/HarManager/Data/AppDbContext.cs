using Microsoft.EntityFrameworkCore;
using HarManager.Models;
using System;
using System.IO;

namespace HarManager.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Project> Projects { get; set; }
        public DbSet<HarEntry> HarEntries { get; set; }

        public string DbPath { get; }

        public AppDbContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = System.IO.Path.Join(path, "HarManager_dev_v3.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options
                .UseLazyLoadingProxies()
                .UseSqlite($"Data Source={DbPath}");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Project>()
                .HasMany(p => p.Entries)
                .WithOne(e => e.Project)
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HarEntry>(entity =>
            {
                entity.OwnsOne(e => e.Request, req =>
                {
                    req.ToJson();
                    req.OwnsMany(r => r.Headers);
                    req.OwnsMany(r => r.Cookies);
                    req.OwnsMany(r => r.QueryString);
                    req.OwnsOne(r => r.PostData, pd =>
                    {
                        pd.OwnsMany(p => p.Params);
                    });
                });

                entity.OwnsOne(e => e.Response, res =>
                {
                    res.ToJson();
                    res.OwnsMany(r => r.Headers);
                    res.OwnsMany(r => r.Cookies);
                    res.OwnsOne(r => r.Content);
                });
            });
        }
    }
}

