#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Интерактивное меню Git для Horizon Nova / SS14
"""

import subprocess
import sys
import os

def run(cmd, check=True):
    result = subprocess.run(
        cmd,
        shell=True,
        capture_output=True,
        text=True,
        encoding="utf-8",
        errors="replace"
    )
    if check and result.returncode != 0:
        print(f"\n[ОШИБКА] {cmd}")
        if result.stderr:
            print(result.stderr.strip())
        return None
    return result

def clear():
    os.system("cls" if os.name == "nt" else "clear")

def pause():
    input("\nНажми Enter, чтобы продолжить...")

def get_current_branch():
    res = run("git branch --show-current", check=False)
    return res.stdout.strip() if res and res.stdout else "unknown"

def show_status():
    clear()
    print("=== СТАТУС ===\n")
    print(f"Текущая ветка: {get_current_branch()}\n")
    res = run("git status")
    if res:
        print(res.stdout)
    pause()

def do_commit(push_after=False):
    clear()
    title = "=== КОММИТ + PUSH ===" if push_after else "=== КОММИТ ==="
    print(title + "\n")

    status = run("git status --short")
    if not status or not status.stdout.strip():
        print("Нет изменений для коммита.")
        pause()
        return

    print("Изменения:")
    print(status.stdout)

    # Добавляем всё, кроме того, что в .gitignore
    print("Добавляю изменения (git add .)...")
    run("git add .")

    staged = run("git diff --cached --name-status")
    if not staged or not staged.stdout.strip():
        print("После фильтрации нечего коммитить.")
        pause()
        return

    print("\nБудет закоммичено:")
    print(staged.stdout)

    message = input("\nСообщение коммита: ").strip()
    if not message:
        print("Сообщение не может быть пустым.")
        pause()
        return

    res = run(f'git commit -m "{message}"')
    if res is None:
        pause()
        return

    print("\nКоммит создан.")
    log = run("git log -1 --oneline")
    if log:
        print(log.stdout)

    if push_after:
        branch = get_current_branch()
        print(f"\nПушу в origin/{branch}...")
        res = run(f"git push -u origin {branch}")
        if res:
            print("Push выполнен успешно.")
        else:
            print("Ошибка при push.")

    pause()

def list_branches():
    clear()
    print("=== ВЕТКИ ===\n")

    print("--- Локальные ветки ---")
    local = run("git branch")
    if local:
        print(local.stdout)

    print("--- Ветки на сервере (origin) ---")
    run("git fetch --prune", check=False)
    remote = run("git branch -r")
    if remote:
        print(remote.stdout)

    pause()

def switch_branch():
    clear()
    print("=== ПЕРЕКЛЮЧЕНИЕ / СКАЧИВАНИЕ ВЕТКИ ===\n")

    print("--- Локальные ---")
    local_res = run("git branch --format=\"%(refname:short)\"")
    local_branches = [b.strip() for b in local_res.stdout.splitlines()] if local_res else []
    for i, b in enumerate(local_branches, 1):
        mark = "  <-- текущая" if b == get_current_branch() else ""
        print(f"  {i}. {b}{mark}")

    print("\n--- На сервере (origin) ---")
    run("git fetch --prune", check=False)
    remote_res = run("git branch -r --format=\"%(refname:short)\"")
    remote_branches = []
    if remote_res:
        for line in remote_res.stdout.splitlines():
            name = line.strip()
            if name.startswith("origin/") and "->" not in name:
                short = name[7:]  # убираем origin/
                remote_branches.append(short)
                print(f"  R{len(remote_branches)}. {short}")

    print("\nВведи номер локальной ветки")
    print("или R + номер серверной (например R1)")
    print("или просто имя ветки")
    choice = input("\nВыбор: ").strip()

    if not choice:
        return

    target = None

    if choice.isdigit():
        idx = int(choice) - 1
        if 0 <= idx < len(local_branches):
            target = local_branches[idx]
    elif choice.upper().startswith("R") and choice[1:].isdigit():
        idx = int(choice[1:]) - 1
        if 0 <= idx < len(remote_branches):
            target = remote_branches[idx]
            print(f"\nСкачиваю origin/{target}...")
            run(f"git checkout -B {target} origin/{target}")
            print(f"Готово. Текущая ветка: {get_current_branch()}")
            pause()
            return
    else:
        target = choice

    if target:
        print(f"\nПереключаюсь на {target}...")
        res = run(f"git checkout {target}")
        if res:
            print(f"Текущая ветка: {get_current_branch()}")
        pause()
    else:
        print("Некорректный выбор.")
        pause()

def show_commits(local=True):
    clear()
    if local:
        print("=== ПОСЛЕДНИЕ ЛОКАЛЬНЫЕ КОММИТЫ ===\n")
        res = run("git log --oneline -20 --decorate")
    else:
        print("=== ПОСЛЕДНИЕ КОММИТЫ НА СЕРВЕРЕ ===\n")
        run("git fetch --prune", check=False)
        branch = get_current_branch()
        res = run(f"git log origin/{branch} --oneline -20 --decorate")

    if res and res.stdout:
        print(res.stdout)
    else:
        print("Нет данных.")
    pause()

def fetch_pull():
    clear()
    print("=== FETCH / PULL ===\n")
    print("1. Только Fetch")
    print("2. Pull")
    print("0. Назад")
    choice = input("\nВыбор: ").strip()

    if choice == "1":
        print("\nВыполняю git fetch --all --prune...")
        res = run("git fetch --all --prune")
        if res:
            print("Fetch выполнен.")
    elif choice == "2":
        print("\nВыполняю git pull...")
        res = run("git pull")
        if res:
            print("Pull выполнен.")
            if res.stdout:
                print(res.stdout)
    pause()

def create_branch():
    clear()
    print("=== СОЗДАНИЕ НОВОЙ ВЕТКИ ===\n")
    name = input("Имя новой ветки: ").strip()
    if not name:
        return

    print(f"Создаю ветку {name}...")
    res = run(f"git checkout -b {name}")
    if res:
        print(f"Готово. Текущая ветка: {get_current_branch()}")
    pause()

def main_menu():
    while True:
        clear()
        branch = get_current_branch()
        print("=" * 50)
        print("       Horizon Nova — Git Menu")
        print("=" * 50)
        print(f"Текущая ветка: {branch}\n")
        print("1. Статус")
        print("2. Коммит")
        print("3. Коммит + Push")
        print("4. Список веток")
        print("5. Переключить / скачать ветку")
        print("6. Создать новую ветку")
        print("7. Последние коммиты (локальные)")
        print("8. Последние коммиты (сервер)")
        print("9. Fetch / Pull")
        print("0. Выход")
        print()

        choice = input("Выбор: ").strip()

        if choice == "1":
            show_status()
        elif choice == "2":
            do_commit(push_after=False)
        elif choice == "3":
            do_commit(push_after=True)
        elif choice == "4":
            list_branches()
        elif choice == "5":
            switch_branch()
        elif choice == "6":
            create_branch()
        elif choice == "7":
            show_commits(local=True)
        elif choice == "8":
            show_commits(local=False)
        elif choice == "9":
            fetch_pull()
        elif choice == "0":
            print("Выход.")
            break
        else:
            print("Неверный пункт.")
            pause()

if __name__ == "__main__":
    if run("git rev-parse --is-inside-work-tree", check=False).returncode != 0:
        print("Ошибка: это не git-репозиторий.")
        input("Нажми Enter...")
        sys.exit(1)

    try:
        main_menu()
    except KeyboardInterrupt:
        print("\n\nВыход.")
