-- Staff passwords must be stored as PBKDF2 hashes (prefix PBKDF2:).
--
-- This project hashes plain-text staff.password values automatically on application
-- startup (StaffPasswordMigration.EnsureApplied). Launch FurnitureERP once after
-- importing seed data if passwords are still plain text.
--
-- Login still uses username/password in the UI; only the database column is hashed.
--
-- Check for rows that still need migration:
SELECT staffID, username,
       CASE
           WHEN password LIKE 'PBKDF2:%' THEN 'hashed'
           ELSE 'plain — run app once to migrate'
       END AS password_storage
FROM staff
WHERE status = 1 OR username = 'admin'
ORDER BY staffID;
