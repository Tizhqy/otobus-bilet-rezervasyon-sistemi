-- ================================================================
-- BUS TICKET RESERVATION SYSTEM - DATABASE SCHEMA
-- ================================================================
--
-- IMPROVEMENTS APPLIED:
-- 1. Added UNIQUE constraint on stations(name, city) - prevents duplicate stations
-- 2. Added UNIQUE constraint on route_stations(route_id, stop_order) - prevents duplicate stop orders
-- 3. Added UNIQUE constraint on passengers(seat_id) - prevents double-booking at DB level
--
-- NORMALIZATION: 1NF, 2NF, 3NF satisfied
--
-- ================================================================

-- 1. ROLES
CREATE TABLE roles (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(50) NOT NULL UNIQUE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 2. USERS
CREATE TABLE users (
    id INT PRIMARY KEY AUTO_INCREMENT,
    role_id INT NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    phone VARCHAR(20),
    is_active TINYINT(1) DEFAULT 1,
    remember_token VARCHAR(255),
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (role_id) REFERENCES roles(id)
);

-- 3. STATIONS
CREATE TABLE stations (
    id INT PRIMARY KEY AUTO_INCREMENT,
    name VARCHAR(150) NOT NULL,
    city VARCHAR(100) NOT NULL,
    address TEXT,
    is_active TINYINT(1) DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE KEY uniq_station_name_city (name, city)
);

-- 4. ROUTES
CREATE TABLE routes (
    id INT PRIMARY KEY AUTO_INCREMENT,
    origin_station_id INT NOT NULL,
    destination_station_id INT NOT NULL,
    distance_km INT,
    duration_minutes INT,
    is_active TINYINT(1) DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (origin_station_id) REFERENCES stations(id),
    FOREIGN KEY (destination_station_id) REFERENCES stations(id)
);

-- 5. ROUTE_STATIONS (ara duraklar)
CREATE TABLE route_stations (
    id INT PRIMARY KEY AUTO_INCREMENT,
    route_id INT NOT NULL,
    station_id INT NOT NULL,
    stop_order INT NOT NULL,
    FOREIGN KEY (route_id) REFERENCES routes(id),
    FOREIGN KEY (station_id) REFERENCES stations(id),
    UNIQUE KEY uniq_route_stop_order (route_id, stop_order)
);

-- 6. BUSES
CREATE TABLE buses (
    id INT PRIMARY KEY AUTO_INCREMENT,
    plate_number VARCHAR(20) NOT NULL UNIQUE,
    capacity INT NOT NULL,
    type VARCHAR(50),
    is_active TINYINT(1) DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- 7. DEPARTURES
CREATE TABLE departures (
    id INT PRIMARY KEY AUTO_INCREMENT,
    route_id INT NOT NULL,
    bus_id INT NOT NULL,
    departure_time DATETIME NOT NULL,
    arrival_time DATETIME NOT NULL,
    price DECIMAL(10,2) NOT NULL,
    is_active TINYINT(1) DEFAULT 1,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (route_id) REFERENCES routes(id),
    FOREIGN KEY (bus_id) REFERENCES buses(id)
);

-- 8. SEATS
CREATE TABLE seats (
    id INT PRIMARY KEY AUTO_INCREMENT,
    departure_id INT NOT NULL,
    seat_number VARCHAR(10) NOT NULL,
    status ENUM('available','booked','reserved') DEFAULT 'available',
    FOREIGN KEY (departure_id) REFERENCES departures(id),
    UNIQUE KEY uniq_departure_seat (departure_id, seat_number)
);

-- ================================================================
-- TRIGGER: Auto-create seats after departure insert
-- ================================================================
-- When a new departure is added, create seats up to bus capacity.
-- All seats are initialized with 'available' status.
-- ================================================================
DELIMITER $$

CREATE TRIGGER trg_create_seats_after_departure_insert
AFTER INSERT ON departures
FOR EACH ROW
BEGIN
    DECLARE v_capacity INT DEFAULT 0;
    DECLARE v_seat_no INT DEFAULT 1;

    SELECT capacity
    INTO v_capacity
    FROM buses
    WHERE id = NEW.bus_id;

    WHILE v_seat_no <= v_capacity DO
        INSERT INTO seats (departure_id, seat_number, status)
        VALUES (NEW.id, CAST(v_seat_no AS CHAR), 'available');

        SET v_seat_no = v_seat_no + 1;
    END WHILE;
END$$

DELIMITER ;

-- 9. TICKETS
CREATE TABLE tickets (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL,
    departure_id INT NOT NULL,
    total_price DECIMAL(10,2) NOT NULL,
    status ENUM('pending','confirmed','cancelled') DEFAULT 'pending',
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id),
    FOREIGN KEY (departure_id) REFERENCES departures(id)
);

-- 10. PASSENGERS
CREATE TABLE passengers (
    id INT PRIMARY KEY AUTO_INCREMENT,
    ticket_id INT NOT NULL,
    seat_id INT NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    id_number VARCHAR(50),
    FOREIGN KEY (ticket_id) REFERENCES tickets(id),
    FOREIGN KEY (seat_id) REFERENCES seats(id),
    UNIQUE KEY uniq_seat_id (seat_id)
);

-- 11. PAYMENTS
CREATE TABLE payments (
    id INT PRIMARY KEY AUTO_INCREMENT,
    ticket_id INT NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    method ENUM('credit_card','debit_card','paypal') NOT NULL,
    status ENUM('pending','completed','failed','refunded') DEFAULT 'pending',
    transaction_id VARCHAR(255),
    paid_at DATETIME,
    FOREIGN KEY (ticket_id) REFERENCES tickets(id)
);

-- 12. LOGS
CREATE TABLE logs (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT,
    action VARCHAR(255) NOT NULL,
    description TEXT,
    ip_address VARCHAR(45),
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id)
);

