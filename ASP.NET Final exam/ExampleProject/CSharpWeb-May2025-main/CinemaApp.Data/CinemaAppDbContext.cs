﻿namespace CinemaApp.Data
{
    using System.Reflection;

    using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore;

    using Models;

    public class CinemaAppDbContext : IdentityDbContext<ApplicationUser>
    {
        public CinemaAppDbContext(DbContextOptions<CinemaAppDbContext> options)
            : base(options)
        {

        }

        public virtual DbSet<Movie> Movies { get; set; } = null!;

        public virtual DbSet<ApplicationUserMovie> ApplicationUserMovies { get; set; } = null!;

        public virtual DbSet<Cinema> Cinemas { get; set; } = null!;

        public virtual DbSet<CinemaMovie> CinemasMovies { get; set; } = null!;

        public virtual DbSet<Ticket> Tickets { get; set; } = null!;

        public virtual DbSet<Manager> Managers { get; set; } = null!;

        public virtual DbSet<ApplicationUser> ApplicationUsers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
