<?php
// Конфигурация базы данных
$host = 'localhost';
$db = 'tosters_offi';
$user = 'tosters_offi';
$password = 'YdYO2L7KwrAJHOAp';

try
{
    // Подключение к базе данных через PDO
    $pdo = new PDO("mysql:host=$host;dbname=$db;charset=utf8", $user, $password);
    $pdo->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

    // Запрос для получения всех данных из таблицы
    $query = "SELECT * FROM Apps";
    $stmt = $pdo->query($query);

    // Получение данных в виде массива
    $fileSystemItems = $stmt->fetchAll(PDO::FETCH_ASSOC);

    // Конвертация массива в JSON и вывод
    header('Content-Type: application/json');
    echo json_encode($fileSystemItems, JSON_PRETTY_PRINT | JSON_UNESCAPED_UNICODE);
}
catch (PDOException $e)
{
    // Обработка ошибок подключения
    http_response_code(500);
    echo json_encode(['error' => $e->getMessage()]);
}
?>