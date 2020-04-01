using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace netcore_postgres_oauth_boiler.Models
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options)
   : base(options)
        {
            this.Database.EnsureCreated();
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Credential> Credentials { get; set; }
    }

    [Table("users")]
    public class User
    {
        public User(string email, string password, Credential credential)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Email = email;
            this.Password = BCrypt.Net.BCrypt.HashPassword(password);
            this.Credentials = new List<Credential>();
            this.Credentials.Add(credential);
        }

        public User(string email, string password)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Email = email;
            this.Password = BCrypt.Net.BCrypt.HashPassword(password);
        }

        public User()
        {
            Credentials = new List<Credential>();
        }

        public string Id { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public List<Credential> Credentials { get; set; }
    }

    public class Credential
    {
        public Credential(AuthProvider provider, string token)
        {
            this.Id = Guid.NewGuid().ToString();
            this.Token = token;
            this.Provider = provider;
        }
        public string Id { get; set; }
        public string Token { get; set; }
        public AuthProvider Provider { get; set; }
    }

    public enum AuthProvider
    {
        GOOGLE,
        REDDIT,
        GITHUB,
    }
}
