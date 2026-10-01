# Husbandry: животноводство (лошадь)

Автономный модуль: нужды (сытость и жажда), пол, рост по стадиям, питание и питьё, навоз, езда верхом. Первое животное — лошадь (`MobHorse`). Новые животные делаются от `BaseHusbandryAnimal` (см. раздел 9).

Модуль почти не правит апстримные файлы (исключение: две правки для стаков навоза, см. раздел 10) и не зависит от ванильных `Hunger`, `Thirst`, `Reproductive`, `Vehicle`, `Defecation`. Он использует только публичные системы и события движка (раздел 6) и общий модуль звуков `_Horizon/SoundCues` (свой README рядом с кодом). Поэтому его можно вынуть папками и перенести.

Статус: собирается, YAML-линтер прототипов модуля проходит, интеграционные тесты `Content.IntegrationTests/Tests/_Horizon/HorseRideTest.cs` проходят (езда, снятие седла, жеребёнок и молодая, здоровье по стадиям), в игре проверено частично. Звуки через `SoundCues` и их тесты (`SoundCuesTest.cs`, `HorseRideTest.MountingMakesTheSoundOfTheMountedCue`) добавлены позже, сборка и тесты после них ещё не прогонялись. Спрайты — плейсхолдеры, из звуков настоящий только хруст (раздел 10).

---

## 1. Как это работает

```
AnimalNeedsSystem (раз в секунду)
  Satiety, Hydration убывают на DecayPerMinute
  значение <= SeekBelow  ──► NeedPrecondition выполняется
  значение <= 0          ──► раз в DamageInterval урон (DeprivationDamage × минуты на нуле)

HTN HorseCompound (ветка еды, ветка воды, иначе HorseIdleCompound)
  HorseIdleCompound: AnimalWanderOperator ─► AnimalWanderSystem (прогулка из нескольких отрезков, каждый — маршрут для AnimalWalkerSystem)
                     ─► WaitOperator (время стоянки)
  NeedPrecondition ─► FindFoodOperator / FindWaterOperator ─► AnimalGoToOperator ─► AnimalEatOperator / AnimalDrinkOperator ─► WaitOperator
                      (цель + путь тайлов, 30 тайлов)           (AnimalWalkerSystem: путь срезается по прямым, ходьба без рулёжки)
                      (ищет цель, проверяет путь)              (идёт)            (запускает AnimalFeedingSystem.TryEat/TryDrink)

AnimalFeedingSystem
  TryEat / TryDrink ─► DoAfter ─► по завершении:
     AnimalNeedsSystem.ModifySatiety / ModifyHydration
     Appearance AnimalVisuals.Eating (true на время DoAfter) ─► GenericVisualizer меняет состояние слоёв
     событие AnimalAteEvent  ─► ManureProducerSystem: питательность × UnitsPerNutrition = куски Product (стак), остаток копится,
                                потом событие AnimalProducedEvent
     событие AnimalDrankEvent
  TryEat в начале поднимает AnimalEatStartedEvent, а если еду прервали (DoAfter отменён) — AnimalEatInterruptedEvent

GrowthSystem
  MapInit: AnimalSexSystem.EnsureRolled, выбор начальной стадии ─► SetStage
  Update: по таймеру переход на следующую стадию ─► SetStage
  SetStage: добавляет ComponentRegistry стадии, поднимает GrowthStageChangedEvent
     GrowthEffectsSystem: имя (по стадии и полу), масштаб спрайта, Butcherable (мясо), MobPrice (цена),
                          порог поиска еды и расход сытости в AnimalNeeds, выход навоза в ManureProducer

RideableSystem (Shared, предсказывается)
  Strap включён, только пока в слоте есть седло
  StrapAttempt: проверки (седло, жива, не занята, свободные руки, не тянет ли кого)
  Strapped: виртуальный предмет в руку (поводья), SetRelay(всадник → животное), слот седла блокируется,
            RiderMountedEvent ─► RideableNpcSystem выключает HTN
  Unstrapped: обратное, RiderDismountedEvent ─► HTN включается
  Двери: пока есть всадник, у животного временно стоит тег DoorBumpOpener, а доступ берётся у всадника (GetAdditionalAccessEvent)
  Поводья лежат в свободной руке, которая не активна: активная остаётся свободной для кликов по дверям и предметам
  RiderDamageRedirectSystem: урон по всаднику отменяется и применяется к животному
  RideableVisualsSystem (Client): сдвигает спрайт всадника по стороне, куда смотрит животное; на юг (животное мордой к камере) всадник рисуется под животным, в остальных сторонах над ним

AnimalSoundBindingsSystem (Shared; единственное место, где Husbandry встречается со звуком, сами звуки — модуль _Horizon/SoundCues)
  RiderMountedEvent ─► cue Mounted (всадник передаётся как User: его клиент предсказал посадку и играет звук сам, сервер ему свою копию не шлёт)
  AnimalEatStartedEvent ─► cue EatStarted, AnimalEatInterruptedEvent ─► обрыв звука EatStarted
  AnimalDrankEvent ─► cue Drank, AnimalProducedEvent ─► cue Produced
  что слышно на каждый cue, написано в таблице SoundCues самого животного (horse.yml); нет таблицы, cue или файла — тишина
```

---

## 2. Карта файлов

Строки — на момент написания. «Движок» — что придётся заменить при переносе в игру без SS14 (подробнее в разделе 6).

### Content.Shared/_Horizon/Husbandry

