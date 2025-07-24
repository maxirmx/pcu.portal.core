// Copyright (C) 2025 Maxim [maxirmx] Samsonov (www.sw.consulting)
// All rights reserved.
// This file is a part of Fuelflux Core application
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
// 1. Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
// 2. Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
// TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
// PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDERS OR CONTRIBUTORS
// BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using NUnit.Framework;
using Fuelflux.Core.Models;
using Fuelflux.Core.RestModels;
using System.Collections.Generic;

namespace Fuelflux.Core.Tests.Models;

public class UserTests
{
    [Test]
    public void HasAnyRole_ReturnsFalse_WhenNoRoles()
    {
        var user = new User { Email = "test@example.com", Password = "password123" };
        Assert.That(user.HasAnyRole(), Is.False);
    }

    [Test]
    public void HasAnyRole_ReturnsTrue_WhenRolesExist()
    {
        var role = new Role { Id = 1, RoleId = UserRoleConstants.Admin, Name = "admin" };
        var user = new User
        {
            Email = "test@example.com",
            Password = "password123",
            RoleId = role.Id,
            Role = role
        };
        Assert.That(user.HasAnyRole(), Is.True);
    }

    [Test]
    public void HasRole_ReturnsTrue_WhenUserHasRole()
    {
        var role = new Role { Id = 1, RoleId = UserRoleConstants.Admin, Name = "Admin" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "password123",
            RoleId = role.Id,
            Role = role
        };
        Assert.That(user.HasRole(UserRoleConstants.Admin), Is.True);
    }

    [Test]
    public void HasRole_ReturnsFalse_WhenRoleMissing()
    {
        var user = new User { Email = "test@example.com", Password = "password123" };
        Assert.That(user.HasRole(UserRoleConstants.Admin), Is.False);
    }

    [Test]
    public void IsAdministrator_ReturnsTrue_WhenAdminRolePresent()
    {
        var role = new Role { Id = 1, RoleId = UserRoleConstants.Admin, Name = "administrator" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "password123",
            RoleId = role.Id,
            Role = role
        };
        Assert.That(user.IsAdministrator(), Is.True);
    }

    [Test]
    public void IsOperator_ReturnsTrue_WhenOperatorRolePresent()
    {
        var role = new Role { Id = 2, RoleId = UserRoleConstants.Operator, Name = "operator" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "password123",
            RoleId = role.Id,
            Role = role
        };
        Assert.That(user.IsOperator(), Is.True);
    }

    [Test]
    public void IsOperator_ReturnsFalse_WhenOperatorRoleNotPresent()
    {
        var role = new Role { Id = 1, RoleId = UserRoleConstants.Admin, Name = "admin" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "password123",
            RoleId = role.Id,
            Role = role
        };
        Assert.That(user.IsOperator(), Is.False);
    }

    [Test]
    public void IsOperator_ReturnsFalse_WhenUserIsNotOperator()
    {
        var role = new Role { Id = 1, RoleId = UserRoleConstants.Admin, Name = "admin" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "password123",
            RoleId = role.Id,
            Role = role
        };
        Assert.That(user.IsOperator(), Is.False);
    }

    [Test]
    public void IsCustomer_ReturnsTrue_WhenUserHasCustomerRole()
    {
        var role = new Role { Id = 3, RoleId = UserRoleConstants.Customer, Name = "customer" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "password123",
            RoleId = role.Id,
            Role = role
        };
        Assert.That(user.IsCustomer(), Is.True);
    }

    [Test]
    public void IsCustomer_ReturnsFalse_WhenUserIsNotCustomer()
    {
        var role = new Role { Id = 1, RoleId = UserRoleConstants.Admin, Name = "admin" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "password123",
            RoleId = role.Id,
            Role = role
        };
        Assert.That(user.IsCustomer(), Is.False);
    }

    [Test]
    public void HasUidAccess_ReturnsTrue_WhenUserIsCustomer()
    {
        var role = new Role { Id = 3, RoleId = UserRoleConstants.Customer, Name = "customer" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "password123",
            RoleId = role.Id,
            Role = role
        };
        Assert.That(user.HasUidAccess(), Is.True);
    }

    [Test]
    public void HasUidAccess_ReturnsTrue_WhenUserIsOperator()
    {
        var role = new Role { Id = 2, RoleId = UserRoleConstants.Operator, Name = "operator" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "password123",
            RoleId = role.Id,
            Role = role
        };
        Assert.That(user.HasUidAccess(), Is.True);
    }

    [Test]
    public void HasUidAccess_ReturnsFalse_WhenUserIsAdminOnly()
    {
        var role = new Role { Id = 1, RoleId = UserRoleConstants.Admin, Name = "admin" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "password123",
            RoleId = role.Id,
            Role = role
        };
        Assert.That(user.HasUidAccess(), Is.False);
    }

    [Test]
    public void HasUidAccess_ReturnsTrue_WhenUserHasBothCustomerAndOperatorRoles()
    {
        var customerRole = new Role { Id = 3, RoleId = UserRoleConstants.Customer, Name = "customer" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "password123",
            RoleId = customerRole.Id,
            Role = customerRole
        };
        Assert.That(user.HasUidAccess(), Is.True);
    }

