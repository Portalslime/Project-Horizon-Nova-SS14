# DSH pack — Горизон Нова

Скопировать в **корень репозитория сборки** (рядом с `.git`):

```text
.dsh/
  AGENTS.md
  skills/
    hn-prototype/SKILL.md
    hn-csharp/SKILL.md
    hn-locale/SKILL.md
    hn-loadout-slot/SKILL.md
    hn-port/SKILL.md
    hn-review/SKILL.md
```

Либо:

```bash
# из корня репо игры
mkdir -p .dsh/skills
cp -r /path/to/dsh-horizon-nova/AGENTS.md .dsh/
cp -r /path/to/dsh-horizon-nova/skills/* .dsh/skills/
```

`AGENTS.md` DSH обычно подхватывает из корня проекта; если нужен именно под `.dsh/`, оставь копию и там, и в корне (или только в корне — как принято в твоей версии Harness).

## Назначение файлов

| Файл | Роль |
|------|------|
| `AGENTS.md` | Постоянные правила проекта (всегда) |
| `hn-prototype` | YAML entity/item |
| `hn-csharp` | C# компоненты/системы |
| `hn-locale` | Fluent ru-RU/en-US |
| `hn-loadout-slot` | Лодауты и слоты |
| `hn-port` | Порт с Goob/Wega/Lust/Fire/Corvax |
| `hn-review` | Ревью diff без правок |

Skills подгружаются по description. Явный вызов: `/hn-prototype`, `/hn-review` и т.д.

## Правка

Тела skills короткие — меняй чеклисты и запреты на месте.  
Не раздувай: детали по задаче — в чат, не в skill.

## Ограничения доступа (заложено)

- Нет доступа к RobustToolbox, картам, секретам по умолчанию.
- Новый код только в `_HorizonNova` (или точечно + маркер).
- `main` и push — только по явной команде.
- Ревью-skill не пишет в файлы без запроса.
