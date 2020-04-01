using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace netcore_postgres_oauth_boiler.Utilities
{
    public class Validator
    {
        public static bool validateEmail(string email)
        {
            if (!Regex.IsMatch(email, @"\S +@\S +\.\S +"))
            {
                return false;
            }

            return true;
        }

        public static bool validatePassword(string password)
        {
            if (password.Length <= 5 || password.Length > 100)
            {
                return false;
            }

            return true;
        }
    }
}
