---
name: hn-loadout-slot
description: Лодауты и слоты инвентаря Горизон Нова (бельё, equipment, loadout groups). Не выдумывать имена слотов. Не для произвольного C#.
---

# hn-loadout-slot

## Когда

Правка/добавление loadout, loadout group, слотов в inventory template, underwear.

## Запреты

- **Не выдумывать имена слотов.** Только из шаблона.
- Не класть предметы в несуществующий слот.
- Не ломать human inventory template без явной задачи.
- Не порт «всего белья» разом без списка id.

## Слоты человека (канон)

```
shoes jumpsuit outerClothing gloves neck mask eyes ears head
socks underwearb underweart
pocket1 pocket2 suitstorage id belt back wallet
```

Бельё Lua-стиль: `equipment.underweart` / `underwearb` / `socks`.  
Референс: `Resources/Prototypes/_Lua/Loadouts/Underwear/` (и `_Wega` / `_HorizonNova` если уже есть).

## Порядок

1. Прочитать актуальный inventory template (слоты).
2. Найти группу loadout и 1–2 соседних loadout.
3. Новый HN → `Resources/Prototypes/_HorizonNova/Loadouts/...`
4. Проверка: каждый ключ `equipment.*` = существующий слот + существующий prototype id.

## Чеклист

- [ ] слот из канон-списка
- [ ] prototype id предмета существует
- [ ] group/loadout id уникальны
- [ ] locale для видимых имён при необходимости
- [ ] маркеры HN на чужих файлах

## Вывод

ПЛАН → YAML loadout/group (минимум) → ПРОВЕРКА (спавн персонажа, слот, предмет на теле).
