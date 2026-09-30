# Names of the horse by growth stage and sex
horse-name-foal = foal
horse-name-young-male = young stallion
horse-name-young-female = young mare
horse-name-adult-male = stallion
horse-name-adult-female = mare

# Needs, shown on examine
animal-needs-satiety-satisfied = { CAPITALIZE(SUBJECT($entity)) } looks well fed.
animal-needs-satiety-low = { CAPITALIZE(SUBJECT($entity)) } looks hungry.
animal-needs-satiety-empty = [color=red]{ CAPITALIZE(SUBJECT($entity)) } is starving![/color]
animal-needs-hydration-satisfied = { CAPITALIZE(SUBJECT($entity)) } is not thirsty.
animal-needs-hydration-low = { CAPITALIZE(SUBJECT($entity)) } looks thirsty.
animal-needs-hydration-empty = [color=red]{ CAPITALIZE(SUBJECT($entity)) } is dying of thirst![/color]

# Name of the manure stack
stack-horse-manure = horse manure

# Riding
rideable-saddle-slot = Saddle
rideable-no-saddle = { CAPITALIZE(THE($animal)) } has no saddle.
rideable-not-alive = { CAPITALIZE(THE($animal)) } is in no state to be ridden.
rideable-occupied = Somebody is already riding { THE($animal) }.
rideable-no-free-hands = You need a free hand to hold the reins.
rideable-cannot-pull = You cannot ride while pulling something.
