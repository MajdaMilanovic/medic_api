using System.ComponentModel.DataAnnotations;

namespace test1.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public DateTime BirthDate { get; set; }
        public DateTime LastLoginDate { get; set; }
        public int Orders { get; set; }
        public string ImageUrl { get; set; }

    }
}
