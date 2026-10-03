-markings-selection = { $selectable ->
    [0] Вы больше не можете выбрать черту.
    [one] Вы можете выбрать еще одну черту.
    *[other] Вы можете выбрать ещё { $selectable } черты.
}
markings-limits = { $required ->
    [true] { $count ->
            [-1] Выберите хотя бы одну черту.
            [0] Вы не можете выбрать ещё черту, но как-то, должны? Это баг.
            [one] Выберите одну черту.
            *[other] Выберите хотя бы одну черту и до { $count }. { -markings-selection(selectable: $selectable) }
        }
    *[false] { $count ->
            [-1] Выберите любое количество черт.
            [0] Вы больше не можете выбрать черту.
            [one] Выберите до одной черты.
            *[other] Выберите до { $count } черт. { -markings-selection(selectable: $selectable) }
        }
}
markings-reorder = Выбранные черты

# Body regions (organ tabs)
markings-organ-Head = Голова
markings-organ-Torso = Туловище
markings-organ-LeftArm = Левая рука
markings-organ-RightArm = Правая рука
markings-organ-LeftLeg = Левая нога
markings-organ-RightLeg = Правая нога
markings-organ-Tail = Хвост
markings-organ-Special = Особое
