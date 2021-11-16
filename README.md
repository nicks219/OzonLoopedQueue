### Тестовое задание для компании OZON
```console
Задача: создать кольцевой буфер - очередь (FIFO) на массиве фиксированного размера.
```
    Реализация: неблокирующий потокобезопасный кольцевой буфер ("обёртка" над обычным)
    Детали: вместо lock или SpinLock я задействовал Interlocked, т.е. блокировку на данных
    У меня есть некоторые сомнения, но под небольшой нагрузкой код не падает
    Я не смог воспроизвести ситуацию, когда один "клиент" смог бы "монополизировать" буфер
