using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace netcore_postgres_oauth_boiler.Models
{
	 public class DatabaseContext : DbContext
	 {
		  public DbSet<User> Users { get; set; }
		  public DbSet<Credential> Credentials { get; set; }
	 }

    public class User
    {
        public User(string email, string password)
        {
            // TODO: hash password
            // TODO: generate id
            this.email = email;
            this.password = password;
        }
        public int id { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public List<Credential> credentials { get; set; }
    }

    public class Credential
    {
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
