# Порт ERP-панели (Interactions Panel) с Lust Station

Ветка: `feature/lust-erp-panel` от `checkpoint-2026-09-28-2`.

## Источник
`F:\MyGames\lust-station` (makura-games/lust-station), namespace `_Sunrise`:
- `Content.Shared/_Sunrise/InteractionsPanel`
- `Content.Server/_Sunrise/InteractionsPanel`
- `Content.Client/_Sunrise/InteractionsPanel`
- `Resources/Prototypes/_Sunrise/Interactions`, `Resources/Prototypes/_Lust/Interactions`
- `Resources/Textures|Audio/_Sunrise/Interactions`, `_Lust/Interactions`
- Доп.: `LoveVisionComponent/Overlay/System`, `ErpStatus`, `Humanoid/Erp.cs`, `Humanoid/Virginity.cs`, `Moan`-эмоут, реагент `Semen`.

## Приёмник
- `Content.Shared|Server|Client/_Lust/...`
- `Resources/Prototypes/_Lust/...`, `Resources/Textures/_Lust/...`, `Resources/Audio/_Lust/...`, `Resources/Locale/**/_Lust/...`

## Адаптации под эту сборку
- Namespace `Content.*_Sunrise*` → `Content.*_Lust*`; пути ассетов `_Sunrise` → `_Lust`.
- `HumanoidProfileComponent` → `HumanoidAppearanceComponent` (условия `Sex/Species/Genital`).
- `HasVisualLayerCondition` / `HasMarkingCondition` переписаны на `HumanoidAppearanceComponent.MarkingSet` (в сборке нет `SharedVisualBodySystem`).
- `BodyAreaTagCondition` переписан на покрытие по слотам инвентаря (`jumpsuit/outerClothing/underweart/underwearb/head/mask/neck/gloves/socks/shoes`), без тегов Lust. Не покрываются: `хвост`, `клетка`.
- `Sex.Futanari` не добавляется: убрано из условий (`requiredSex`), маппинг в Female/Unsexed.
- Категории и звуки Sunrise сохранены отдельными файлами `sunrise_*.yml` (в Lust `_Sunrise` и `_Lust` сосуществуют).

## Что выкинуто (по требованию заказчика или из-за отсутствия зависимостей)
- Кнопка «раздеться» и `RequestUndressMessage` (в сборке своя логика снятия одежды).
- Видимость эмоутов (`PlayerCache`): эмоут всегда уходит в радиусе голоса.
- Привязка клавиши `Interact` (F) и авто-открытие: остаётся только верб.
- `toyInteractions.yml` — нет сущностей `ERPDildo*`/`ERPFleshlight*`/плюшей.
- `ERP/Other/Structure/sex_rack.yml` — поле `buckleOffsets` несовместимо со `StrapComponent`.
- Дубль `sunrise_tail.yml` (в Lust конфликт `PatTail`).
- Реагент `Aphrodisiac` не тащился (LoveVision срабатывает от шкалы в панели).

## Замена Lua-ЭРП
- `EnumERPStatus` удалён; в `HumanoidCharacterProfile` добавлены `Erp`, `Virginity`, `AnalVirginity` (Lust).
- Выбор ЕРП перенесён во вкладку «Внешность» (`ErpButton`), 4 варианта: Нет / Спрашивать / Да / ОБСОЛЮТНОЕ. Отображается при осмотре (`DetailExaminable` + верб `ErpStatus`).
- Из вкладки «Описание» убраны три контрола (ЕРП + две девственности) и поясняющая метка; `Virginity`/`AnalVirginity` остаются полями профиля (без UI).
- В БД остаётся прежняя колонка `erpstatus` (int), в неё пишется `(int)Erp` (0=No,1=Ask,2=Yes,3=Absolute); новые колонки не создавались.
- `Virginity`/`AnalVirginity` не персистятся в БД (по умолчанию).
- ВНИМАНИЕ: старые значения `erpstatus` интерпретируются как новое `Erp`; при необходимости — сбросить.

## Проверка
- `dotnet build Content.Shared|Content.Server|Content.Client` — 0 ошибок.
- `dotnet run --project Content.YAMLLinter` — ошибок от `_Lust` нет (остаются 43 базовые, не связанные с портом).
