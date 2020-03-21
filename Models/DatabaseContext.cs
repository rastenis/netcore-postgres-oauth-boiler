using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BCrypt;

namespace netcore_postgres_oauth_boiler.Models
{
	 public class DatabaseContext : DbContext
	 {
		  public DbSet<User> Users { get; set; }
		  public DbSet<Credential> Credentials { get; set; }
	 }

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
		  public string id { get; set; }
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
