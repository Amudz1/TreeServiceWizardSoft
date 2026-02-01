# Примеры тестирования Tree Service API

## Сценарий 1: Базовая работа с деревом

### Шаг 1: Вход под администратором
```json
POST /api/auth/login
{
  "username": "admin",
  "password": "admin123"
}
```

### Шаг 2: Создание структуры дерева
```json
// Создание корня "Компания"
POST /api/nodes
{
  "name": "Компания",
  "description": "Головная организация"
}

// Создание отдела "Разработка"
POST /api/nodes
{
  "name": "Разработка",
  "description": "Отдел разработки ПО",
  "parentId": 1
}

// Создание команды "Backend"
POST /api/nodes
{
  "name": "Backend",
  "description": "Команда backend разработки",
  "parentId": 2
}

// Создание команды "Frontend"
POST /api/nodes
{
  "name": "Frontend",
  "description": "Команда frontend разработки",
  "parentId": 2
}

// Создание отдела "HR"
POST /api/nodes
{
  "name": "HR",
  "description": "Отдел кадров",
  "parentId": 1
}
```

### Шаг 3: Просмотр дерева
```
GET /api/nodes/tree
```

Результат:
```json
{
  "id": 1,
  "name": "Компания",
  "description": "Головная организация",
  "children": [
    {
      "id": 2,
      "name": "Разработка",
      "description": "Отдел разработки ПО",
      "children": [
        {
          "id": 3,
          "name": "Backend",
          "description": "Команда backend разработки",
          "children": []
        },
        {
          "id": 4,
          "name": "Frontend",
          "description": "Команда frontend разработки",
          "children": []
        }
      ]
    },
    {
      "id": 5,
      "name": "HR",
      "description": "Отдел кадров",
      "children": []
    }
  ]
}
```

## Сценарий 2: Проверка защиты от циклов

### Попытка создать цикл
```json
// Попытка сделать узел 1 дочерним для узла 3
// (это создаст цикл: 1 -> 2 -> 3 -> 1)
PUT /api/nodes/1
{
  "name": "Компания",
  "description": "Головная организация",
  "parentId": 3
}
```

Ожидаемый результат:
```json
{
  "message": "Moving node would create a cycle"
}
```

## Сценарий 3: Авторизация по ролям

### Вход под обычным пользователем
```json
POST /api/auth/login
{
  "username": "user",
  "password": "user123"
}
```

### Попытка удалить узел (должна быть отклонена)
```
DELETE /api/nodes/5
```

Ожидаемый результат: HTTP 403 Forbidden

### Попытка обновить узел (должна быть отклонена)
```json
PUT /api/nodes/5
{
  "name": "HR Updated",
  "description": "Обновленный отдел"
}
```

Ожидаемый результат: HTTP 403 Forbidden

### Создание нового узла (должно работать)
```json
POST /api/nodes
{
  "name": "QA",
  "description": "Отдел тестирования",
  "parentId": 1
}
```

Ожидаемый результат: HTTP 201 Created

## Сценарий 4: Экспорт дерева

### Экспорт всего дерева
```
GET /api/nodes/export
```

### Экспорт поддерева
```
GET /api/nodes/export?rootId=2
```

Результат (только ветка "Разработка"):
```json
{
  "id": 2,
  "name": "Разработка",
  "description": "Отдел разработки ПО",
  "children": [
    {
      "id": 3,
      "name": "Backend",
      "description": "Команда backend разработки",
      "children": []
    },
    {
      "id": 4,
      "name": "Frontend",
      "description": "Команда frontend разработки",
      "children": []
    }
  ]
}
```

## Сценарий 5: Каскадное удаление

### Попытка удалить узел с детьми
```
DELETE /api/nodes/2
```

Ожидаемый результат:
```json
{
  "message": "Cannot delete node with children"
}
```

### Правильная последовательность удаления
```
// Сначала удаляем листовые узлы
DELETE /api/nodes/3  // Backend
DELETE /api/nodes/4  // Frontend

// Теперь можем удалить родительский узел
DELETE /api/nodes/2  // Разработка
```

## Сценарий 6: Транзакции

### Создание узла с несуществующим родителем
```json
POST /api/nodes
{
  "name": "Новый узел",
  "description": "Тест",
  "parentId": 999
}
```

Ожидаемый результат:
```json
{
  "message": "Parent node does not exist"
}
```

Транзакция откатывается, узел не создается.

## Проверка валидации

### Создание узла без обязательного поля
```json
POST /api/nodes
{
  "description": "Описание без имени"
}
```

Ожидаемый результат: HTTP 400 Bad Request с сообщением о валидации

### Создание узла со слишком длинным именем
```json
POST /api/nodes
{
  "name": "Очень длинное имя которое превышает максимальную длину в 200 символов... (продолжайте до 201+ символов)",
  "description": "Тест"
}
```

Ожидаемый результат: HTTP 400 Bad Request с сообщением о валидации
