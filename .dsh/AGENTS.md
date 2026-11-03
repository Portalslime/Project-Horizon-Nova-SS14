# AGENTS.md — Горизон Нова (SS14)

Сборка: форк Lua Sector Frontier (`3fdcbcae09`). Репо: `Project-Horizon-Nova-SS14/Project-Horizon-Nova-SS14`.

## Область

Работай только с задачей пользователя. Не расширяй scope.

Разрешено трогать:
- `Content.Shared|Server|Client/_HorizonNova/**`
- `Resources/**/_HorizonNova/**`
- существующие файлы вне `_HorizonNova` — только точечно + маркеры HN

Запрещено без явной команды:
- `RobustToolbox/**`
- `bin/`, `*.rsi` целиком, карты (`Resources/Maps/**`)
- массовый reformat / rename / delete `_*-папок`
- push в `main`, force-push, правки чужих PR
- секреты, `.env`, токены, ключи API
- выдуманные `DataField`, слоты, id, компоненты

## Ветки

По умолчанию: `Develop_V_0.1`.  
`main` — только по явной команде. Фичи не коммитить в `main`.

## Куда писать

| Что | Куда |
|-----|------|
| Новый HN-контент | `Content.<Layer>/_HorizonNova/...` и `Resources/**/_HorizonNova/...` |
| Правка чужого файла | тот же путь + маркеры |
| Порт | папка источника (`_Goobstation`, `_Wega`, `_Lust`, `_Fire`, `_Corvax`) или `_HorizonNova` если сильно переписано |

Маркеры:
- YAML: `#HN: ...` или `#HN Start` / `#HN End`
- C#: `//HN Start` / `//HN End`; using: `//HN`
- FTL: комментарий строкой выше ключа

## Правила кода

1. id прототипов — английские. Русский только в `Resources/Locale/ru-RU/**/*.ftl`.
2. В YAML name/desc сырым русским не писать. Locale: `ent-<Id>` + `.desc`.
3. Слои: Shared = компоненты/net; Server = логика; Client = UI/спрайты/предикт. Серверную логику в Client не класть.
4. Слоты человека (не выдумывать):  
   `shoes jumpsuit outerClothing gloves neck mask eyes ears head socks underwearb underweart pocket1 pocket2 suitstorage id belt back wallet`
5. Перед кодом — поиск: нет ли уже такого id/компонента/слота в репо.
6. Вывод ИИ = черновик. Человек ревьюит до push. Не коммитить «как есть».

## Формат ответа (всегда)

```
ПЛАН
- тип / пути / зачем (3–7 строк)

ПРАВКИ
- файл: diff или полный новый короткий файл

ПРОВЕРКА
- dotnet build
- что заспавнить / слот / ключ FTL
```

Не цитировать целые неизменённые файлы. Нет файла в контексте — запросить фрагмент, не фантазировать.

## Команда

Глава: Zscreeper. Зам: Kanelorra. Целевой программист: Portal_Slime (любитель; C# объяснять коротко).

Игровые термины (лодаут, слот, прототип, станция, шаттл) не заменять бытовыми синонимами.
