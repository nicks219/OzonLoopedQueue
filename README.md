### Тестовое задание для компании OZON
```console
Задача: создать кольцевой буфер - очередь (FIFO) на массиве фиксированного размера.
```
    Реализация: lock-free потокобезопасный кольцевой буфер ("обёртка" над обычным)
    Детали: попытка использовать lock-free блокировку (Interlocked)
    Тесты: два бенчмарка для двух методов чтения из очереди
    Результат: средняя производительность - несколько млн. запросов/сек.
```console
Параллельно: ~3.000.000 операций/сек. на поток записи + столько же на поток чтения (в тестовых сценариях)
System.Threading.Channels показывает примерно такую же производительность (в тех же тестовых сценариях)
```