| Файл | Строк | Что это | Зависит от движка |
|---|---|---|---|
| `Core/AnimalSexKind.cs` | 10 | enum `Male`/`Female` | нет |
| `Core/NeedRules.cs` | 28 | `Tick` (убывание, возвращает минуты на нуле), `Modify` (прибавка с границами) | нет |
| `Core/GrowthRules.cs` | 24 | `TryGetNext` (следующая стадия), `RollSex` | нет |
| `Core/ProductionRules.cs` | 23 | `Accumulate` (копит, возвращает число готовых партий) | нет |
| `Needs/NeedState.cs` | 56 | данные одной нужды, `GetLevel()` | `DamageSpecifier` |
| `Needs/NeedLevel.cs` | 14 | `Satisfied`/`Low`/`Empty`, единственное, что видит клиент | `NetSerializable` |
| `Needs/AnimalNeedsComponent.cs` | 44 | компонент: `Satiety`, `Hydration`, уровни, интервалы | сетевой компонент, `AutoPausedField` |
| `Needs/AnimalNeedsExamineSystem.cs` | 29 | текст при осмотре | `ExaminedEvent`, `Identity` |
| `Sex/AnimalSexComponent.cs` | 23 | пол, `Randomize`, `MaleChance` | сетевой компонент |
| `Growth/GrowthComponent.cs` | 79 | стадии (`GrowthStageDef`), текущая стадия, таймер | `ComponentRegistry`, `AutoPausedField` |
| `Growth/GrowthEvents.cs` | 7 | `GrowthStageChangedEvent` | `ByRefEvent` |
| `Feeding/DietComponent.cs` | 38 | что ест животное | `EntityWhitelist` |
| `Feeding/WaterSourceComponent.cs` | 26 | из чего пьёт животное | нет |
| `Feeding/FeedingEvents.cs` | 34 | `AnimalEatStartedEvent`, `AnimalEatInterruptedEvent`, `AnimalAteEvent`, `AnimalDrankEvent`, DoAfter-события | `DoAfterEvent` |
| `Production/ManureProducerComponent.cs` | 34 | продукт и кусков на единицу питательности | нет |
| `Production/ProductionEvents.cs` | 9 | `AnimalProducedEvent` | `ByRefEvent` |
| `Walking/AnimalWalkerComponent.cs` | 115 | настройки ходьбы (допуски, зазор, замедление, застревание), маршрут и статус | нет |
| `Wander/AnimalWanderComponent.cs` | 125 | настройки прогулок: шанс, время стоянки, отрезки, дистанции, повороты, паузы | нет |
| `Pulling/PullFacingComponent.cs` | 21 | скорость поворота и минимальная дистанция | нет |
| `Pulling/PullMassComponent.cs` | 26 | плотность фикстур, пока животное тащат | нет |
| `Rideable/RideableComponent.cs` | 48 | слот седла, руки, редирект урона, оффсеты, текущий всадник | сетевой компонент |
| `Rideable/SaddleComponent.cs` | 9 | маркер седла | нет |
| `Rideable/RiderComponent.cs` | 13 | на всаднике, ссылка на животное | нет |
| `Rideable/RideableEvents.cs` | 13 | `RiderMountedEvent`, `RiderDismountedEvent` | `ByRefEvent` |
| `Rideable/RideableSystem.cs` | 186 | вся логика езды | Buckle/Strap, ItemSlots, Hands, VirtualItem, Mover, MobState, Popups |
| `Visuals/AnimalVisuals.cs` | 17 | ключи `Eating` и `Saddled` для Appearance | `NetSerializable` |
| `Visuals/SaddleVisualsComponent.cs` | 17 | слот седла для визуала (`Slot`) | нет |
| `Visuals/SaddleVisualsSystem.cs` | 45 | ставит `Saddled` по слоту седла | `SharedAppearanceSystem`, `ItemSlotsSystem` |
| `Sounds/AnimalSoundBindingsSystem.cs` | 65 | события животного → звуковые сигналы | модуль `_Horizon/SoundCues` |

Сам звуковой модуль, `Content.Shared/_Horizon/SoundCues`, описан в своём README.

### Content.Server/_Horizon/Husbandry

| Файл | Строк | Что это | Зависит от движка |
|---|---|---|---|
| `Needs/AnimalNeedsSystem.cs` | 143 | убывание, урон, `ModifySatiety`/`ModifyHydration` | Damageable, MobState, Rejuvenate |
| `Sex/AnimalSexSystem.cs` | 39 | бросок пола, `EnsureRolled` | random |
| `Growth/GrowthSystem.cs` | 84 | стадии по времени | MobState, `AddComponents` |
| `Growth/GrowthEffectsSystem.cs` | 46 | применяет стадию к имени, размеру, мясу, цене | MetaData, ScaleVisuals, `Butcherable`, `MobPrice` (`_NF`) |
| `Feeding/AnimalFeedingSystem.cs` | 186 | `CanEat`, `TryEat`, `CanDrink`, `TryDrink`, обработка DoAfter | DoAfter, Appearance, SolutionContainer, Whitelist, Container, `FoodComponent` |
| `Production/ManureProducerSystem.cs` | 49 | навоз по `AnimalAteEvent`, потом `AnimalProducedEvent` | `StackSystem` (стаки) |
| `Npc/NeedPrecondition.cs` | 32 | предусловие HTN «нужда ниже порога» | HTN |
| `Npc/FindFoodOperator.cs` | 93 | ищет ближайшую съедобную и достижимую еду | HTN, EntityLookup, Pathfinding |
| `Npc/FindWaterOperator.cs` | 89 | то же для воды | HTN, EntityLookup, Pathfinding |
| `Npc/AnimalEatOperator.cs` | 43 | запускает еду, ставит `IdleTime` | HTN |
| `Npc/AnimalDrinkOperator.cs` | 43 | запускает питьё, ставит `IdleTime` | HTN |
| `Walking/AnimalWalkerSystem.cs` | 270 | ходьба по маршруту (ввод в `InputMover` раз в тик), срезание пути по прямым, проверки свободного места | `SharedPhysicsSystem` (лучи), `SharedMapSystem`, `TurfSystem`, `InputMoverComponent`, `PullableComponent` |
| `Wander/AnimalWanderSystem.cs` | 200 | прогулки: выбор места, отрезки, паузы | `AnimalWalkerSystem` |
| `Npc/AnimalWanderOperator.cs` | 65 | запускает прогулку из HTN, задаёт время стоянки | HTN |
| `Npc/AnimalGoToOperator.cs` | 95 | ведёт к цели по маршруту из блэкборда | HTN, `AnimalWalkerSystem` |
| `Pulling/PullFacingSystem.cs` | 36 | пока животное тащат, плавно поворачивает его мордой к тянущему | `PullableComponent`, `RotateToFaceSystem` |
| `Pulling/PullMassSystem.cs` | 70 | облегчает тащимое животное, потом возвращает массу | `PullStartedMessage`/`PullStoppedMessage`, `SharedPhysicsSystem.SetDensity` |
| `Rideable/RideableNpcSystem.cs` | 36 | включает/выключает HTN по событиям езды | `HTNSystem.SetHTNEnabled` |
| `Rideable/RiderDamageRedirectSystem.cs` | 40 | урон всаднику → животному | `BeforeDamageChangedEvent`, Damageable |

