-- Device command log table + insert procedure
-- Assumes MySQL 8.x

CREATE TABLE IF NOT EXISTS device_command_log (
    id BIGINT NOT NULL AUTO_INCREMENT,
    device_name VARCHAR(64) NOT NULL,
    command_name VARCHAR(64) NOT NULL,
    success TINYINT(1) NOT NULL,
    error_code VARCHAR(64) NULL,
    origin VARCHAR(16) NOT NULL,
    started_at DATETIME NOT NULL,
    finished_at DATETIME NOT NULL,
    duration_ms BIGINT NOT NULL,
    created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (id),
    INDEX idx_device_time (device_name, started_at),
    INDEX idx_command_time (command_name, started_at)
) ENGINE=InnoDB DEFAULT CHARSET=UTF8MB4 COLLATE=utf8mb4_0900_ai_ci;

DROP PROCEDURE IF EXISTS sp_insert_device_command_log;
DELIMITER //
CREATE PROCEDURE sp_insert_device_command_log(
    IN p_device_name VARCHAR(64),
    IN p_command VARCHAR(64),
    IN p_success INT,
    IN p_error_code VARCHAR(64),
    IN p_origin VARCHAR(16),
    IN p_started_at DATETIME,
    IN p_finished_at DATETIME,
    IN p_duration_ms BIGINT
)
BEGIN
    INSERT INTO device_command_log (
        device_name,
        command_name,
        success,
        error_code,
        origin,
        started_at,
        finished_at,
        duration_ms
    )
    VALUES (
        p_device_name,
        p_command,
        p_success,
        p_error_code,
        p_origin,
        p_started_at,
        p_finished_at,
        p_duration_ms
    );
END //
DELIMITER ;
