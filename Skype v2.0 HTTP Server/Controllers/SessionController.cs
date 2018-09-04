﻿namespace HttpServer.Controllers
{
    using System;
    using System.Threading.Tasks;

    using HttpServer.Database;
    using HttpServer.Services.Interfaces;

    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    using Shared.Models;

    [AllowAnonymous]
    [ApiController]
    [Route("[Controller]")]
    public class SessionController : ControllerBase
    {
        private readonly IAuthService _authService;

        private readonly Skype2Context _databaseContext;

        private readonly IHashService _hashService;

        public SessionController(IAuthService authService, Skype2Context databaseContext, IHashService hashService)
        {
            _authService = authService;
            _databaseContext = databaseContext;
            _hashService = hashService;
        }

        [HttpPost("login")]
        public IActionResult RequestToken([FromBody] UserCredentials userCredentials)
        {
            if (_authService.Authorize(userCredentials.Username, userCredentials.Password, out string token))
            {
                return Ok(token);
            }

            return BadRequest("Invalid credentials.");
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserCredentials userCredentials)
        {
            byte[] salt = _hashService.GenerateSalt();

            await _databaseContext.Users.AddAsync(new User
            {
                CreatedAt = DateTime.Now,
                Name = userCredentials.Username,
                Password = _hashService.HashPassword(userCredentials.Password, salt),
                Salt = Convert.ToBase64String(salt)
            });
            await _databaseContext.SaveChangesAsync();

            if (_authService.Authorize(userCredentials.Username, userCredentials.Password, out string token))
            {
                return Ok(token);
            }

            return Unauthorized();
        }

        [Authorize]
        [HttpGet("logout")]
        public IActionResult Logout([FromHeader] string authorization)
        {
            _authService.DeAuthorize(authorization);

            return Ok();
        }
    }
}