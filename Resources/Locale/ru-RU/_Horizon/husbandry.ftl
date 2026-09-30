# Названия лошади по стадии роста и полу
horse-name-foal = жеребёнок
horse-name-young-male = молодой конь
horse-name-young-female = молодая лошадь
horse-name-adult-male = конь
horse-name-adult-female = лошадь

# Нужды, при осмотре
animal-needs-satiety-satisfied = { CAPITALIZE(SUBJECT($entity)) } выглядит сытым.
animal-needs-satiety-low = { CAPITALIZE(SUBJECT($entity)) } выглядит голодным.
animal-needs-satiety-empty = [color=red]{ CAPITALIZE(SUBJECT($entity)) } умирает от голода![/color]
animal-needs-hydration-satisfied = { CAPITALIZE(SUBJECT($entity)) } не хочет пить.
animal-needs-hydration-low = { CAPITALIZE(SUBJECT($entity)) } хочет пить.
animal-needs-hydration-empty = [color=red]{ CAPITALIZE(SUBJECT($entity)) } умирает от жажды![/color]

# Название стака навоза
stack-horse-manure = конский навоз

# Езда верхом
rideable-saddle-slot = Седло
rideable-no-saddle = На { THE($animal) } нет седла.
rideable-not-alive = { CAPITALIZE(THE($animal)) } не в состоянии везти всадника.
rideable-occupied = На { THE($animal) } уже кто-то сидит.
rideable-no-free-hands = Нужна свободная рука, чтобы держать поводья.
rideable-cannot-pull = Нельзя ехать верхом, пока вы что-то тянете.

# Сущности
ent-MobHorse = лошадь
    .desc = Большое сильное животное. С седлом на ней можно ездить верхом.
ent-MobHorseFoal = { ent-MobHorse }
    .suffix = Жеребёнок
    .desc = { ent-MobHorse.desc }
ent-MobHorseYoung = { ent-MobHorse }
    .suffix = Молодая
    .desc = { ent-MobHorse.desc }
ent-MobHorseMale = { ent-MobHorse }
    .suffix = Самец
    .desc = { ent-MobHorse.desc }
ent-MobHorseFemale = { ent-MobHorse }
    .suffix = Самка
    .desc = { ent-MobHorse.desc }
ent-FoodMeatHorse = сырая конина
    .desc = Кусок сырой конины.
ent-FoodMeatHorseCooked = конский стейк
    .desc = Приготовленный кусок конины. Постный и сладковатый.
ent-HorseManure = конский навоз
    .desc = Куча конского навоза. Растения его любят, но не слишком.
ent-HorseSaddle = седло
    .desc = Наденьте на лошадь, чтобы ездить на ней верхом.
ent-AnimalTrough = поилка для животных
    .desc = Из неё пьют животные. Подключите её к водопроводу или наполните вручную.
