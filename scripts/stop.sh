#!/bin/bash

# Остановка всех экземпляров приложения
pkill -f "dotnet run --urls http://0.0.0.0:5001"
pkill -f "dotnet run --urls http://0.0.0.0:5002"

# Остановка Nginx
nginx -s stop
