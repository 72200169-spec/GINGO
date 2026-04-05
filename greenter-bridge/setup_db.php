<?php
$hosts = ['127.0.0.1', 'localhost'];
$users = ['root', 'gingo_app'];
$passwords = ['', 'gingo_password', 'root'];

foreach ($hosts as $host) {
    foreach ($users as $user) {
        foreach ($passwords as $password) {
            try {
                $pdo = new PDO("mysql:host=$host", $user, $password);
                echo "Connected as $user@$host\n";
                
                $pdo->exec("CREATE DATABASE IF NOT EXISTS gingo");
                $pdo->exec("USE gingo");
                
                $sql = "CREATE TABLE IF NOT EXISTS comprobantes (
                    id INT AUTO_INCREMENT PRIMARY KEY,
                    tipo_doc VARCHAR(2) NOT NULL,
                    serie VARCHAR(4) NOT NULL,
                    correlativo VARCHAR(10) NOT NULL,
                    fecha_emision DATETIME NOT NULL,
                    ruc_emisor VARCHAR(11) NOT NULL,
                    doc_cliente VARCHAR(15) NOT NULL,
                    nombre_cliente VARCHAR(255) NOT NULL,
                    total DECIMAL(12, 2) NOT NULL,
                    xml_path TEXT,
                    cdr_path TEXT,
                    pdf_path TEXT,
                    estado_sunat VARCHAR(50) DEFAULT 'PENDIENTE',
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )";
                $pdo->exec($sql);
                echo "Table 'comprobantes' created or exists.\n";
                
                // Also create gingo_app user if needed
                if ($user === 'root') {
                    $pdo->exec("CREATE USER IF NOT EXISTS 'gingo_app'@'localhost' IDENTIFIED BY 'gingo_password'");
                    $pdo->exec("GRANT ALL PRIVILEGES ON gingo.* TO 'gingo_app'@'localhost'");
                    $pdo->exec("CREATE USER IF NOT EXISTS 'gingo_app'@'127.0.0.1' IDENTIFIED BY 'gingo_password'");
                    $pdo->exec("GRANT ALL PRIVILEGES ON gingo.* TO 'gingo_app'@'127.0.0.1'");
                    $pdo->exec("FLUSH PRIVILEGES");
                    echo "User 'gingo_app' created and granted privileges.\n";
                }
                
                exit(0);
            } catch (PDOException $e) {
                // Continue
            }
        }
    }
}
echo "Failed to connect to MySQL\n";
exit(1);
