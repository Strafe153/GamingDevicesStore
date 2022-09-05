﻿using Core.Entities;
using Core.Enums;
using Core.Exceptions;
using Core.Interfaces.Repositories;
using Core.Interfaces.Services;
using Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Drawing;
using System.Security.Claims;
using System.Security.Principal;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _repository;
        private readonly IPictureService _pictureService;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IUserRepository repository,
            IPictureService pictureService,
            ILogger<UserService> logger)
        {
            _repository = repository;
            _pictureService = pictureService;
            _logger = logger;
        }

        public async Task CreateAsync(User entity)
        {
            try
            {
                _repository.Create(entity);
                await _repository.SaveChangesAsync();

                _logger.LogInformation("Succesfully registered a user");
            } 
            catch (DbUpdateException)
            {
                _logger.LogWarning($"Failed to register a user. The username '{entity.Username}' is already taken");
                throw new UsernameNotUniqueException($"Username '{entity.Username}' is already taken");
            }
        }

        public async Task DeleteAsync(User entity)
        {
            _repository.Delete(entity);
            await _repository.SaveChangesAsync();
            await _pictureService.DeleteAsync(entity.ProfilePicture!);

            _logger.LogInformation($"Succesfully deleted a user with id {entity.Id}");
        }

        public async Task<PaginatedList<User>> GetAllAsync(int pageNumber, int pageSize)
        {
            var users = await _repository.GetAllAsync(pageNumber, pageSize);
            _logger.LogInformation("Successfully retrieved all users");

            return users;
        }

        public async Task<User> GetByIdAsync(int id)
        {
            var user = await _repository.GetByIdAsync(id);

            if (user is null)
            {
                _logger.LogWarning($"Failed to retrieve a user with id {id}");
                throw new NullReferenceException($"User with id {id} not found");
            }

            _logger.LogInformation($"Successfully retrieved a user with id {id}");

            return user;
        }

        public async Task<User> GetByNameAsync(string name)
        {
            var user = await _repository.GetByNameAsync(name);

            if (user is null)
            {
                _logger.LogWarning($"Failed to retrieve a user with username {name}");
                throw new NullReferenceException($"User with username {name} not found");
            }

            _logger.LogInformation($"Successfully retrieved a user with username {name}");

            return user;
        }

        public async Task UpdateAsync(User entity)
        {
            _repository.Update(entity);
            await _repository.SaveChangesAsync();

            _logger.LogInformation($"Successfully updated a user with id {entity.Id}");
        }

        public async Task<User> ConstructUserAsync(string username, byte[] passwordHash, byte[] passwordSalt, string? profilePicture)
        {
            User user = new()
            {
                Username = username,
                Role = UserRole.User,
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt
            };

            user.ProfilePicture = await GetProfilePicture(profilePicture, user.Username);

            return user;
        }

        public void ChangePasswordData(User user, byte[] passwordHash, byte[] passwordSalt)
        {
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
        }

        public void VerifyUserAccessRights(User performedOn, IIdentity performer, IEnumerable<Claim> claims)
        {
            if (performedOn.Username != performer.Name
                && !claims.Any(c => c.Value == UserRole.Admin.ToString()))
            {
                _logger.LogWarning($"User '{performer.Name}' failed to perform an operation due to insufficient access rights");
                throw new NotEnoughRightsException("Not enough rights to perform the operation");
            }
        }

        private async Task<string?> GetProfilePicture(string? profilePicture, string username)
        {
            byte[]? bytes;

            if (profilePicture is not null)
            {
                bytes = await File.ReadAllBytesAsync(profilePicture);
            }
            else
            {
                string defaultProfilePicPath = "../Application/Assets/Images/default_profile_pic.jpg";
                bytes = await File.ReadAllBytesAsync(defaultProfilePicPath);
            }

            using (MemoryStream ms = new(bytes))
            {
                var image = Image.FromStream(ms);
                var profilePictureLink = await _pictureService.UploadAsync(image, "userProfilePictures", username, "jpg");

                return profilePictureLink;
            }
        }
    }
}
