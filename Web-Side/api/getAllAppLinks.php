<?php
$host = 'localhost';
$db = 'tosters_offi';
$user = 'tosters_offi';
$password = 'YdYO2L7KwrAJHOAp';






















try
{
    $pdo = new PDO("mysql:host=$host;dbname=$db;charset=utf8", $user, $password);
    $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

    $query = "SELECT * FROM Apps";
    $stmt = $pdo->query($query);

    $fileSystemItems = $stmt->fetchAll(PDO::FETCH_ASSOC);

    header('Content-Type: application/json');
    echo json_encode($fileSystemItems, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE);
}
catch (PDOException $e)
{
    http_response_code(500);
    echo json_encode(['error' => $e->getMessage()]);
}
?>