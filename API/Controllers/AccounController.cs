using API.Data;
using API.DTOs;
using API.Entities;
using API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private DataContext context;
        private readonly TokenService tokenService;

        public AccountController(DataContext context, TokenService tokenService)
        {
            this.context = context;
            this.tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await IsUserExist(registerDto.UserName)) return BadRequest("User Name is taken");

            using var hmac = new HMACSHA512();
            AppUser user = new AppUser
            {
                UserName = registerDto.UserName.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };

            this.context.Users.Add(user);
            await this.context.SaveChangesAsync();
            return new UserDto
            {
                UserName = user.UserName,
                Token = this.tokenService.CreateToken(user)
            };
        }

        private async Task<bool> IsUserExist(string userName)
        {
            return await this.context.Users.AnyAsync(user => user.UserName == userName.ToLower());
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> login(LoginDto loginDto)
        {
            var user = await this.context.Users.SingleOrDefaultAsync(x => x.UserName == loginDto.UserName.ToLower());
            if (user == null) return Unauthorized("Invalid user name");
            using var hmac = new HMACSHA512(user.PasswordSalt);
            var hachedPassword = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));
            for(int i=0; i<hachedPassword.Length; i++)
            {
                if (hachedPassword[i] != user.PasswordHash[i]) return Unauthorized("Invalid Password");
            }
            return new UserDto 
            { 
                UserName = user.UserName,
                Token = this.tokenService.CreateToken(user)
            };
        }

    }
   
}