### Content.Client/_Horizon/Husbandry

| Файл | Строк | Что это | Зависит от движка |
|---|---|---|---|
| `Rideable/RideableVisualsSystem.cs` | 55 | оффсет спрайта всадника по стороне | Sprite, Eye, Transform |

### Resources

| Файл | Что это |
|---|---|
| `Prototypes/_Horizon/Husbandry/base_animal.yml` | `BaseHusbandryAnimal` (абстрактный) |
| `Prototypes/_Horizon/Husbandry/horse.yml` | `MobHorse` и варианты `MobHorseFoal`, `MobHorseYoung`, `MobHorseMale`, `MobHorseFemale`, `MobHorseSaddled` (взрослая уже в седле, для меню спавна: слот седла объявлен в самом прототипе, а стадия `Adult` добавляет компоненты только если их ещё нет) |
| `Prototypes/_Horizon/Husbandry/horse_npc.yml` | HTN `HorseCompound` |
| `Prototypes/_Horizon/Husbandry/horse_food.yml` | `FoodMeatHorse`, `FoodMeatHorseCooked`, граф `HorseMeatSteak`, `HorseManure` |
| `Prototypes/_Horizon/Husbandry/saddle.yml` | `HorseSaddle` + граф и рецепт (20 ткани) |
| `Prototypes/_Horizon/Husbandry/trough.yml` | `AnimalTrough` + граф и рецепт (5 стали, категория сантехники) |
| `Prototypes/_Horizon/Husbandry/sounds.yml` | коллекции `HorseNeigh`, `HorseFootstep`, `HorseEat` |
| `Prototypes/_Horizon/Husbandry/sound_cues.yml` | сигналы животных: `Mounted`, `EatStarted`, `Drank`, `Produced` (что слышно на каждый, написано в `SoundCues` животного) |
| `Audio/_Horizon/Animals/` | `horse_eat_1..3.ogg` и `attributions.yml` (раздел 12) |
| `Tools/_Horizon/audio/prepare_sfx.py` | нарезка записей в mono OGG (раздел 11) |
| `Locale/en-US/_Horizon/husbandry.ftl`, `Locale/ru-RU/_Horizon/husbandry.ftl` | строки |
| `Textures/_Horizon/Mobs/Animals/horse.rsi` | лошадь и седло на ней (автор mrl4an), черновик |
| `Textures/_Horizon/Objects/Husbandry/saddle.rsi` | иконка седла (автор mrl4an), черновик |
| `Textures/_Horizon/Objects/Husbandry/trough.rsi` | поилка (распиленное бревно): `trough` (4 направления, пустое корыто) и `trough-fill-1` (вода, отдельный слой); нарисована под проект, черновик |
| `Textures/_Horizon/Objects/Husbandry/horse_manure.rsi` | плейсхолдер иконки навоза |
| `Textures/_Horizon/Objects/Husbandry/horse_meat.rsi` | плейсхолдер сырой и готовой конины (копия обычного мяса) |
| `Textures/_Horizon/Objects/Devices/debug_analyzer.rsi` | плейсхолдер дебаг-анализатора (копия анализатора здоровья) |

Все спрайты собственные папки `_Horizon`, ванильные ассеты модуль больше не использует. Список состояний, которые нужно нарисовать, см. раздел 11.

---

## 3. Компоненты (имена в YAML)

| Компонент | Главные поля (значения по умолчанию) | Кто обрабатывает |
|---|---|---|
| `AnimalNeeds` | `satiety` и `hydration` (`NeedState`), `updateRate` 1 с, `damageInterval` 10 с | `AnimalNeedsSystem`, `AnimalNeedsExamineSystem` |
| `AnimalSex` | `sex`, `randomize` true, `maleChance` 0.5 | `AnimalSexSystem` |
| `Growth` | `stages`, `initialStage` (последняя, если не задана), `meat` `FoodMeat` | `GrowthSystem`, `GrowthEffectsSystem` |
| `Diet` | `whitelist`, `blacklist`, `solution` `food`, `nutritionPerUnit` 1, `eatDelay` 2 с | `AnimalFeedingSystem`, операторы HTN |
| `WaterSource` | `solution` `tank`, `amountPerDrink` 15, `hydrationPerUnit` 1, `delay` 2 с | `AnimalFeedingSystem` |
| `ManureProducer` | `product` (обязательно, стакающаяся сущность), `unitsPerNutrition` 1, `dropOffset` 0 (на сколько тайлов позади животного класть, против его взгляда) | `ManureProducerSystem` |
| `AnimalWalker` | `waypointTolerance` 0.4, `slowDistance` 1.5, `minSpeedFraction` 0.35, `clearance` 0.45 (половина ширины коридора), `stuckTime` 1.5 с, `stuckDistance` 0.2, `maxRouteTime` 90 с, `smoothLookahead` 15 | `AnimalWalkerSystem` |
| `AnimalWander` | `walkChance` 0.85, `standTime` 4–12 с, `retryTime` 2–5 с (если места нет), `legs` 2–4 (отрезков за прогулку), `minDistance` 4 и `maxDistance` 10 (желаемая длина отрезка, короче там, где тесно), `minLegDistance` 2, `turnAngle` 70°, `legPause` 0.5–2.5 с, `probes` 10, `minClearProbes` 3, `arriveDistance` 0.3, `maxWalkTime` 120 с | `AnimalWanderSystem`, `AnimalWanderOperator` |
| `PullMass` | `density` 50 | `PullMassSystem` |
| `PullFacing` | `rotationSpeed` 8, `minDistance` 0.4 | `PullFacingSystem` |
| `Rideable` | `saddleSlot` `saddle_slot`, `requiredHands` 1, `redirectDamage` true, `riderOpensDoors` true, `south/north/east/westOffset` (в тайлах; идут по сети, потому что компонент добавляет стадия на сервере, а смещение рисует клиент) | `RideableSystem`, `RideableNpcSystem`, `RiderDamageRedirectSystem`, `RideableVisualsSystem` |
| `Saddle` | маркер | белый список слота |
| `Rider` | `mount` | ставится и снимается `RideableSystem` |
| `SoundCues` | `cues`: имя сигнала → `sound` (файл или коллекция) и `cooldown`; компонент модуля `_Horizon/SoundCues`, не сетевой, объявляется в самом прототипе | `SoundCueSystem`, привязки `AnimalSoundBindingsSystem` |

