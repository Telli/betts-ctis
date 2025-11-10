-- Check what users exist in the database
SELECT Id, UserName, Email, FirstName, LastName, EmailConfirmed, IsActive 
FROM AspNetUsers;

-- Check user roles
SELECT u.Email, r.Name as Role
FROM AspNetUsers u
JOIN AspNetUserRoles ur ON u.Id = ur.UserId
JOIN AspNetRoles r ON ur.RoleId = r.Id;
