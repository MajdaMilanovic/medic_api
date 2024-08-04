using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.RegularExpressions;
using test1.Context;
using test1.Helpers;
using test1.Models;
using System;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

namespace test1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly AppDbContext _db;

        public UserController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        [Route("/authenticate")]
        public async Task<IActionResult> Authenticate([FromBody] User userObj)
        {
            if (userObj == null)
                return BadRequest();

            var user = await _db.User.FirstOrDefaultAsync(x => x.UserName == userObj.UserName);
            if (user == null)
                return NotFound(new { Message = "User Not Found!" });

            if(!PasswordHash.VerifyPassword(userObj.Password, user.Password))
            {
                return BadRequest(new { Message = "Password is incorrect!" });
            }

            user.Token = CreateJwt(user);

            return Ok(new
            {
                Token =user.Token,
                Message = "Login Sucess!"
            });
        }

        [HttpPost]
        [Route("/register")]
        public async Task<IActionResult> RegisterUser([FromBody] User userObj)
        {
            if (userObj == null)
                return BadRequest();


            //Check username
            if(await CheckIfUsernameExistsAsync(userObj.UserName))
                return BadRequest(new {Message="Username already exists!"});



            //Check email
            if (await CheckIfEmailExistsAsync(userObj.Email))
                return BadRequest(new { Message = "Email already exists!" });


            //Check password strength

            var pass = CheckPasswordStrength(userObj.Password);
            if (!string.IsNullOrEmpty(pass))
                return BadRequest(new { Message = pass.ToString() });



            userObj.Password = PasswordHash.HashPassword(userObj.Password);
            userObj.Role = "User";
            userObj.Token = "";
            await _db.User.AddAsync(userObj);
            await _db.SaveChangesAsync();
            return Ok(new
            {
                Message = "User registered!"
            });



        }
        private async Task<bool> CheckIfUsernameExistsAsync(string username)
        {
            return await _db.User.AnyAsync(x => x.UserName == username);
        }
        private async Task<bool> CheckIfEmailExistsAsync(string email)
        {
            return await _db.User.AnyAsync(x => x.Email == email);
        }
        private string CheckPasswordStrength(string password)
        {
            StringBuilder sb = new StringBuilder();
            if(password.Length < 8)
                sb.Append("Minimum password length should be 8 characters" + Environment.NewLine);
            if (!(Regex.IsMatch(password, "[a-z]") && Regex.IsMatch(password, "[A-Z]") && Regex.IsMatch(password, "[0-9]")))
                sb.Append("Password should be Alphanumeric" + Environment.NewLine);
            if (!(Regex.IsMatch(password, "[<,>,@,*,/,&,#,{,},[,,$,-,_,+,=,?]")))
                sb.Append("Password should contain special chars" + Environment.NewLine);
            return sb.ToString();
        }
        private string CreateJwt(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("veryverysecret.....");
            var identity = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(ClaimTypes.Name, $"{user.FirstName} {user.LastName}")
            });

            var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = identity,
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = credentials
            };
            var token = jwtTokenHandler.CreateToken(tokenDescriptor);

            return jwtTokenHandler.WriteToken(token);
        }
        [HttpDelete]
        [Route("/block/{id}")]
        public async Task<IActionResult> BlockUser(int id)
        {
            var user = await _db.User.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            _db.User.Remove(user);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDtop userUpdateDto)
        {
            if (id != userUpdateDto.Id)
            {
                return BadRequest();
            }

            var user = await _db.User.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Update the user properties here
            user.FirstName = userUpdateDto.Name;
            user.UserName = userUpdateDto.Username;
            user.Orders = userUpdateDto.Orders;
            user.Role = userUpdateDto.Status;
            user.BirthDate = userUpdateDto.DateOfBirth;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _db.User.Any(e => e.Id == id);
        }


        [HttpGet("id")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _db.User.FindAsync(id);
            return user == null ? NotFound() : Ok(user);
        }
       

        [HttpGet("details/{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _db.User.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return user;
        }

        [Authorize]
        [HttpGet]
        [Route("/GetAllUsers")]
        public async Task<ActionResult<User>> GetAllUsers()
        {
            return Ok(await _db.User.ToListAsync());
        }

    }
}
