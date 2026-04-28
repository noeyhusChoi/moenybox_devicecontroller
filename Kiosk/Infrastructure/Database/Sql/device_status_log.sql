DROP PROCEDURE IF EXISTS sp_insert_device_status_log;
DELIMITER //
CREATE PROCEDURE sp_insert_device_status_log(
    IN p_kiosk_id VARCHAR(64),
    IN p_device_name VARCHAR(64),
    IN p_device_type VARCHAR(64),
    IN p_source VARCHAR(32),
    IN p_code VARCHAR(128),
    IN p_severity VARCHAR(16),
    IN p_message VARCHAR(512),
    IN p_created_at DATETIME
)
BEGIN
    INSERT INTO device_status_log (
        kiosk_id,
        device_name,
        device_type,
        source,
        code,
        severity,
        message,
        created_at
    )
    VALUES (
        p_kiosk_id,
        p_device_name,
        p_device_type,
        p_source,
        p_code,
        p_severity,
        p_message,
        p_created_at
    );
END //
DELIMITER ;
