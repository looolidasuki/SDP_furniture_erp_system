-- Optional: ensure salesorder.status never relies on a NULL user variable fallback.
-- Safe to run multiple times on MariaDB/MySQL 8+.
ALTER TABLE salesorder
  MODIFY COLUMN status INT(10) NOT NULL DEFAULT 0
  COMMENT '状态机控制：草稿、已锁定、生产中、发货完成等';