`NeedState` (вложенные `satiety` и `hydration`): `value` (−1 = бросить из `startingRange`), `max` 100, `startingRange` 70–100, `decayPerMinute`, `seekBelow` 50, `deprivationDamage` (урон в минуту при нуле). По умолчанию сытость убывает на 2 в минуту, жажда на 1.

`GrowthStageDef`: `id`, `duration` (секунды, пусто у последней), `names` (пол → ключ локализации), `scale`, `maxHealth` (порог смерти, 0 — не менять), `meatCount`, `price`, `satietySeekBelow` (порог поиска еды, пусто — не менять), `satietyDecayPerMinute` (расход сытости, то есть сколько животное ест, пусто — не менять), `manureUnitsPerNutrition` (кусков навоза на единицу питательности, пусто — не менять), `components` (добавляются при входе в стадию; порядок в списке важен: `Strap` и слот идут раньше `Rideable`).

---

## 4. События

| Событие | Кто поднимает | Где поднимается | Кто слушает |
|---|---|---|---|
| `AnimalEatStartedEvent(Food)` | `AnimalFeedingSystem` (`TryEat`) | на животном | `AnimalSoundBindingsSystem` |
| `AnimalEatInterruptedEvent` | `AnimalFeedingSystem` (еду прервали) | на животном | `AnimalSoundBindingsSystem` |
| `AnimalAteEvent(Food, Nutrition)` | `AnimalFeedingSystem` | на животном | `ManureProducerSystem` |
| `AnimalDrankEvent(Source, Amount)` | `AnimalFeedingSystem` | на животном | `AnimalSoundBindingsSystem` |
| `AnimalProducedEvent(Product, Units)` | `ManureProducerSystem` | на животном | `AnimalSoundBindingsSystem` |
| `GrowthStageChangedEvent(Index, Stage)` | `GrowthSystem` | на животном | `GrowthEffectsSystem` |
| `RiderMountedEvent(Rider)` | `RideableSystem` | на животном | `RideableNpcSystem`, `AnimalSoundBindingsSystem` |
| `RiderDismountedEvent(Rider)` | `RideableSystem` | на животном | `RideableNpcSystem` |

Appearance: `AnimalVisuals.Eating` (bool) ставит `AnimalFeedingSystem`, слушает `GenericVisualizer` из YAML лошади.

Публичные методы, через которые другие системы могут работать с модулем:
- `AnimalNeedsSystem.ModifySatiety(ent, amount)`, `ModifyHydration(ent, amount)`;
- `AnimalFeedingSystem.CanEat`, `GetNutrition`, `CanDrink`, `TryEat`, `TryDrink`;
- `GrowthSystem.SetStage(ent, index)`;
- `AnimalSexSystem.EnsureRolled(ent)`.

---

## 5. Прототипы и ключи

Сущности: `BaseHusbandryAnimal`, `MobHorse`, `MobHorseFoal`, `MobHorseYoung`, `MobHorseMale`, `MobHorseFemale`, `MobHorseSaddled`, `FoodMeatHorse`, `FoodMeatHorseCooked`, `HorseManure`, `HorseSaddle`, `AnimalTrough`.
Прочее: HTN `HorseCompound`, графы `HorseMeatSteak`, `HorseSaddleGraph`, `AnimalTroughGraph`, рецепты `HorseSaddleConstruction`, `AnimalTroughConstruction`, коллекции звуков `HorseNeigh`, `HorseFootstep`, `HorseEat`, сигналы `soundCue`: `Mounted`, `EatStarted`, `Drank`, `Produced`.

Ключи локализации: `horse-name-foal`, `horse-name-young-male`, `horse-name-young-female`, `horse-name-adult-male`, `horse-name-adult-female`, `animal-needs-{satiety|hydration}-{satisfied|low|empty}`, `rideable-saddle-slot`, `rideable-no-saddle`, `rideable-not-alive`, `rideable-occupied`, `rideable-no-free-hands`, `rideable-cannot-pull`, плюс `ent-*` в ru-RU.

Где что настроено для лошади (все числа в `horse.yml`):

