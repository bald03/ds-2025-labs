#!/bin/bash

# Переход в корень проекта
cd "$(dirname "$0")/.." || exit 1

# 1. Остановка приложений
if [ -f .pids ]; then
    echo "Остановка приложений..."
    source .pids
    kill $VALUATOR_PID 2>/dev/null && echo "Valuator остановлен (PID $VALUATOR_PID)"
    for i in {1..3}; do
        pid_var="RANK_PID_$i"
        kill ${!pid_var} 2>/dev/null && echo "RankCalculator $i остановлен (PID ${!pid_var})"
    done
    rm .pids
else
    echo "Файл с PID не найден. Остановите процессы вручную:"
    echo "ps aux | grep 'dotnet run'"
    echo "kill [PID]"
fi

# 2. Остановка RabbitMQ
echo "Остановка RabbitMQ..."
brew services stop rabbitmq

# 3. Остановка Redis
echo "Остановка Redis..."
docker stop redis 2>/dev/null && docker rm redis 2>/dev/null && echo "Redis контейнер остановлен и удален"

echo "----------------------------------------"
echo "Все сервисы остановлены"
