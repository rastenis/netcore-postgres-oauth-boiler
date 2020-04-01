using System;
using System.Net.Mail;

namespace netcore_postgres_oauth_boiler.Utilities
{
    public class Validator
    {
        public static bool validateEmail(string email)
        {
            try
            {
                MailAddress m = new MailAddress(email);

                return true;
            }
            catch (FormatException)
            {
                return false;
            }
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