| Параметр | Значение |
|---|---|
| Здоровье | задаётся стадией (`maxHealth`): жеребёнок 80, молодая 140, взрослая 200 (порог `Dead` в `MobThresholds`), замедление от урона с 120 и 160 |
| Скорость | ходьба 4.5, бег 8 |
| Масса | плотность 600 (~380 кг); пока лошадь тащат, `PullMass` на время опускает плотность фикстур до 50 (как у моба), иначе подвижный сустав перетаскивания не сдвигает такую массу |
| Сытость | 100; расход и порог поиска еды задаёт стадия: жеребёнок −1/мин и ≤ 50, молодая −1.5/мин и ≤ 50, взрослая −2.5/мин и ≤ 90; 5 урона/мин при 0. Чем животное больше, тем больше оно ест |
| Жажда | 100, −1/мин, искать воду при ≤ 50, 5 урона/мин при 0 |
| Рацион | `Produce`, без тегов `Meat` и `Trash` (человеческий `Feces` тоже `Produce`) |
| Навоз | `HorseManure`, стак до 100 (стак `HorseManure`); кусков на единицу съеденной питательности: жеребёнок 0.25, молодая 0.5, взрослая 1; в каждом куске 0.5u `Feces` (раньше было 1.2u на единицу питательности) |
| Стадии | жеребёнок 15 мин / масштаб 0.6 / 2 мяса / цена 300; молодая 30 мин / 0.85 / 4 / 700; взрослая / 1.0 / 6 / 1500 |
| Езда | только у взрослой: `Strap`, `ItemSlots` (слот седла) и `Rideable` добавляет стадия `Adult` (у жеребёнка и молодой слота седла нет), одна рука на поводья. Слой `saddle` включает `SaddleVisualsSystem` (компонент `SaddleVisuals`, поле `Slot`, от езды не зависит): он ставит `AnimalVisuals.Saddled`, а `GenericVisualizer` в `horse.yml` показывает слой. `ItemMapper` не подходит: стадию применяет только сервер, а `ItemMapper` не сетевой, поэтому клиент его не видит. `Strap` с `unbuckleDistanceSquared: 0.09` и `maintainSpriteLayers: true`, как у транспорта |
| Цены мяса | сырое 40, готовое 60 (`StaticPrice`) |
| Звуки | таблица `SoundCues` в `horse.yml`: `EatStarted` (коллекция `HorseEat`, жуёт с начала еды и обрывается, если лошадь прервали), `Drank` (`/Audio/Items/drink.ogg`), `Produced` (`splat.ogg`). Для посадки (`Mounted`) звука пока нет. Шаги (`FootstepModifier`) и ржание при поглаживании (`InteractionPopup`) — ванильные компоненты, через `SoundCues` не идут |

---

## 6. Зависимости от SS14 и чем их заменить

Всё, что модуль берёт у движка или контента SS14. Если переносишь в другую игру, заменяется именно это.

| Область | Что используется | Где | Чем заменить в другой игре |
|---|---|---|---|
| Здоровье | `DamageableSystem.TryChangeDamage`, `DamageSpecifier`, `BeforeDamageChangedEvent`, `MobStateSystem`, `RejuvenateEvent` | `AnimalNeedsSystem`, `RiderDamageRedirectSystem`, `NeedState`, `GrowthSystem`, `RideableSystem` | нанесение урона, перехват урона, проверка «жив», полное лечение |
| Еда | `FoodComponent` как маркер «съедобно», раствор `food` (его объём = питательность), `SharedSolutionContainerSystem`, `EntityWhitelist` | `AnimalFeedingSystem`, `FindFoodOperator`, `DietComponent` | свой «предмет-еда» со значением питательности |
| Вода | `SolutionContainerManager` раствор `tank`, `SplitSolution` | `AnimalFeedingSystem` | запас воды источника |
| Действие с задержкой | `SharedDoAfterSystem`, `DoAfterArgs`, `SimpleDoAfterEvent` | `AnimalFeedingSystem`, `FeedingEvents` | таймер действия с отменой при движении и уроне |
| ИИ | HTN: `HTNOperator`, `HTNPrecondition`, `NPCBlackboard`, `PathfindingSystem.GetPath`, `PathPoly`, `InputMoverComponent.CurTick*Movement`, `WaitOperator`, `KeyExistsPrecondition`, `IdleCompound`, `HTNSystem.SetHTNEnabled` | `Npc/*`, `RideableNpcSystem`, `horse_npc.yml` | поведение ИИ: «проголодался → найти достижимую еду → дойти → съесть» |
| Езда | `SharedBuckleSystem`/`StrapComponent`, `SharedMoverController.SetRelay`, `RelayInputMoverComponent`, `SharedVirtualItemSystem`, `ItemSlotsSystem`, `SharedHandsSystem`, `PullerComponent`/`PullAttemptEvent`, `ActionBlockerSystem` | `RideableSystem` | посадка, перенаправление управления, занятая рука, слот предмета |
| Визуал | `SharedAppearanceSystem`, `GenericVisualizer`, `SpriteMovement`, `SharedScaleVisualsSystem`, RSI | `AnimalFeedingSystem`, `GrowthEffectsSystem`, `horse.yml` | анимации и слои спрайта |
| Разделка и цена | `ButcherableComponent.SpawnedEntities`, `MobPriceComponent` (`Content.Server._NF`), `StaticPrice` | `GrowthEffectsSystem`, `horse_food.yml` | данные «сколько мяса» и «сколько стоит» |
| Имя | `MetaDataSystem.SetEntityName` | `GrowthEffectsSystem` | смена названия |
| Осмотр | `ExaminedEvent`, `Identity` | `AnimalNeedsExamineSystem` | текст в описании |
| Сантехника | `PlumbingNode`, `PlumbingOutlet` (из `_Horizon/Plumbing`) | `trough.yml` | любой источник воды |
| Кулинария | граф `construction` с `minTemperature` | `horse_food.yml` | готовка |
| Сущности | `ComponentRegistry`/`AddComponents`, ECS | `GrowthSystem` | механизм «добавить компоненты при входе в стадию» |
| Локализация | Fluent (`.ftl`) | `Locale/*` | система строк игры |
| Звук | `SharedAudioSystem.PlayPredicted`/`Stop`, `SoundSpecifier`, `IResourceManager.ContentFileExists` | модуль `_Horizon/SoundCues`, `AnimalSoundBindingsSystem` | воспроизведение и остановка звука на объекте (см. README `SoundCues`, раздел 7) |

