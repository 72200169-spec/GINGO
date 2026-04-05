<?php
try {
    $pdo = new PDO("mysql:host=127.0.0.1;dbname=gingo", "gingo_app", "gingo_password");
    $stmt = $pdo->query("SHOW TABLES LIKE 'comprobantes'");
    if ($stmt->rowCount() > 0) {
        echo "Table exists\n";
        $stmt = $pdo->query("DESCRIBE comprobantes");
        print_r($stmt->fetchAll(PDO::FETCH_ASSOC));
    } else {
        echo "Table does not exist\n";
    }
} catch (PDOException $e) {
    echo "Connection failed: " . $e->getMessage();
}
