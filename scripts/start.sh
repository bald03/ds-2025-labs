#!/bin/bash

# Переход в корень проекта
cd "$(dirname "$0")/.." || { echo "Ошибка перехода в корень проекта"; exit 1; }

# Создаем папку для логов заранее
mkdir -p logs || { echo "Не удалось создать папку logs"; exit 1; }

# 1. Проверка RabbitMQ
echo "Проверка RabbitMQ..."
if ! brew services list | grep rabbitmq | grep -q started; then
    echo "Запуск RabbitMQ..."
    brew services start rabbitmq || { echo "Ошибка запуска RabbitMQ"; exit 1; }
    sleep 5  # Даем время для запуска
fi

# 2. Проверка Redis
echo "Проверка Redis..."
if ! docker ps --format '{{.Names}}' | grep -q redis; then
    if docker ps -a --format '{{.Names}}' | grep -q redis; then
        echo "Запуск существующего контейнера Redis..."
        docker start redis || { echo "Ошибка запуска Redis"; exit 1; }
    else
        echo "Создание нового контейнера Redis..."
        docker run -d --name redis -p 6379:6379 redis || { echo "Ошибка создания Redis"; exit 1; }
    fi
    sleep 3  # Даем время для запуска
fi

# 3. Запуск Valuator с проверкой
echo "Запуск Valuator..."
cd Valuator || { echo "Папка Valuator не найдена"; exit 1; }

# Убиваем предыдущий процесс, если есть
pkill -f "dotnet run"

# Запускаем и проверяем
dotnet run > ../logs/valuator.log 2>&1 &
VALUATOR_PID=$!
sleep 5  # Даем время для запуска

if ! ps -p $VALUATOR_PID > /dev/null; then
    echo "Ошибка: Valuator не запустился. Проверьте logs/valuator.log"
    tail -n 20 ../logs/valuator.log
    exit 1
fi

cd ..

# 4. Запуск RankCalculator с проверкой
echo "Запуск RankCalculator (3 экземпляра)..."
for i in {1..3}; do
    cd RankCalculator || { echo "Папка RankCalculator не найдена"; exit 1; }

    dotnet run > ../logs/rankcalculator_$i.log 2>&1 &
    RANK_PIDS[$i]=$!
    sleep 1

    if ! ps -p ${RANK_PIDS[$i]} > /dev/null; then
        echo "Ошибка: RankCalculator $i не запустился. Проверьте logs/rankcalculator_$i.log"
        tail -n 20 ../logs/rankcalculator_$i.log
    fi

    cd ..
done

# Проверка доступности Valuator
echo "Проверка доступности Valuator..."
if ! curl -s http://localhost:5000 > /dev/null; then
    echo "ОШИБКА: Valuator не отвечает на порту 5000"
    echo "Последние строки лога:"
    tail -n 20 logs/valuator.log
    exit 1
fi

# Сохраняем PID процессов
echo "VALUATOR_PID=$VALUATOR_PID" > .pids
for i in {1..3}; do
    echo "RANK_PID_$i=${RANK_PIDS[$i]}" >> .pids
done

echo "----------------------------------------"
echo "Сервисы успешно запущены и проверены:"
echo "- RabbitMQ: http://localhost:15672 (guest/guest)"
echo "- Redis: redis://localhost:6379"
echo "- Valuator: http://localhost:5000 (PID $VALUATOR_PID)"
echo "- RankCalculator: PIDs ${RANK_PIDS[@]}"
echo "Логи:"
ls -lh logs/