Порядок инициализации: в SS14 порядок обработчиков `MapInit` у разных компонентов не определён, поэтому `GrowthSystem` сам вызывает `AnimalSexSystem.EnsureRolled`, а не полагается на порядок. В другом движке эта связка может понадобиться или исчезнуть.

---

## 7. Как вынуть модуль

1. **Правила.** `Core/*` копируется как есть: три маленьких статических класса и один enum без зависимостей. Их вызывают `AnimalNeedsSystem`, `GrowthSystem`/`AnimalSexSystem`, `ManureProducerSystem`.
2. **Данные.** Компоненты в разделе 3 — простые наборы полей. Перенеси их в формат целевой игры как есть. `NeedState` и `GrowthStageDef` — вложенные структуры.
3. **Поведение.** Перепиши системы по порядку: `AnimalNeedsSystem` → `AnimalFeedingSystem` → `ManureProducerSystem` → `GrowthSystem`/`GrowthEffectsSystem` → `AnimalSexSystem` → ИИ (`Npc/*`) → езда (`RideableSystem`, самая привязанная к SS14).
4. **Связь между системами** идёт через события из раздела 4: сохрани их, и система станет собираться из независимых кусков.
5. **Данные лошади.** Все числа лежат в `horse.yml`, `horse_food.yml`, `trough.yml`; переноси их в формат игры отдельно от кода.
6. **Что можно не переносить сразу.** Езду (`Rideable*`, `Saddle`, `Rider`, `RideableNpcSystem`, `RiderDamageRedirectSystem`, `RideableVisualsSystem`) можно вынуть последней: остальные системы её не используют. Навоз (`ManureProducer*`) и рост (`Growth*`, `AnimalSex*`) тоже независимы друг от друга.
7. **Звуки.** `_Horizon/SoundCues` переносится отдельным модулем (его README, раздел 7). Привязки `Sounds/AnimalSoundBindingsSystem` перепиши под события твоей игры: они только переводят `RiderMountedEvent`, `AnimalEatStartedEvent` и другие в сигналы.

Проверка, что ничего не забыто: в `Core/*` не должно быть `using Robust.*` и `using Content.*`; компоненты модуля не должны ссылаться на ванильные `Hunger`, `Thirst`, `Reproductive`, `Vehicle`.

---

## 8. Связи между подсистемами

| Подсистема | Нужна ли другим | Что нужно ей |
|---|---|---|
| Нужды (`AnimalNeeds`) | да: HTN, питание, осмотр | Damageable, MobState |
| Питание (`Diet`, `WaterSource`, `AnimalFeedingSystem`) | да: HTN, навоз | Нужды, DoAfter, раствор еды |
| ИИ (`Npc/*`, `HorseCompound`) | нет | Нужды, Питание, Pathfinding |
| Навоз (`ManureProducer`) | нет | событие `AnimalAteEvent` |
| Пол (`AnimalSex`) | да: рост (имя) | random |
| Рост (`Growth`) | нет; `Rideable` добавляется через стадию | Пол (для имени), Butcherable, MobPrice |
| Езда (`Rideable*`) | нет | Buckle, ItemSlots, Hands, Mover; ИИ выключается событиями |
| Звуки (`SoundCues`, привязки) | нет | события Питания, Навоза и Езды; без них лошадь работает, только молча |

Без роста лошадь работает (стадий просто нет, `Butcherable` берётся из YAML). Без езды тоже. Без питания лошадь тупо голодает: нужды работают независимо.

---

## 9. Как добавить новое животное

1. В `Resources/Prototypes/_Horizon/Husbandry/` создай прототип `parent: BaseHusbandryAnimal`. Он уже даёт `AnimalNeeds`, `AnimalSex` (без ванильных `Hunger`/`Thirst`, без `DoorBumpOpener`).
2. Подбери числа для `animalNeeds`: `satiety`, `hydration` (`max`, `decayPerMinute`, `seekBelow`, `deprivationDamage`).
3. Добавь `Diet` (белый и чёрный список) и, если нужно, `ManureProducer`.
4. Добавь `Growth` со стадиями, если животное растёт. Названия по полу и стадии — в `Locale`.
5. Сделай спрайты и слои, при необходимости `SpriteMovement` и `GenericVisualizer` для `enum.AnimalVisuals.Eating` (как у лошади).
6. ИИ: скопируй `HorseCompound` (или собери свою ветку из тех же операторов) и укажи `rootTask` в `HTN`. Прогулки (`AnimalWander`) у лошади свои: чтобы новое животное ходило так же, добавь ему компонент `AnimalWander` и используй `HorseIdleCompound` (или свою ветку с `AnimalWanderOperator`), иначе оно ходит по-ванильному.
7. Если на животном можно ездить: слот `saddle_slot` с белым списком по компоненту `Saddle`, `Strap` с `enabled: false`, `Rideable` (в `Growth.stages[].components` для взрослой стадии, если оно растёт).
8. Мясо: свои `Food...` и граф приготовления; `Growth.meat` указывает прототип сырого мяса.
9. Звуки: добавь `SoundCues` с нужными сигналами (`EatStarted`, `Drank`, `Produced`, `Mounted`; имена в `sound_cues.yml`). Чего нет в таблице, то животное не озвучивает, ломать ничего не нужно. Объявляй компонент в самом прототипе, не через `components` стадии `Growth`: он не сетевой, клиент такой компонент не знает.

---

## 10. Известные ограничения и что заменить

