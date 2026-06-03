-- Promote default admin account to Super User (full permissions + System Admin menu).
UPDATE staff
SET title = 'Super User',
    department = 'Super User'
WHERE username = 'admin';
