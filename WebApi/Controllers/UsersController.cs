﻿using AutoMapper;
using Core.Dtos;
using Core.Dtos.UserDtos;
using Core.Entities;
using Core.Interfaces.Services;
using Core.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[Route("api/users")]
[ApiController]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IPasswordService _passwordService;
    private readonly IPictureService _pictureService;
    private readonly IMapper _mapper;
    private readonly string _blobFolder;

    public UsersController(
        IUserService userService,
        IPasswordService passwordService,
        IPictureService pictureService,
        IMapper mapper)
    {
        _userService = userService;
        _passwordService = passwordService;
        _pictureService = pictureService;
        _mapper = mapper;
        _blobFolder = "user-profile-pictures";
    }

    [HttpGet]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult<PageDto<UserReadDto>>> GetAsync([FromQuery] PageParameters pageParams, CancellationToken token)
    {
        var users = await _userService.GetAllAsync(pageParams.PageNumber, pageParams.PageSize, token);
        var pageDto = _mapper.Map<PageDto<UserReadDto>>(users);

        return Ok(pageDto);
    }

    [HttpGet("{id:int:min(1)}")]
    public async Task<ActionResult<UserReadDto>> GetAsync([FromRoute] int id, CancellationToken token)
    {
        var user = await _userService.GetByIdAsync(id, token);
        var readDto = _mapper.Map<UserReadDto>(user);

        return Ok(readDto);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<UserReadDto>> RegisterAsync([FromForm] UserRegisterDto registerDto)
    {
        (byte[] hash, byte[] salt) = _passwordService.CreatePasswordHash(registerDto.Password!);
        string? pictureLink = await _pictureService.UploadAsync(registerDto.ProfilePicture, _blobFolder, registerDto.Email!);

        User user = _userService.ConstructUser(registerDto.Username!, registerDto.Email!, pictureLink, hash, salt);
        await _userService.CreateAsync(user);

        var readDto = _mapper.Map<UserReadDto>(user);

        return CreatedAtAction(nameof(GetAsync), new { Id = readDto.Id }, readDto);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<UserWithTokenReadDto>> LoginAsync([FromBody] UserLoginDto loginDto, CancellationToken token)
    {
        var user = await _userService.GetByEmailAsync(loginDto.Email!, token);

        _passwordService.VerifyPasswordHash(loginDto.Password!, user.PasswordHash!, user.PasswordSalt!);

        string jwtToken = _passwordService.CreateToken(user);
        var readDto = _mapper.Map<UserWithTokenReadDto>(user) with { Token = jwtToken };

        return Ok(readDto);
    }

    [HttpPut("{id:int:min(1)}")]
    public async Task<ActionResult> UpdateAsync([FromRoute] int id, [FromForm] UserUpdateDto updateDto)
    {
        var user = await _userService.GetByIdAsync(id);

        _userService.VerifyUserAccessRights(user);
        await _pictureService.DeleteAsync(user.ProfilePicture!);

        _mapper.Map(updateDto, user);
        user.ProfilePicture = await _pictureService.UploadAsync(updateDto.ProfilePicture, _blobFolder, user.Email!);

        await _userService.UpdateAsync(user);

        return NoContent();
    }

    [HttpPut("{id:int:min(1)}/changePassword")]
    public async Task<ActionResult<string>> ChangePasswordAsync(
        [FromRoute] int id,
        [FromBody] UserChangePasswordDto changePasswordDto)
    {
        var user = await _userService.GetByIdAsync(id);
        _userService.VerifyUserAccessRights(user);

        (byte[] hash, byte[] salt) = _passwordService.CreatePasswordHash(changePasswordDto.Password!);
        _userService.ChangePasswordData(user, hash, salt);
        await _userService.UpdateAsync(user);

        string token = _passwordService.CreateToken(user);

        return Ok(token);
    }

    [HttpPut("{id:int:min(1)}/changeRole")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<ActionResult> ChangeRoleAsync([FromRoute] int id, [FromBody] UserChangeRoleDto changeRoleDto)
    {
        var user = await _userService.GetByIdAsync(id);

        _mapper.Map(changeRoleDto, user);
        await _userService.UpdateAsync(user);

        return NoContent();
    }

    [HttpDelete("{id:int:min(1)}")]
    public async Task<ActionResult> DeleteAsync([FromRoute] int id)
    {
        var user = await _userService.GetByIdAsync(id);
        _userService.VerifyUserAccessRights(user);

        await _userService.DeleteAsync(user);
        await _pictureService.DeleteAsync(user.ProfilePicture!);

        return NoContent();
    }
}
