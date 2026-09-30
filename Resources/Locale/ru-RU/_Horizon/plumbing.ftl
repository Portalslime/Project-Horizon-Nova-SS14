plumbing-pump-enabled = Вы включаете насос.
plumbing-pump-disabled = Вы выключаете насос.
plumbing-pump-examine-on = Он [color=green]работает[/color].
plumbing-pump-examine-off = Он [color=red]выключен[/color].

flushable-verb-flush = Смыть
flushable-overflow = { CAPITALIZE($entity) } переполняется, всё выливается на пол!
defecation-seat-overflow = { CAPITALIZE($seat) } переполнен, всё выливается на пол!

plumbing-analyzer-header = [bold]Анализ труб: { $target }[/bold]
plumbing-analyzer-network = Сеть у отверстия «{ $node }»: заполнена на [color=yellow]{ $percent }%[/color] ({ $volume }/{ $capacity } ед.)
plumbing-analyzer-empty =   Пусто.
plumbing-analyzer-reagent =   { $reagent }: { $quantity } ед. ({ $percent }%)

plumbing-filter-ui-title = Жидкостный фильтр
plumbing-filter-ui-search = Поиск жидкости...
plumbing-filter-ui-confirm = Фильтровать эту жидкость
plumbing-filter-ui-enabled = Фильтр: включён (нажмите, чтобы выключить)
plumbing-filter-ui-disabled = Фильтр: выключен (нажмите, чтобы включить)
plumbing-filter-ui-current = Фильтруется: { $reagent }
plumbing-filter-ui-none = Ничего

ent-PlumbingPipeHalf = трубопровод
    .desc = Переносит жидкости.
    .suffix = Заглушка
ent-PlumbingPipeStraight = трубопровод
    .desc = Переносит жидкости.
    .suffix = Прямая
ent-PlumbingPipeBend = трубопровод
    .desc = Переносит жидкости.
    .suffix = Изгиб
ent-PlumbingPipeTJunction = трубопровод
    .desc = Переносит жидкости.
    .suffix = Тройник
ent-PlumbingPipeFourway = трубопровод
    .desc = Переносит жидкости.
    .suffix = Крестовина
ent-PlumbingPump = жидкостный насос
    .desc = Перекачивает жидкость из сети труб на стороне входа в сеть на стороне выхода. Используйте, чтобы включить или выключить.
ent-PlumbingFilter = жидкостный фильтр
    .desc = Отправляет выбранную жидкость в боковое отверстие, а всё остальное пропускает дальше. Используйте, чтобы выбрать жидкость.
ent-PlumbingPort = жидкостный порт
    .desc = Подключение для бочек. Закрепите на нём бочку, чтобы подключить её к сети труб.
ent-PlumbingBarrel = бочка для жидкостей
    .desc = Бочка для жидкостей. Закрепите её на жидкостном порту, чтобы подключить к сети труб.
ent-PlumbingAnalyzer = анализатор жидкостных труб
    .desc = Ручной сканер. Используйте на трубе или устройстве, чтобы узнать давление и содержимое их сети.
ent-SinkDrain = сливная раковина
    .desc = Раковина для слива, а не для набора жидкости. Всё, что в неё вылито, уходит в канализацию.