- **Не проверено в игре:** порядок отрисовки и оффсеты всадника, тряска при езде с реальной задержкой сети (в тестах без задержки движение ровное), поиск воды и еды, отсутствие `DoorBumpOpener` у готовой лошади.
- **Ловушка Robust:** на пару «компонент + событие» допустим один подписчик на всё приложение. Нельзя подписаться на `StrapComponent, ComponentStartup` и т. п., если это уже делает апстрим (сервер не запустится: `Duplicate Subscriptions`).
- **Отрисовка всадника простая:** на юг он под лошадью целиком, в остальных сторонах над ней. Нормальная посадка (голова лошади поверх всадника, ноги позади) требует разделить спрайт лошади на слои, это работа с артом.
- **Тесты** запускаются в Release (`dotnet test -c Release`): в Debug отладочная проверка в чужом `Content.Client/_Mono/Audio/AudioEffectSystem` роняет тестовый клиент при любом звуке.
- **Нет сообщения**, почему нельзя сесть на жеребёнка или молодую лошадь (`Strap` выключен, `Rideable` появляется только на взрослой стадии).
- **Питательность** равна объёму раствора `food` × `nutritionPerUnit`, считается весь объём, а не только питательные вещества (яблоко 14, морковь 12). Сытость ограничена 100, лишнее пропадает, а навоз считается от всей съеденной питательности. Значения для разных продуктов различаются слабо, подбирается в игре.
- **Правки вне модуля** (две, помечены `Horizon`), без них стак навоза не работает: `Content.Server/Materials/ProduceMaterialExtractorSystem.cs` (биогенератор считает раствор × размер стака) и `Content.Server/Botany/Systems/PlantHolderSystem.cs` (`CompostStack`: в грядку идёт столько кусков стака, сколько влезет, остальное остаётся в руке). Обе правки общие для любого стакающегося `Produce`.
- **Баланс навоза:** в куске 0.5u `Feces`. Биогенератор: 1u = 1 биомасса, значит 0.5 за кусок, стак из 100 даёт 50; взрослая лошадь ≈ 75 биомассы в час (150 питательности × 1 кусок × 0.5), ящик почвы стоит 1600, то есть лошадь не заменяет покупку почвы. Путь «яблоко → лошадь → биомасса» всегда хуже прямого (яблоко 14 объёма: 7 против 10). Грядка: 1u `Feces` даёт 4 питательности, кусок 2, около 50 кусков заполняют пустую грядку, взрослая лошадь набирает их примерно за 20 минут.
- **Прогулки (`AnimalWander`):** случайные перемещения лошади не используют штатный `NPCSteeringSystem`, который каждый тик пересчитывает направление из 16 и дёргает животных у стен. Прогулка состоит из нескольких отрезков (2–4): лошадь проверяет лучами коридор шириной с тело, идёт по прямой шагом и замедляется у цели, короткая пауза, и следующий отрезок с поворотом не более 70° от прошлого. Ограничения: нет привязки к «дому» (бродит где угодно), не проверяются опасные тайлы (лава, шипы), закрытые двери считаются стеной. Дорога к еде и воде тоже идёт через `AnimalWalker`: путь, найденный `FindFoodOperator`/`FindWaterOperator`, срезается по прямым там, где свободен коридор шириной с тело; при застревании маршрут проваливается и ИИ переплановывает (может выбрать ту же цель снова).
- **Засыпание ИИ:** сервер (Mono) останавливает ИИ, когда в радиусе `npc.player_pause_distance` (32 тайла) нет игроков, и такие лошади стоят. Лошади переопределяют радиус: `HTN.sleepPlayerCheckRangeOverride: 100`.
- **Навоз не сливается в стаки сам:** каждый приём пищи оставляет отдельный стак (до 100 кусков), сливать их приходится руками.
- **Рывки при езде:** всадник и лошадь стоят в одной точке, и толчок мобов друг от друга (`MobCollision`) мог сдвигать и замедлять лошадь на сервере, о чём клиент не знал. `RideableSystem` отключает толчок всадника (`AttemptMobTargetCollideEvent`). В игре не проверено.
- **Кулинарные рецепты**, принимающие `FoodMeat`, конину не принимают. Стейк готовится своим графом, нарезка даёт обычные котлеты.
- **Поилка** не проверяет, что именно течёт по трубам: лошадь выпьет любую жидкость.
- **Плейсхолдеры:**
  - `horse.rsi`, `saddle.rsi` и `trough.rsi` нарисованы заново (раздел 12), это черновики: ходьба и еда пока один кадр на направление, `dead` заглушка;
  - конина и анализатор: копии ванильных спрайтов с пометкой `PLACEHOLDER` в `meta.json` (лицензия CC-BY-SA-3.0 перенесена с оригинала, при замене на свой арт поправь `license` и `copyright`);
  - у сырой конины в `horse_food.yml` стоит `color` для перекраски копии, при своём арте убери;
  - в `sounds.yml` заглушки остались у ржания `HorseNeigh` (`cow_moo.ogg`, играет при поглаживании) и шагов `HorseFootstep` (звуки дерева); настоящие файлы положи в `Resources/Audio/_Horizon/Animals/` и опиши в `attributions.yml`. Хруст `HorseEat` уже настоящий (раздел 12), для посадки (`Mounted`) звука пока нет.
- **Размножения нет.** Пол и стадии готовы; отдельный `Breeding` можно сделать на их основе.

---

## 11. Ассеты: что нужно нарисовать и записать

Папка для текстур: `Resources/Textures/_Horizon/`. У каждого RSI в `meta.json` поправь `license` и `copyright`, когда положишь свой арт. Размер кадра 32×32, если не сказано иначе. Если меняешь имя состояния, поменяй его и в YAML (указано в колонке «Где используется»).

