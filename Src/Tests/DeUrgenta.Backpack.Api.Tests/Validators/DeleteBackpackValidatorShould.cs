﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DeUrgenta.Backpack.Api.Commands;
using DeUrgenta.Backpack.Api.Validators;
using DeUrgenta.Common.Validation;
using DeUrgenta.Domain.Api;
using DeUrgenta.Domain.Api.Entities;
using DeUrgenta.I18n.Service.Models;
using DeUrgenta.Tests.Helpers;
using DeUrgenta.Tests.Helpers.Builders;
using FluentAssertions;
using Xunit;

namespace DeUrgenta.Backpack.Api.Tests.Validators
{
    [Collection(TestsConstants.DbCollectionName)]
    public class DeleteBackpackValidatorShould
    {
        private readonly DeUrgentaContext _dbContext;

        public DeleteBackpackValidatorShould(DatabaseFixture fixture)
        {
            _dbContext = fixture.Context;
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("my-weird-sub")]
        public async Task Invalidate_request_when_no_user_found_by_sub(string sub)
        {
            // Arrange
            var sut = new DeleteBackpackValidator(_dbContext);

            // Act
            var result = await sut.IsValidAsync(new DeleteBackpack(sub, Guid.NewGuid()));

            // Assert
            result.Should().BeOfType<GenericValidationError>();
        }

        [Fact]
        public async Task Invalidate_request_when_backpack_does_not_exists()
        {
            // Arrange
            var userSub = Guid.NewGuid().ToString();
            var user = new UserBuilder().WithSub(userSub).Build();

            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            var sut = new DeleteBackpackValidator(_dbContext);

            // Act
            var result = await sut.IsValidAsync(new DeleteBackpack(userSub, Guid.NewGuid()));

            // Assert
            result.Should().BeOfType<GenericValidationError>();
        }

        [Fact]
        public async Task Invalidate_request_when_user_is_not_owner_of_backpack()
        {
            // Arrange
            var userSub = Guid.NewGuid().ToString();
            var ownerSub = Guid.NewGuid().ToString();

            var user = new UserBuilder().WithSub(userSub).Build();
            var owner = new UserBuilder().WithSub(ownerSub).Build();

            var backpack = new Domain.Api.Entities.Backpack
            {
                Name = "A backpack"
            };

            await _dbContext.Users.AddAsync(user);
            await _dbContext.Users.AddAsync(owner);
            await _dbContext.Backpacks.AddAsync(backpack);
            await _dbContext.BackpacksToUsers.AddAsync(new BackpackToUser { Backpack = backpack, User = owner, IsOwner = true });
            await _dbContext.BackpacksToUsers.AddAsync(new BackpackToUser { Backpack = backpack, User = user, IsOwner = false });
            await _dbContext.SaveChangesAsync();

            var sut = new DeleteBackpackValidator(_dbContext);

            // Act
            var result = await sut.IsValidAsync(new DeleteBackpack(userSub, backpack.Id));

            // Assert
            result
                .Should()
                .BeOfType<LocalizableValidationError>()
                .Which.Messages
                .Should()
                .BeEquivalentTo(new Dictionary<LocalizableString, LocalizableString>
                {
                    { "not-backpack-owner", "not-backpack-owner-delete-message" }
                });
        }

        [Fact]
        public async Task Validate_when_user_is_owner_of_requested_backpack()
        {
            // Arrange
            var sut = new DeleteBackpackValidator(_dbContext);

            var userSub = Guid.NewGuid().ToString();
            var user = new UserBuilder().WithSub(userSub).Build();

            var backpack = new Domain.Api.Entities.Backpack
            {
                Name = "my backpack"
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.Backpacks.AddAsync(backpack);
            await _dbContext.BackpacksToUsers.AddAsync(new BackpackToUser { Backpack = backpack, User = user, IsOwner = true });
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await sut.IsValidAsync(new DeleteBackpack(userSub, backpack.Id));

            // Assert
            result.Should().BeOfType<ValidationPassed>();
        }
    }
}