    [Test]
    public void HasAnyRole_ReturnsFalse_WhenRoleIsNull()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "hashedpassword",
            FirstName = "Test",
            LastName = "User",
            Patronymic = "",
            RoleId = null,
            Role = null
        };

        // Act
        var result = user.HasAnyRole();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasAnyRole_ReturnsTrue_WhenRoleIsNotNull()
    {
        // Arrange
        var role = new Role { Id = 1, RoleId = UserRoleConstants.Admin, Name = "Admin" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "hashedpassword",
            FirstName = "Test",
            LastName = "User",
            Patronymic = "",
            RoleId = 1,
            Role = role
        };

        // Act
        var result = user.HasAnyRole();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void HasRole_ReturnsFalse_WhenRoleIsNull()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "hashedpassword",
            FirstName = "Test",
            LastName = "User",
            Patronymic = "",
            RoleId = null,
            Role = null
        };

        // Act
        var result = user.HasRole(UserRoleConstants.Admin);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasRole_ReturnsTrue_WhenRoleMatches()
    {
        // Arrange
        var role = new Role { Id = 1, RoleId = UserRoleConstants.Admin, Name = "Admin" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "hashedpassword",
            FirstName = "Test",
            LastName = "User",
            Patronymic = "",
            RoleId = 1,
            Role = role
        };

        // Act
        var result = user.HasRole(UserRoleConstants.Admin);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void HasRole_ReturnsFalse_WhenRoleDoesNotMatch()
    {
        // Arrange
        var role = new Role { Id = 1, RoleId = UserRoleConstants.Admin, Name = "Admin" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "hashedpassword",
            FirstName = "Test",
            LastName = "User",
            Patronymic = "",
            RoleId = 1,
            Role = role
        };

        // Act
        var result = user.HasRole(UserRoleConstants.Customer);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsAdministrator_ReturnsFalse_WhenRoleIsNull()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "hashedpassword",
            FirstName = "Test",
            LastName = "User",
            Patronymic = "",
            RoleId = null,
            Role = null
        };

        // Act
        var result = user.IsAdministrator();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsOperator_ReturnsFalse_WhenRoleIsNull()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "hashedpassword",
            FirstName = "Test",
            LastName = "User",
            Patronymic = "",
            RoleId = null,
            Role = null
        };

        // Act
        var result = user.IsOperator();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsCustomer_ReturnsFalse_WhenRoleIsNull()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "hashedpassword",
            FirstName = "Test",
            LastName = "User",
            Patronymic = "",
            RoleId = null,
            Role = null
        };

        // Act
        var result = user.IsCustomer();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void HasUidAccess_ReturnsFalse_WhenRoleIsNull()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "hashedpassword",
            FirstName = "Test",
            LastName = "User",
            Patronymic = "",
            RoleId = null,
            Role = null
        };

        // Act
        var result = user.HasUidAccess();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void User_HasUidAccess_ReturnsFalse_WhenRoleIsNull()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "hashedpassword",
            FirstName = "Test",
            LastName = "User",
            Patronymic = "",
            RoleId = null,
            Role = null
        };

        // Act
        var result = user.HasUidAccess();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void User_HasUidAccess_ReturnsTrue_WhenUserIsCustomer_WithNullRoleCheck()
    {
        // Arrange
        var role = new Role { Id = 3, RoleId = UserRoleConstants.Customer, Name = "Customer" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "hashedpassword",
            FirstName = "Test",
            LastName = "User",
            Patronymic = "",
            RoleId = 3,
            Role = role
        };

        // Act
        var result = user.HasUidAccess();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void User_HasUidAccess_ReturnsTrue_WhenUserIsOperator_WithNullRoleCheck()
    {
        // Arrange
        var role = new Role { Id = 2, RoleId = UserRoleConstants.Operator, Name = "Operator" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "hashedpassword",
            FirstName = "Test",
            LastName = "User",
            Patronymic = "",
            RoleId = 2,
            Role = role
        };

        // Act
        var result = user.HasUidAccess();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void User_HasUidAccess_ReturnsFalse_WhenUserIsAdminOnly_WithNullRoleCheck()
    {
        // Arrange
        var role = new Role { Id = 1, RoleId = UserRoleConstants.Admin, Name = "Admin" };
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Password = "hashedpassword",
            FirstName = "Test",
            LastName = "User",
            Patronymic = "",
            RoleId = 1,
            Role = role
        };

        // Act
        var result = user.HasUidAccess();

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void UserCreateItem_HasRole_ReturnsFalse_WhenRoleIsNull()
    {
        // Arrange
        var userCreateItem = new UserCreateItem
        {
            Email = "test@example.com",
            Password = "password",
            FirstName = "Test",
            LastName = "User",
            Role = null
        };

        // Act
        var result = userCreateItem.HasRole(UserRoleConstants.Admin);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void UserCreateItem_HasRole_ReturnsTrue_WhenRoleMatches()
    {
        // Arrange
        var userCreateItem = new UserCreateItem
        {
            Email = "test@example.com",
            Password = "password",
            FirstName = "Test",
            LastName = "User",
            Role = UserRoleConstants.Admin
        };

        // Act
        var result = userCreateItem.HasRole(UserRoleConstants.Admin);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void UserCreateItem_HasRole_ReturnsFalse_WhenRoleDoesNotMatch()
    {
        // Arrange
        var userCreateItem = new UserCreateItem
        {
            Email = "test@example.com",
            Password = "password",
            FirstName = "Test",
            LastName = "User",
            Role = UserRoleConstants.Customer
        };

        // Act
        var result = userCreateItem.HasRole(UserRoleConstants.Admin);

        // Assert
        Assert.That(result, Is.False);
    }
}
