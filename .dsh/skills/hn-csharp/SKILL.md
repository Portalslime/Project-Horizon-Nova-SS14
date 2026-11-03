---
name: hn-csharp
description: C# код Горизон Нова — компоненты, системы, net-сообщения. Shared/Server/Client. Не для YAML-прототипов и не для массового рефакторинга.
---

# hn-csharp

## Когда

Новый или правка C# в `Content.Shared|Server|Client`.

## Запреты

- Не класть серверную логику в Client.
- Не править `RobustToolbox`.
- Не выдумывать `DataField` / атрибуты, которых нет в сборке.
- Не массовый rename/reformat.
- Новый код HN → namespace и папка `_HorizonNova`.

## Слои

| Слой | Что |
|------|-----|
| Shared | Component, NetworkedComponent, net-сообщения, общее |
| Server | EntitySystem логики мира |
| Client | UI, спрайты, предикт |

Имена: `FooComponent`, `FooSystem`. Namespace = путь (`Content.Shared._HorizonNova.Foo`).

## Маркеры (чужой файл)

```csharp
using Content.Shared._HorizonNova.Foo; //HN

//HN Start
// ...правка...
//HN End
```

## Порядок

1. Класс задачи → слой.
2. Поиск существующего компонента/системы.
3. Путь: `Content.<Layer>/_HorizonNova/<Feature>/` или точечная правка + маркер.
4. Минимум кода. Полный файл — только новый короткий.

## Чеклист

- [ ] слой верный
- [ ] `[RegisterComponent]` / networked по образцу соседей
- [ ] DataField существуют в базе
- [ ] маркеры HN
- [ ] нет логики Server в Client

## Вывод

ПЛАН (пути + слой) → diff/фрагмент → ПРОВЕРКА (`dotnet build`).
