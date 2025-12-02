using AI_SP.Data;
using AI_SP.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using static AI_SP.DTOs.AuthDtos;

namespace AI_SP.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest("Email already exists.");

            var user = new User
            {
                Name = dto.Name,
                Email = dto.Email,
                Password = dto.Password
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            return Ok();
        }


        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (user == null || user.Password != dto.Password)
                return Unauthorized("Invalid credentials.");

            var token = GenerateJwt(user);

            return Ok(new
            {
                token,
                user = new { user.UserId, user.Name, user.Email }
            });
        }


        private string GenerateJwt(User user)
        {
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

            var claims = new[]
            {
                new Claim("id", user.UserId.ToString()),
                new Claim("email", user.Email),
                new Claim("name", user.Name)
            };

            var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


    }
}