| RSI | Состояние | Направления | Что это | Где используется |
|---|---|---|---|---|
| `Mobs/Animals/horse.rsi` | `horse` | 4 | лошадь стоит | `horse.yml`, слой `Base` |
| | `horse-moving` | 4 | лошадь идёт | `SpriteMovement` |
| | `horse-eating` | 4 | лошадь ест или пьёт: голова опущена (нарисована из `horse` поворотом головы и шеи, черновик) | `GenericVisualizer` по `AnimalVisuals.Eating` |
| | `saddle`, `saddle-moving`, `saddle-eating` | 4 | только седло, кадры совпадают с лошадью, лежит поверх | слой `saddle`, показывается, когда седло в слоте |
| | `dead` | 1 | мёртвая лошадь | `DamageStateVisuals` |
| `Objects/Husbandry/saddle.rsi` | `icon` | 1 | седло как предмет | `saddle.yml`, меню строительства |
| `Objects/Husbandry/trough.rsi` | `trough` | 4 (можно одинаковые) | поилка | `trough.yml`, меню строительства |
| | `trough-fill-1` | 4 | вода в поилке, накладывается поверх | `SolutionContainerVisuals`, `fillBaseName: trough-fill-` |
| `Objects/Husbandry/horse_manure.rsi` | `icon` | 1 | куча навоза (один спрайт для любого размера стака) | `horse_food.yml` |
| `Objects/Husbandry/horse_meat.rsi` | `raw`, `cooked` | 1 | сырая и готовая конина | `horse_food.yml` |
| `Objects/Devices/debug_analyzer.rsi` | `icon` | 1 | предмет | `debug_analyzer.yml` |
| | `analyzer` | 1, 12 кадров | светящийся слой экрана (`unshaded`), задержки `0.1`×11 и `0.5` | `debug_analyzer.yml` |
| | `analyzer-inhand-left`, `analyzer-inhand-right` | 4 | в руке, имена задаёт `heldPrefix: analyzer` | `Item` |

Порядок направлений в PNG сетки 2×2: юг (слева сверху), север (справа сверху), **восток (слева снизу, смотрит вправо)**, запад (справа снизу, смотрит влево). У всех состояний `horse*` и `saddle*` он должен быть одинаковым, иначе лошадь при смене состояния (стоит, идёт, ест) разворачивается боком не туда. Верхом лошадь всегда в состоянии `horse-moving`, поэтому ошибка в порядке этого файла проявляется именно при езде влево и вправо.

Окно дебаг-анализатора рисуется кодом (подписи и полосы), спрайтов не требует.

Необязательно, если захочется:
- **Уровни воды в поилке:** сейчас `maxFillLevels: 1`. Для трёх уровней нарисуй `trough-fill-1..3` и поставь `maxFillLevels: 3`.
- **Свой арт жеребёнка и молодой лошади:** сейчас стадии только масштабируют спрайт (0.6 и 0.85), отдельных состояний нет, потребуется доработка `Growth`.
- **Окрас:** серые слои шерсти, гривы и отметин для случайной раскраски (идея, не реализовано).
- **Посадка всадника:** верхняя часть лошади отдельным слоем поверх всадника (идея, не реализовано, см. обсуждение в разделе 10).

Звуки (не спрайты), положи в `Resources/Audio/_Horizon/Animals/` и опиши в `attributions.yml`; пути в `Prototypes/_Horizon/Husbandry/sounds.yml`:
- `HorseNeigh`: ржание, 1–3 с, несколько вариантов (сейчас заглушка);
- звук для посадки верхом, около 1 с, лучше фырканье (сигнал `Mounted`, сейчас звука нет: заведи коллекцию в `sounds.yml` и добавь `Mounted` в таблицу `SoundCues` лошади в `horse.yml`);
- `HorseFootstep`: один удар копыта, 0.2–0.4 с, 4–6 вариантов (сейчас заглушка);
- `HorseEat`: хруст, 2–4 коротких клипа (готово, 3 клипа).

Требования к файлам: **mono OGG**. Движок не умеет позиционировать стерео: звук на сущности из стерео-файла в Debug-сборке падает на `Assert` (`AudioSource.cs`), в Release играет без затухания по расстоянию. Громкость подгоняется под соседние звуки игры (звук около −20…−27 dBFS, пики не выше −3). Записи берутся только под CC0 или свои: CC-BY и подобные требуют указать автора в титрах игры, а проект этого не хочет (фырканье под CC-BY 4.0 поэтому убрано). Всё это делает скрипт `Tools/_Horizon/audio/prepare_sfx.py` (нарезка, моно, громкость, OGG; нужен `pip install soundfile numpy`): новый звук добавляется как `Job` в его список.

---

## 12. Лицензия и авторство

Автор модуля и ассетов ниже: **mrl4an**. Лицензия этих ассетов: **CC-BY-SA-3.0**: её можно использовать где угодно с указанием авторства, а переработки должны остаться под той же лицензией. Поэтому любая чужая переработка этих спрайтов доступна и автору.

| Что | Автор | Примечание |
|---|---|---|
| `Mobs/Animals/horse.rsi` | mrl4an | сделана по большому референсу: уменьшена, обведена, перерисована, разрезана на направления и слой седла, позы «ест» и «мёртвая» |
| `Objects/Husbandry/saddle.rsi` | mrl4an | иконка сделана из арта лошади и седла |
| `Objects/Husbandry/trough.rsi` | mrl4an | распиленное бревно с выемкой под воду |
| `Objects/Husbandry/horse_manure.rsi` | mrl4an | тот же спрайт, что `_Horizon/Objects/Misc/feces.rsi` (его автор тоже mrl4an) |

**Звуки** (`Resources/Audio/_Horizon/Animals/`, описаны в `attributions.yml` рядом) сделаны из чужих записей. Берутся только CC0 и свои записи, чтобы не пришлось указывать авторов в титрах игры:

| Что | Автор | Лицензия | Примечание |
|---|---|---|---|
| `horse_eat_1..3.ogg` | Joseph Sardin ([BigSoundBank](https://bigsoundbank.com/horse-eats-carrot-3-s1847.html)) | CC0-1.0 | одна запись, нарезана на три клипа; ограничений нет |

**Чужое, не переписывать на себя**, пока спрайт реально не перерисован (проверено: пиксели совпадают с оригиналом): `horse_meat.rsi`, `Objects/Devices/debug_analyzer.rsi`. У них в `meta.json` остаются лицензия и авторство оригинала.

Код модуля отдельно от ассетов, он лежит под лицензией проекта; здесь автор и лицензия кода не менялись.
