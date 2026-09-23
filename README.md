# Horizon Nova

**Horizon Nova** (Горизонт Нова) — русскоязычный форк [Space Station 14](https://github.com/space-wizards/space-station-14), основанный на сборке [Sector Frontier 14 (Lua)](https://github.com/Lua-Frontier/sector-frontier-14) (коммит `3fdcbcae09`).

Проект ориентирован на создание стабильной и удобной в поддержке сборки с прозрачной структурой веток и чёткими правилами разработки.

## Атрибуция и используемые сборки

Проект основан на [Sector Frontier 14](https://github.com/Lua-Frontier/sector-frontier-14) (Lua).  
При заимствовании контента из других форков он по возможности размещается в соответствующих подкаталогах для удобства отслеживания авторства.

| Подкаталог       | Название форка      | Репозиторий                                                          | Лицензия  |
|------------------|---------------------|----------------------------------------------------------------------|-----------|
| `_NF`            | Frontier Station    | https://github.com/new-frontiers-14/frontier-station-14              | AGPL 3.0  |
| `_CD`            | Cosmatic Drift      | https://github.com/cosmatic-drift-14/cosmatic-drift                  | MIT       |
| `_Corvax`        | Corvax              | https://github.com/space-syndicate/space-station-14                  | MIT       |
| `_Corvax`        | Corvax Frontier     | https://github.com/Corvax-Frontier/Frontier                          | AGPL 3.0  |
| `_Corvax`        | Corvax WL           | https://github.com/corvax-team/ss14-wl                               | —         |
| `_Corvax_Goob`   | Corvax Goob         | —                                                                    | —         |
| `_DV`            | Delta-V             | https://github.com/DeltaV-Station/Delta-v                            | AGPL 3.0  |
| `_EE`            | Einstein Engines    | https://github.com/Simple-Station/Einstein-Engines                   | AGPL 3.0  |
| `_Emberfall`     | Emberfall           | https://github.com/emberfall-14/emberfall                            | MIT       |
| `_EstacaoPirata` | Estacao Pirata      | https://github.com/Day-OS/estacao-pirata-14                          | AGPL 3.0  |
| `_Goobstation`   | Goob Station        | https://github.com/Goob-Station/Goob-Station                         | AGPL 3.0  |
| `_Impstation`    | Impstation          | https://github.com/impstation/imp-station-14                         | AGPL 3.0  |
| `_NC14`          | Nuclear 14          | https://github.com/Vault-Overseers/nuclear-14                        | AGPL 3.0  |
| `Nyanotrasen`    | Nyanotrasen         | https://github.com/Nyanotrasen/Nyanotrasen                           | MIT       |
| `_Lua`           | Sector Frontier 14  | https://github.com/Lua-Frontier/sector-frontier-14                   | AGPL 3.0  |
| `_ADT`           | AdventureTimeSS14   | https://github.com/AdventureTimeSS14/space_station_ADT               | AGPL 3.0  |
| `_Nuclear14`     | Nuclear 14          | https://github.com/Vault-Overseers/nuclear-14                        | AGPL 3.0  |
| `_RMC14`         | RMC-14              | https://github.com/RMC-14/RMC-14                                     | MIT       |
| `_Backman`       | Rxup                | https://github.com/Rxup/space-station-14                             | AGPL 3.0  |
| `_DeadSpace`     | Мёртвый Космос      | https://github.com/dead-space-server/space-station-14-fobos          | Custom    |
| `_Mono`          | Monolith            | https://github.com/Monolith-Station/Monolith                         | AGPL 3.0  |
| `_Theta`         | ThetaStation        | https://github.com/ThetaStation/ThetaStation                         | AGPL 3.0  |
| `Sirena`         | Sirena              | https://github.com/EvgenRP99/SS14-Sirena                             | MIT       |
| `_Erida`         | Erida               | https://github.com/SS14Backmen/space-station-14-Erida                | AGPL 3.0  |
| `ShibaStation`   | ShibaStation        | https://github.com/AstroDogeDX/ShibaStation-GS                       | AGPL 3.0  |
| `Imperial`       | Imperial            | https://github.com/imperial-space/SS14-public                        | MIT       |
| `_Wega`          | Wega                | https://github.com/wega-team/ss14-wega                               | GPL 3.0   |
| `_Lust`          | Lust Station        | https://github.com/makura-games/lust-station                         | —         |
| `_Fire`          | Project Fire        | https://github.com/makura-games/project-fire                         | —         |

## Документация

- [Официальная документация Space Station 14](https://docs.spacestation14.com/)
- [Документация по настройке окружения разработки](https://docs.spacestation14.com/en/general-development/setup.html)
- [Goob Station Docs](https://docs.goobstation.com/)
- [Space Wizards Development Wiki](https://docs.spacestation14.com/)

## Сборка проекта

1. Клонируйте репозиторий:
   ```bash
   git clone https://github.com/Project-Horizon-Nova-SS14/Project-Horizon-Nova-SS14.git
   cd Project-Horizon-Nova-SS14
   ```

2. Инициализируйте подмодули и загрузите движок:
   ```bash
   python RUN_THIS.py
   ```

3. Соберите решение:
   ```bash
   dotnet build
   ```

Подробная инструкция: [Setting up a Development Environment](https://docs.spacestation14.com/en/general-development/setup.html)

## Структура веток

```
main
├── CodeFix     — исправление ошибок и мелкие правки
└── Develop     — разработка нового контента (может быть несколько параллельных веток)
    ├── Feature — отдельные функции / механики
    └── Release — доработка и объединение Feature-веток перед влитием в main
```

### Правила вливания изменений

- **Feature → чужая Feature**  
  Требуется разрешение владельца целевой Feature-ветки.

- **Feature → Develop**  
  Требуется разрешение ответственного за данную Develop-ветку.  
  В ветку `Release` новый функционал не добавляется.

- **CodeFix → main** и **Develop → main**  
  Только после разрешения одного из основных разработчиков.

Любой член команды разработки может создавать Feature-ветки.

## Контакты

По вопросам разработки:

| Роль                  | Ник                | Discord     |
|-----------------------|--------------------|-------------|
| Основной разработчик  | Portal_Slime       | Portal_Slime|
| Глава проекта         | Zscreeper          | Zscreeper   |
| Заместитель главы     | Kanelorra          | Kanelorra   |

## Лицензия

Код, добавленный в этот репозиторий, распространяется под лицензией **GNU Affero General Public License версии 3.0**, если не указано иное.  
Полный текст лицензии: `LICENSE-AGPLv3.txt`.

Большинство ассетов лицензированы под [CC-BY-SA 3.0](https://creativecommons.org/licenses/by-sa/3.0/), если не указано иное в метаданных файлов.

Некоторые ассеты могут иметь некоммерческие лицензии (CC-BY-NC-SA и аналогичные). Их необходимо удалить при коммерческом использовании проекта.

