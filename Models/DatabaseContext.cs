using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace netcore_postgres_oauth_boiler.Models
{
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options)
   : base(options)
        {

            try
            {
                var databaseCreator = (Database.GetService<IDatabaseCreator>() as RelationalDatabaseCreator);
                databaseCreator.CreateTables();
            }
            catch (Exception e)
            {
                // Ignoring exception if tables already exist.
            }
        }


        public DbSet<User> Users { get; set; }
        public DbSet<Credential> Credentials { get; set; }
    }

    [Table("users")]
    public class User
    {
        public User(string email, string password, Credential credential)
        {
            this.id = Guid.NewGuid().ToString();
            this.email = email;
            this.password = BCrypt.Net.BCrypt.HashPassword(password);
            this.credentials = new List<Credential>();
            this.credentials.Add(credential);
        }

        public User(string email, string password)
        {
            this.id = Guid.NewGuid().ToString();
            this.email = email;
            this.password = BCrypt.Net.BCrypt.HashPassword(password);
        }
        public string id { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public List<Credential> credentials { get; set; }
    }

    [Table("credentials")]
    public class Credential
    {
        public string id { get; set; }
        public AuthProvider provider { get; set; }
        public string token { get; set; }
    }

    public enum AuthProvider
    {
        GOOGLE,
        TWITTER,
        GITHUB,
    }
}