-- 13. PASSWORD_RESETS
CREATE TABLE password_resets (
    id INT PRIMARY KEY AUTO_INCREMENT,
    user_id INT NOT NULL,
    token VARCHAR(255) NOT NULL,
    expires_at DATETIME NOT NULL,
    used TINYINT(1) DEFAULT 0,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(id)
);

-- ================================================================
-- STORED PROCEDURE: Purchase Ticket
-- ================================================================
-- Atomic purchase flow: ticket insert + seat book + payment insert.
-- NOTE: This procedure handles ONE seat per ticket. For multiple passengers,
--       the tickets table design supports it but this procedure would need to be
--       modified to accept arrays/loops or called multiple times.
-- NOTE: This procedure does NOT insert into passengers table - that should be
--       done separately after ticket creation.
-- ================================================================
DELIMITER $$

CREATE PROCEDURE sp_purchase_ticket(
    IN p_user_id INT,
    IN p_departure_id INT,
    IN p_seat_id INT,
    IN p_total_price DECIMAL(10,2),
    IN p_payment_amount DECIMAL(10,2),
    IN p_payment_method VARCHAR(20),
    IN p_transaction_id VARCHAR(255)
)
BEGIN
    DECLARE v_ticket_id INT;

    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    START TRANSACTION;

    UPDATE seats
    SET status = 'booked'
    WHERE id = p_seat_id
      AND departure_id = p_departure_id
      AND status = 'available';

    IF ROW_COUNT() = 0 THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Seat is not available for this departure.';
    END IF;

    IF p_payment_method NOT IN ('credit_card', 'debit_card', 'paypal') THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Invalid payment method.';
    END IF;

    INSERT INTO tickets (user_id, departure_id, total_price, status)
    VALUES (p_user_id, p_departure_id, p_total_price, 'confirmed');

    SET v_ticket_id = LAST_INSERT_ID();

    INSERT INTO payments (ticket_id, amount, method, status, transaction_id, paid_at)
    VALUES (v_ticket_id, p_payment_amount, p_payment_method, 'completed', p_transaction_id, NOW());

    COMMIT;
END$$

DELIMITER ;

-- ================================================================
-- SEED DATA
-- ================================================================
-- SEED: Roller
INSERT INTO roles (name) VALUES ('admin'), ('user'), ('staff');